using DataWeb.Domain;

namespace DataWeb.Services;

public interface IDataJudClient
{
    Task<IReadOnlyList<RespostaCnj>> ConsultarPorEstadoAsync(
        string estado,
        IEnumerable<string> cnjs,
        int paralelismo,
        CancellationToken ct,
        Action? onCnjConsultado = null);
}
