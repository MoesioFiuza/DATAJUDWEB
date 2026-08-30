using DataWeb.Domain;
using DataWeb.Exporters;
using DataWeb.Parsers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace DataWeb.Services;

public class ProcessamentoBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProcessamentoBackgroundService> _logger;
    private readonly SemaphoreSlim _semaphore;

    public ProcessamentoBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ProcessamentoBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        var maxConcorrencia = configuration.GetValue<int>("Processamento:MaxConcorrencia", 3);
        _semaphore = new SemaphoreSlim(maxConcorrencia);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessarProximoJobAsync(stoppingToken);
                // ALTERADO: Aumentado de 1 para 5 segundos para reduzir queries no banco
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no processamento de jobs");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcessarProximoJobAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<IJobService>();        
        var jobs = await jobService.ListarJobsPorUsuarioAsync(null);
        var jobPendente = jobs.FirstOrDefault(j => j.Status == JobStatus.Pendente);
        
        if (jobPendente == null) return;

        await _semaphore.WaitAsync(ct);
        _ = Task.Run(async () =>
        {
            try
            {
                await ProcessarJobAsync(jobPendente.Id, ct);
            }
            finally
            {
                _semaphore.Release();
            }
        }, ct);
    }

    private async Task ProcessarJobAsync(string jobId, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<IJobService>();
        
        var job = await jobService.ObterJobAsync(jobId);
        if (job == null || job.Status != JobStatus.Pendente) return;

        await jobService.MarcarJobComoProcessandoAsync(jobId);
        _logger.LogInformation("Iniciando processamento do job {JobId}", jobId);

        try
        {
            var consultaUseCase = scope.ServiceProvider.GetRequiredService<IConsultaUseCase>();
            var parser = scope.ServiceProvider.GetRequiredService<IDatajudParser>();
            var exporter = scope.ServiceProvider.GetRequiredService<IExcelExporter>();

            // Obter arquivo temporário
            var arquivoBytes = jobService.ObterArquivoTemporario(jobId);
            if (arquivoBytes == null)
            {
                await jobService.MarcarJobComoErroAsync(jobId, "Arquivo não encontrado");
                return;
            }

            using var stream = new MemoryStream(arquivoBytes);
            
            // Processar
            var respostas = await consultaUseCase.ConsultarJsonAsync(stream, paralelismo: 20, ct);
            var linhas = parser.ExtrairLinhas(respostas);
            var excelBytes = exporter.GerarExcel(linhas);

            // Salvar resultado
            var caminhoBase = Path.Combine(
                AppContext.BaseDirectory,
                "temp", "resultados");
            Directory.CreateDirectory(caminhoBase);
            
            var caminhoResultado = Path.Combine(caminhoBase, $"{jobId}.xlsx");
            await File.WriteAllBytesAsync(caminhoResultado, excelBytes, ct);

            // Marcar como concluído
            await jobService.MarcarJobComoConcluidoAsync(jobId, caminhoResultado, linhas.Count(l => l.FoiEncontrado));
            jobService.RemoverArquivoTemporario(jobId);

            _logger.LogInformation(
                "Job {JobId} concluído. {Encontrados} encontrados, {Pendencias} pendências",
                jobId,
                linhas.Count(l => l.FoiEncontrado),
                linhas.Count(l => !l.FoiEncontrado));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar job {JobId}", jobId);
            await jobService.MarcarJobComoErroAsync(jobId, ex.Message);
        }
    }
}