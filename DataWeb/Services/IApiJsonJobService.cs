using DataWeb.Models;

namespace DataWeb.Services;

public interface IApiJsonJobService
{
    Task<ApiJsonJobCreatedResponse> CriarJobAsync(IReadOnlyList<string> cnjs, CancellationToken ct = default);
    Task<ApiJsonJobStatusResponse?> ObterStatusAsync(string jobId, CancellationToken ct = default);
    Task<ProcessarCnjsJsonResponse?> ObterResultadoAsync(string jobId, CancellationToken ct = default);
    Task<bool> TentarMarcarComoProcessandoAsync(string jobId, CancellationToken ct = default);
    Task MarcarComoConcluidoAsync(string jobId, ProcessarCnjsJsonResponse resultado, CancellationToken ct = default);
    Task MarcarComoErroAsync(string jobId, string erro, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ObterCnjsDoJobAsync(string jobId, CancellationToken ct = default);
}
