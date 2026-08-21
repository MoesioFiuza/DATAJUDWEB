using System.Text.Json;
using DataWeb.Domain;
using DataWeb.Domain.Entities;

namespace DataWeb.Parsers;

public class DatajudParser : IDatajudParser
{
    public List<LinhaProcesso> ExtrairLinhas(IEnumerable<RespostaCnj> respostas)
    {
        var linhas = new List<LinhaProcesso>();

        foreach (var resp in respostas)
        {
            linhas.AddRange(ExtrairDeResposta(resp));
        }

        return linhas
            .OrderBy(l => l.NumeroProcesso)
            .ThenBy(l => l.FoiEncontrado ? 0 : 1)
            .ThenBy(l => l.OrdemMovimentacao)
            .ToList();
    }

    private static List<LinhaProcesso> ExtrairDeResposta(RespostaCnj resp)
    {
        if (resp.StatusPrevio == DataJudProcessoStatus.Invalido)
        {
            return
            [
                LinhaProcesso.Placeholder(
                    resp.Cnj,
                    DataJudProcessoStatus.Invalido,
                    resp.Erro ?? "CNJ inválido ou tribunal não mapeado")
            ];
        }

        if (resp.StatusPrevio == DataJudProcessoStatus.Erro)
        {
            return
            [
                LinhaProcesso.Placeholder(
                    resp.Cnj,
                    DataJudProcessoStatus.Erro,
                    resp.Erro ?? "Erro ao consultar o DataJud")
            ];
        }

        if (resp.HttpStatus is >= 400)
        {
            return
            [
                LinhaProcesso.Placeholder(
                    resp.Cnj,
                    DataJudProcessoStatus.Erro,
                    resp.Erro ?? $"HTTP {resp.HttpStatus} na consulta ao DataJud")
            ];
        }

        if (string.IsNullOrWhiteSpace(resp.Json))
        {
            return
            [
                LinhaProcesso.Placeholder(resp.Cnj, DataJudProcessoStatus.Erro, "Resposta vazia do DataJud")
            ];
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(resp.Json);
        }
        catch (JsonException ex)
        {
            return
            [
                LinhaProcesso.Placeholder(
                    resp.Cnj,
                    DataJudProcessoStatus.Erro,
                    $"JSON inválido: {ex.Message}")
            ];
        }

        using (doc)
        {
            if (!doc.RootElement.TryGetProperty("hits", out var hits) || hits.ValueKind != JsonValueKind.Object
                || !hits.TryGetProperty("hits", out var arr) || arr.ValueKind != JsonValueKind.Array)
            {
                return
                [
                    LinhaProcesso.Placeholder(
                        resp.Cnj,
                        DataJudProcessoStatus.Erro,
                        "Resposta do DataJud sem hits")
                ];
            }

            if (arr.GetArrayLength() == 0)
            {
                return
                [
                    LinhaProcesso.Placeholder(
                        resp.Cnj,
                        DataJudProcessoStatus.NaoEncontrado,
                        "Processo não encontrado no DataJud")
                ];
            }

            var extraidas = ExtrairHits(arr);
            if (extraidas.Count == 0)
            {
                return
                [
                    LinhaProcesso.Placeholder(
                        resp.Cnj,
                        DataJudProcessoStatus.NaoEncontrado,
                        "Processo não encontrado no DataJud")
                ];
            }

            return extraidas;
        }
    }

    private static List<LinhaProcesso> ExtrairHits(JsonElement arr)
    {
        var linhas = new List<LinhaProcesso>();
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

        return linhas;
    }
}

static class JsonExt
{
    public static string GetPropertyOrDefault(this JsonElement el, string prop)
    {
        return el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
    }
}
