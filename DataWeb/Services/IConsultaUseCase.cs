using DataWeb.Domain;

namespace DataWeb.Services;

public interface IConsultaUseCase
{
    Task<IReadOnlyList<RespostaCnj>> ConsultarJsonAsync(Stream xlsxStream, int paralelismo, CancellationToken ct);
    Task<IReadOnlyList<RespostaCnj>> ConsultarPorCnjsAsync(
        IEnumerable<string> cnjs,
        int paralelismo,
        CancellationToken ct,
        IProgress<int>? progressoCnjs = null);
}
