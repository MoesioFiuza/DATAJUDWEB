using DataWeb.Domain;

namespace DataWeb.Models;

public record ProcessarCnjsJsonResponse(
    int TotalCnjsEnviados,
    int TotalLinhas,
    IReadOnlyList<LinhaProcesso> Processos);
