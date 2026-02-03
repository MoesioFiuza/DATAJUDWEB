using System.Collections.Concurrent;
using DataWeb.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataWeb.Services;

public class JobService : IJobService
{
    private readonly ConcurrentDictionary<string, ProcessamentoJob> _jobs = new();
    private readonly ConcurrentDictionary<string, string> _arquivosTemporarios = new();
    private readonly string _diretorioResultados;
    private readonly string _diretorioUploads;
    private readonly ILogger<JobService> _logger;

    public JobService(ILogger<JobService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _diretorioResultados = Path.Combine(env.ContentRootPath, "temp", "resultados");
        _diretorioUploads = Path.Combine(env.ContentRootPath, "temp", "uploads");
        Directory.CreateDirectory(_diretorioResultados);        
        Directory.CreateDirectory(_diretorioUploads);
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromHours(1));
                LimparJobsAntigos(TimeSpan.FromHours(24));
            }
        });
    }

    public async Task<string> CriarJobAsync(string? usuarioId, string nomeArquivo, long tamanhoArquivo, Stream arquivoStream)
    {
        var job = new ProcessamentoJob
        {
            UsuarioId = usuarioId,
            NomeArquivo = nomeArquivo,
            TamanhoArquivo = tamanhoArquivo,
            Status = JobStatus.Pendente
        };

        var caminhoUpload = Path.Combine(_diretorioUploads, $"{job.Id}.xlsx");
        await using (var fs = new FileStream(caminhoUpload, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await arquivoStream.CopyToAsync(fs);
        }
        job.CaminhoArquivoTemp = caminhoUpload;
        _arquivosTemporarios[job.Id] = caminhoUpload;

        _jobs[job.Id] = job;
        _logger.LogInformation("Job criado: {JobId} para usuário {UsuarioId}", job.Id, usuarioId);
        
        return job.Id;
    }

    public Task<ProcessamentoJob?> ObterJobAsync(string jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return Task.FromResult(job);
    }

    public Task<IEnumerable<ProcessamentoJob>> ListarJobsPorUsuarioAsync(string? usuarioId)
    {
        var jobs = _jobs.Values
            .Where(j => usuarioId == null || j.UsuarioId == usuarioId)
            .OrderByDescending(j => j.CriadoEm)
            .ToList();
        return Task.FromResult(jobs.AsEnumerable());
    }

    public Task<bool> MarcarJobComoProcessandoAsync(string jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = JobStatus.Processando;
            job.IniciadoEm = DateTime.UtcNow;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> MarcarJobComoConcluidoAsync(string jobId, string caminhoResultado, int totalProcessos)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = JobStatus.Concluido;
            job.ConcluidoEm = DateTime.UtcNow;
            job.CaminhoResultado = caminhoResultado;
            job.TotalProcessos = totalProcessos;
            job.ProcessosProcessados = totalProcessos;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> MarcarJobComoErroAsync(string jobId, string erro)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = JobStatus.Erro;
            job.Erro = erro;
            job.ConcluidoEm = DateTime.UtcNow;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task AtualizarProgressoAsync(string jobId, int processados, int total)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.ProcessosProcessados = processados;
            job.TotalProcessos = total;
        }
        return Task.CompletedTask;
    }

    // Método público para o BackgroundService acessar
    public string? ObterArquivoTemporario(string jobId)
    {
        _arquivosTemporarios.TryGetValue(jobId, out var arquivo);
        return arquivo;
    }

    public void RemoverArquivoTemporario(string jobId)
    {
        if (_arquivosTemporarios.TryRemove(jobId, out var caminho) && File.Exists(caminho))
        {
            try { File.Delete(caminho); } catch { }
        }
    }

    private void LimparJobsAntigos(TimeSpan idade)
    {
        var limite = DateTime.UtcNow - idade;
        var paraRemover = _jobs.Values
            .Where(j => j.ConcluidoEm.HasValue && j.ConcluidoEm < limite)
            .Select(j => j.Id)
            .ToList();

        foreach (var id in paraRemover)
        {
            if (_jobs.TryRemove(id, out var job))
            {
                // Remover arquivo de resultado se existir
                if (!string.IsNullOrEmpty(job.CaminhoResultado) && File.Exists(job.CaminhoResultado))
                {
                    try { File.Delete(job.CaminhoResultado); } catch { }
                }
            }
            if (_arquivosTemporarios.TryRemove(id, out var caminho) && File.Exists(caminho))
            {
                try { File.Delete(caminho); } catch { }
            }
        }

        _logger.LogInformation("Limpeza: {Count} jobs antigos removidos", paraRemover.Count);
    }
}
