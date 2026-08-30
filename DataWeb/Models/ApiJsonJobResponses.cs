namespace DataWeb.Models;

public record ApiJsonJobCreatedResponse(
    string JobId,
    string Status,
    int TotalCnjs,
    string Message);

/// <summary>
/// Status do job JSON. Progresso = percentual de CNJs consultados (0–100).
/// TotalLinhas só é preenchido quando status = concluido.
/// TotalEncontrados / TotalNaoEncontrados / TotalErros só quando concluido.
/// </summary>
public record ApiJsonJobStatusResponse(
    string JobId,
    string Status,
    int TotalCnjs,
    int CnjsProcessados,
    int TotalLinhas,
    int Progresso,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? Erro,
    int TotalEncontrados = 0,
    int TotalNaoEncontrados = 0,
    int TotalErros = 0);

public record ServiceStatusResponse(
    string Status,
    ServiceStatusDetails Services,
    DateTime Timestamp);

public record ServiceStatusDetails(
    string Api,
    string Database,
    bool DataJudConfigured);
