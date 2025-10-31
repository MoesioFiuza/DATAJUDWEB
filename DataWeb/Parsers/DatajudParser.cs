using System.Text.Json;
using DataWeb.Domain;

namespace DataWeb.Parsers;

public class DatajudParser : IDatajudParser
{
    public List<LinhaProcesso> ExtrairLinhas(IEnumerable<string> respostasJson)
    {
        var linhas = new List<LinhaProcesso>();

        foreach (var json in respostasJson)
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("hits", out var hits) || hits.ValueKind != JsonValueKind.Object) continue;
            if (!hits.TryGetProperty("hits", out var arr) || arr.ValueKind != JsonValueKind.Array) continue;

            foreach (var proc in arr.EnumerateArray())
            {
                if (!proc.TryGetProperty("_source", out var src)) continue;

                var baseLinha = LinhaProcesso.From(proc, src);

                if (src.TryGetProperty("movimentos", out var movsEl) && movsEl.ValueKind == JsonValueKind.Array && movsEl.GetArrayLength() > 0)
                {
                    var ordenados = movsEl.EnumerateArray()
                        .Select(x => new { El = x, Data = x.GetPropertyOrDefault("dataHora") })
                        .OrderByDescending(x => x.Data ?? "1900-01-01T00:00:00.000Z")
                        .ToList();

                    for (int i = 0; i < ordenados.Count; i++)
                    {
                        var l = baseLinha.Clone();
                        l.PreencherMovimento(ordenados[i].El, i == 0, i + 1);
                        linhas.Add(l);
                    }
                }
                else
                {
                    linhas.Add(baseLinha);
                }
            }
        }

        return linhas
            .OrderBy(l => l.NumeroProcesso)
            .ThenBy(l => l.OrdemMovimentacao)
            .ToList();
    }
}

static class JsonExt
{
    public static string GetPropertyOrDefault(this JsonElement el, string prop)
    {
        return el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
    }
}