namespace DataWeb.Services;

public interface IConsultaUseCase
{
    Task<IReadOnlyList<string>> ConsultarJsonAsync(Stream xlsxStream, int paralelismo, CancellationToken ct);
    Task<IReadOnlyList<string>> ConsultarPorCnjsAsync(
        IEnumerable<string> cnjs,
        int paralelismo,
        CancellationToken ct,
        IProgress<int>? progressoCnjs = null);
}