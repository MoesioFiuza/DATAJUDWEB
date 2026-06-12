using System.Text.Json;
using DataWeb.Domain.Entities;
using DataWeb.Infrastructure.Repositories;
using DataWeb.Models;

namespace DataWeb.Services;

public class ApiJsonJobService : IApiJsonJobService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IJobRepository _repository;
    private readonly ILogger<ApiJsonJobService> _logger;

    public ApiJsonJobService(IJobRepository repository, ILogger<ApiJsonJobService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ApiJsonJobCreatedResponse> CriarJobAsync(IReadOnlyList<string> cnjs, CancellationToken ct = default)
    {
        var job = new DataJudJob
        {
            Id = Guid.NewGuid().ToString(),
            UserId = 0,
            FileName = "api-json",
            OriginalFileSize = 0,
            JobKind = DataJudJobKind.ApiJson,
            InputCnjsJson = JsonSerializer.Serialize(cnjs, JsonOptions),
            Status = DataJudJobStatus.Pendente,
            TotalProcessos = cnjs.Count,
            ProcessosProcessados = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.CreateAsync(job);

        _logger.LogInformation("Job JSON criado: {JobId} com {Count} CNJs", job.Id, cnjs.Count);

        return new ApiJsonJobCreatedResponse(
            job.Id,
            job.Status,
            cnjs.Count,
            "Job criado e aguardando processamento");
    }

    public async Task<ApiJsonJobStatusResponse?> ObterStatusAsync(string jobId, CancellationToken ct = default)
    {
        var job = await _repository.GetByIdAsync(jobId);
        if (job == null || job.JobKind != DataJudJobKind.ApiJson) return null;

        return MapStatus(job);
    }

    public async Task<ProcessarCnjsJsonResponse?> ObterResultadoAsync(string jobId, CancellationToken ct = default)
    {
        var job = await _repository.GetByIdAsync(jobId);
        if (job == null || job.JobKind != DataJudJobKind.ApiJson) return null;
        if (job.Status != DataJudJobStatus.Concluido || string.IsNullOrWhiteSpace(job.ResultJson)) return null;

        return JsonSerializer.Deserialize<ProcessarCnjsJsonResponse>(job.ResultJson, JsonOptions);
    }

    public async Task<bool> TentarMarcarComoProcessandoAsync(string jobId, CancellationToken ct = default)
    {
        var job = await _repository.GetByIdAsync(jobId);
        if (job == null || job.JobKind != DataJudJobKind.ApiJson) return false;
        if (job.Status != DataJudJobStatus.Pendente) return false;

        job.Status = DataJudJobStatus.Processando;
        job.StartedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(job);
        return true;
    }

    public async Task MarcarComoConcluidoAsync(string jobId, ProcessarCnjsJsonResponse resultado, CancellationToken ct = default)
    {
        var job = await _repository.GetByIdAsync(jobId);
        if (job == null) return;

        job.Status = DataJudJobStatus.Concluido;
        job.CompletedAt = DateTime.UtcNow;
        job.ResultJson = JsonSerializer.Serialize(resultado, JsonOptions);
        job.TotalProcessos = resultado.TotalCnjsEnviados;
        job.ProcessosProcessados = resultado.TotalLinhas;

        await _repository.UpdateAsync(job);
        _logger.LogInformation("Job JSON {JobId} concluído com {Linhas} linhas", jobId, resultado.TotalLinhas);
    }

    public async Task MarcarComoErroAsync(string jobId, string erro, CancellationToken ct = default)
    {
        var job = await _repository.GetByIdAsync(jobId);
        if (job == null) return;

        job.Status = DataJudJobStatus.Erro;
        job.ErrorMessage = erro;
        job.CompletedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(job);

        _logger.LogError("Job JSON {JobId} falhou: {Erro}", jobId, erro);
    }

    public async Task<IReadOnlyList<string>> ObterCnjsDoJobAsync(string jobId, CancellationToken ct = default)
    {
        var job = await _repository.GetByIdAsync(jobId);
        if (job == null || string.IsNullOrWhiteSpace(job.InputCnjsJson))
            return Array.Empty<string>();

        return JsonSerializer.Deserialize<List<string>>(job.InputCnjsJson, JsonOptions) ?? [];
    }

    private static ApiJsonJobStatusResponse MapStatus(DataJudJob job)
    {
        return new ApiJsonJobStatusResponse(
            job.Id,
            job.Status,
            job.TotalProcessos,
            job.Status == DataJudJobStatus.Concluido ? job.ProcessosProcessados : 0,
            job.Progresso,
            job.CreatedAt,
            job.StartedAt,
            job.CompletedAt,
            job.ErrorMessage);
    }
}
