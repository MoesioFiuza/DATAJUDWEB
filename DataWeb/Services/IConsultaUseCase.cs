namespace DataWeb.Services;

public interface IConsultaUseCase
{
    Task<IReadOnlyList<string>> ConsultarJsonAsync(Stream xlsxStream, int paralelismo, CancellationToken ct);
}