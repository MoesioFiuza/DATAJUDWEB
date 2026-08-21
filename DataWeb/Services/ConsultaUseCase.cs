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

    public async Task<IReadOnlyList<RespostaCnj>> ConsultarJsonAsync(Stream xlsxStream, int paralelismo, CancellationToken ct)
    {
        var cnjs = _reader.LerCnjs(xlsxStream);
        return await ConsultarPorCnjsAsync(cnjs, paralelismo, ct);
    }

    public async Task<IReadOnlyList<RespostaCnj>> ConsultarPorCnjsAsync(
        IEnumerable<string> cnjs,
        int paralelismo,
        CancellationToken ct,
        IProgress<int>? progressoCnjs = null)
    {
        var porEstado = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var naoConsultados = new List<RespostaCnj>();

        var concluidos = 0;
        void ReportarConclusao()
        {
            var atual = Interlocked.Increment(ref concluidos);
            progressoCnjs?.Report(atual);
        }

        foreach (var cnj in cnjs)
        {
            if (!CnjParse.TryObterCodigo(cnj, out var codigo))
            {
                naoConsultados.Add(RespostaCnj.Invalido(cnj, "CNJ em formato inválido"));
                ReportarConclusao();
                continue;
            }

            if (!CnjMaps.CodigoEstado.TryGetValue(codigo, out var estado))
            {
                naoConsultados.Add(RespostaCnj.Invalido(cnj, $"Tribunal {codigo} não mapeado"));
                ReportarConclusao();
                continue;
            }

            if (!porEstado.TryGetValue(estado, out var lista))
                porEstado[estado] = lista = new List<string>();
            lista.Add(cnj);
        }

        if (porEstado.Count == 0)
            return naoConsultados;

        var tasks = porEstado.Select(kv =>
            _client.ConsultarPorEstadoAsync(kv.Key, kv.Value, paralelismo, ct, ReportarConclusao));
        var respostasPorEstado = await Task.WhenAll(tasks);

        return naoConsultados
            .Concat(respostasPorEstado.SelectMany(x => x))
            .ToList();
    }
}
