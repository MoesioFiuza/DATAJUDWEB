using DataWeb.Domain;
using DataWeb.Utils;

namespace DataWeb.Services;

public class ConsultaUseCase : IConsultaUseCase
{
    private readonly IExcelReader _reader;
    private readonly IDataJudClient _client;

    public ConsultaUseCase(IExcelReader reader, IDataJudClient client)
    {
        _reader = reader; _client = client;
    }

    public async Task<IReadOnlyList<string>> ConsultarJsonAsync(Stream xlsxStream, int paralelismo, CancellationToken ct)
    {
        var cnjs = _reader.LerCnjs(xlsxStream);
        return await ConsultarPorCnjsAsync(cnjs, paralelismo, ct);
    }

    public async Task<IReadOnlyList<string>> ConsultarPorCnjsAsync(IEnumerable<string> cnjs, int paralelismo, CancellationToken ct)
    {
        var porEstado = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var cnj in cnjs)
        {
            if (!CnjParse.TryObterCodigo(cnj, out var codigo)) continue;
            if (!CnjMaps.CodigoEstado.TryGetValue(codigo, out var estado)) continue;

            if (!porEstado.TryGetValue(estado, out var lista))
                porEstado[estado] = lista = new List<string>();
            lista.Add(cnj);
        }

        var tasks = porEstado.Select(kv => _client.ConsultarPorEstadoAsync(kv.Key, kv.Value, paralelismo, ct));
        var respostasPorEstado = await Task.WhenAll(tasks);
        return respostasPorEstado.SelectMany(x => x).ToList();
    }
}
