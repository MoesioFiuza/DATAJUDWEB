namespace DataWeb.Models;

public record ApiJsonJobCreatedResponse(
    string JobId,
    string Status,
    int TotalCnjs,
    string Message);

public record ApiJsonJobStatusResponse(
    string JobId,
    string Status,
    int TotalCnjs,
    int TotalLinhas,
    int Progresso,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? Erro);
