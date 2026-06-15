using DataWeb.Domain.Entities;
using DataWeb.Infrastructure.Repositories;
using DataWeb.Models;
using DataWeb.Parsers;

namespace DataWeb.Services;

public class ApiJsonJobBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ApiJsonJobBackgroundService> _logger;
    private readonly SemaphoreSlim _semaphore;

    public ApiJsonJobBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ApiJsonJobBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        var maxConcorrencia = configuration.GetValue<int>("Processamento:MaxConcorrencia", 2);
        _semaphore = new SemaphoreSlim(maxConcorrencia);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessarProximoJobAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no processamento de jobs JSON");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcessarProximoJobAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var pending = await repository.GetPendingApiJsonJobsAsync();
        var job = pending.FirstOrDefault();
        if (job == null) return;

        await _semaphore.WaitAsync(ct);
        _ = Task.Run(async () =>
        {
            try
            {
                await ProcessarJobAsync(job.Id, ct);
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
        var jobService = scope.ServiceProvider.GetRequiredService<IApiJsonJobService>();
        var useCase = scope.ServiceProvider.GetRequiredService<IConsultaUseCase>();
        var parser = scope.ServiceProvider.GetRequiredService<IDatajudParser>();
        var options = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ProcessamentoOptions>>();

        if (!await jobService.TentarMarcarComoProcessandoAsync(jobId, ct)) return;

        _logger.LogInformation("Iniciando job JSON {JobId}", jobId);

        try
        {
            var cnjs = await jobService.ObterCnjsDoJobAsync(jobId, ct);
            if (cnjs.Count == 0)
            {
                await jobService.MarcarComoErroAsync(jobId, "Nenhum CNJ encontrado no job", ct);
                return;
            }

            var progresso = new Progress<int>(cnjsProcessados =>
            {
                _ = AtualizarProgressoAsync(jobId, cnjsProcessados);
            });

            var respostasJson = await useCase.ConsultarPorCnjsAsync(
                cnjs,
                options.Value.Paralelismo,
                CancellationToken.None,
                progresso);

            var linhas = parser.ExtrairLinhas(respostasJson);
            var resultado = new ProcessarCnjsJsonResponse(
                TotalCnjsEnviados: cnjs.Count,
                TotalLinhas: linhas.Count,
                Processos: linhas);

            await jobService.MarcarComoConcluidoAsync(jobId, resultado, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar job JSON {JobId}", jobId);
            await jobService.MarcarComoErroAsync(jobId, ex.Message, ct);
        }
    }

    private async Task AtualizarProgressoAsync(string jobId, int cnjsProcessados)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var jobService = scope.ServiceProvider.GetRequiredService<IApiJsonJobService>();
            await jobService.AtualizarProgressoAsync(jobId, cnjsProcessados);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao atualizar progresso do job {JobId}", jobId);
        }
    }
}
