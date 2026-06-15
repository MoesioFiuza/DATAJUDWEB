namespace DataWeb.Services;

public interface IDataJudClient
{
    Task<IReadOnlyList<string>> ConsultarPorEstadoAsync(
        string estado,
        IEnumerable<string> cnjs,
        int paralelismo,
        CancellationToken ct,
        Action? onCnjConsultado = null);
}