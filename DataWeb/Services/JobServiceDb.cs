using System.Collections.Concurrent;
using DataWeb.Domain;
using DataWeb.Domain.Entities;
using DataWeb.Infrastructure.Repositories;

namespace DataWeb.Services;

// Implementação do JobService que persiste os dados no banco de dados MySQL.
public class JobServiceDb : IJobService
{
    private readonly IJobRepository _repository;
    private readonly ILogger<JobServiceDb> _logger;
    private readonly string _diretorioResultados;
    
    // Arquivos temporários ficam em memória (são grandes e temporários)
    // IMPORTANTE: Estático para ser compartilhado entre todos os scopes/instâncias
    private static readonly ConcurrentDictionary<string, byte[]> _arquivosTemporarios = new();

    public JobServiceDb(
        IJobRepository repository,
        ILogger<JobServiceDb> logger,
        IWebHostEnvironment env)
    {
        _repository = repository;
        _logger = logger;
        _diretorioResultados = Path.Combine(env.ContentRootPath, "temp", "resultados");
        Directory.CreateDirectory(_diretorioResultados);
    }

    public async Task<string> CriarJobAsync(
        string? usuarioId,
        string nomeArquivo, 
        long tamanhoArquivo, 
        Stream arquivoStream)
    {
        // Converter usuarioId string para int (0 se não informado ou inválido)
        var userIdInt = 0;
        if (!string.IsNullOrEmpty(usuarioId) && int.TryParse(usuarioId, out var parsed))
        {
            userIdInt = parsed;
        }

        // Criar entidade para o banco
        var jobEntity = new DataJudJob
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userIdInt,
            FileName = nomeArquivo,
            OriginalFileSize = tamanhoArquivo,
            Status = DataJudJobStatus.Pendente,
            CreatedAt = DateTime.UtcNow
        };

        // Salvar no banco
        await _repository.CreateAsync(jobEntity);

        // Salvar arquivo temporariamente em memória
        using var ms = new MemoryStream();
        await arquivoStream.CopyToAsync(ms);
        _arquivosTemporarios[jobEntity.Id] = ms.ToArray();

        _logger.LogInformation(
            "Job criado no banco: {JobId} para usuário {UsuarioId}", 
            jobEntity.Id, 
            usuarioId);

        return jobEntity.Id;
    }

    public async Task<ProcessamentoJob?> ObterJobAsync(string jobId)
    {
        var entity = await _repository.GetByIdAsync(jobId);
        if (entity == null) return null;

        // Converter entidade do banco para o DTO usado pela aplicação
        return MapEntityToProcessamentoJob(entity);
    }

    public async Task<IEnumerable<ProcessamentoJob>> ListarJobsPorUsuarioAsync(string? usuarioId)
    {
        IEnumerable<DataJudJob> entities;

        if (!string.IsNullOrEmpty(usuarioId) && int.TryParse(usuarioId, out var userIdInt))
        {
            entities = await _repository.GetByUserIdAsync(userIdInt);
        }
        else
        {
            // Se não informou usuário, busca jobs pendentes (para o background service)
            entities = await _repository.GetPendingJobsAsync();
        }

        return entities.Select(MapEntityToProcessamentoJob);
    }

    public async Task<bool> MarcarJobComoProcessandoAsync(string jobId)
    {
        var entity = await _repository.GetByIdAsync(jobId);
        if (entity == null) return false;

        entity.Status = DataJudJobStatus.Processando;
        entity.StartedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(entity);
        
        _logger.LogInformation("Job {JobId} marcado como processando", jobId);
        return true;
    }

    public async Task<bool> MarcarJobComoConcluidoAsync(string jobId, string caminhoResultado, int totalProcessos)
    {
        var entity = await _repository.GetByIdAsync(jobId);
        if (entity == null) return false;

        entity.Status = DataJudJobStatus.Concluido;
        entity.CompletedAt = DateTime.UtcNow;
        entity.ResultFilePath = caminhoResultado;
        entity.TotalProcessos = totalProcessos;
        entity.ProcessosProcessados = totalProcessos;

        // Calcular tamanho do arquivo resultado
        if (File.Exists(caminhoResultado))
        {
            entity.ResultFileSize = new FileInfo(caminhoResultado).Length;
        }

        await _repository.UpdateAsync(entity);

        _logger.LogInformation(
            "Job {JobId} concluído com {Total} processos", 
            jobId, 
            totalProcessos);

        return true;
    }

    public async Task<bool> MarcarJobComoErroAsync(string jobId, string erro)
    {
        var entity = await _repository.GetByIdAsync(jobId);
        if (entity == null) return false;

        entity.Status = DataJudJobStatus.Erro;
        entity.ErrorMessage = erro;
        entity.CompletedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(entity);

        _logger.LogError("Job {JobId} marcado com erro: {Erro}", jobId, erro);
        return true;
    }

    public async Task AtualizarProgressoAsync(string jobId, int processados, int total)
    {
        var entity = await _repository.GetByIdAsync(jobId);
        if (entity == null) return;

        entity.ProcessosProcessados = processados;
        entity.TotalProcessos = total;

        await _repository.UpdateAsync(entity);
    }

    public byte[]? ObterArquivoTemporario(string jobId)
    {
        _arquivosTemporarios.TryGetValue(jobId, out var arquivo);
        return arquivo;
    }

    public void RemoverArquivoTemporario(string jobId)
    {
        _arquivosTemporarios.TryRemove(jobId, out _);
        _logger.LogDebug("Arquivo temporário removido para job {JobId}", jobId);
    }

    private static ProcessamentoJob MapEntityToProcessamentoJob(DataJudJob entity)
    {
        return new ProcessamentoJob
        {
            Id = entity.Id,
            UsuarioId = entity.UserId.ToString(),
            NomeArquivo = entity.FileName,
            TamanhoArquivo = entity.OriginalFileSize,
            Status = MapStatus(entity.Status),
            CriadoEm = entity.CreatedAt,
            IniciadoEm = entity.StartedAt,
            ConcluidoEm = entity.CompletedAt,
            CaminhoResultado = entity.ResultFilePath,
            Erro = entity.ErrorMessage,
            TotalProcessos = entity.TotalProcessos,
            ProcessosProcessados = entity.ProcessosProcessados
        };
    }

    private static JobStatus MapStatus(string status)
    {
        return status switch
        {
            DataJudJobStatus.Pendente => JobStatus.Pendente,
            DataJudJobStatus.Processando => JobStatus.Processando,
            DataJudJobStatus.Concluido => JobStatus.Concluido,
            DataJudJobStatus.Erro => JobStatus.Erro,
            _ => JobStatus.Pendente
        };
    }
}
