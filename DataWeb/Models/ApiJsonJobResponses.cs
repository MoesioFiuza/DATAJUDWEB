namespace DataWeb.Models;

public record ApiJsonJobCreatedResponse(
    string JobId,
    string Status,
    int TotalCnjs,
    string Message);

/// <summary>
/// Status do job JSON. Progresso = percentual de CNJs consultados (0–100).
/// TotalLinhas só é preenchido quando status = concluido.
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
    string? Erro);

public record ServiceStatusResponse(
    string Status,
    ServiceStatusDetails Services,
    DateTime Timestamp);

public record ServiceStatusDetails(
    string Api,
    string Database,
    bool DataJudConfigured);
