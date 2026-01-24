using System.Text.Json;

namespace DataWeb.Domain;

public class LinhaProcesso
{
    public string ID_Datajud { get; set; } = "";
    public string NumeroProcesso { get; set; } = "";
    public string Tribunal { get; set; } = "";
    public string Grau { get; set; } = "";
    public string NivelSigilo { get; set; } = "";
    public string CClasse_Codigo { get; set; } = "";
    public string CClasse_Nome { get; set; } = "";
    public string SSistema_Codigo { get; set; } = "";
    public string SSistema_Nome { get; set; } = "";
    public string FFormato_Codigo { get; set; } = "";
    public string FFormato_Nome { get; set; } = "";
    public string DataAjuizamento { get; set; } = "";
    public string DataHoraMovimentacao { get; set; } = "";
    public string Orgao_Codigo { get; set; } = "";
    public string Orgao_Nome { get; set; } = "";
    public string Orgao_Municipio_IBGE { get; set; } = "";
    public string Mov_Orgao_Codigo { get; set; } = "";
    public string Mov_Orgao_Nome { get; set; } = "";
    public string Assuntos { get; set; } = "";
    public string Movimento_Codigo { get; set; } = "";
    public string Movimento_Nome { get; set; } = "";
    public string Complementos_Tabelados { get; set; } = "";
    public string EhMaisRecente { get; set; } = "N/A";
    public int OrdemMovimentacao { get; set; } = 0;

    public LinhaProcesso Clone() => (LinhaProcesso)MemberwiseClone();

    public static LinhaProcesso From(JsonElement proc, JsonElement src)
    {
        string GetStr(JsonElement e, string p) => e.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
        string GetNum(JsonElement e, string p) => e.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.Number ? v.ToString() : GetStr(e, p);

        var l = new LinhaProcesso
        {
            ID_Datajud = proc.TryGetProperty("_id", out var id) ? id.GetString() ?? "" : "",
            NumeroProcesso = GetStr(src, "numeroProcesso"),
            Tribunal = GetStr(src, "tribunal"),
            Grau = GetStr(src, "grau"),
            NivelSigilo = GetNum(src, "nivelSigilo"),
            CClasse_Codigo = src.TryGetProperty("classe", out var c1) ? GetNum(c1, "codigo") : "",
            CClasse_Nome = src.TryGetProperty("classe", out var c2) ? GetStr(c2, "nome") : "",
            SSistema_Codigo = src.TryGetProperty("sistema", out var s1) ? GetNum(s1, "codigo") : "",
            SSistema_Nome = src.TryGetProperty("sistema", out var s2) ? GetStr(s2, "nome") : "",
            FFormato_Codigo = src.TryGetProperty("formato", out var f1) ? GetNum(f1, "codigo") : "",
            FFormato_Nome = src.TryGetProperty("formato", out var f2) ? GetStr(f2, "nome") : "",
            DataAjuizamento = FormatarData(GetStr(src, "dataAjuizamento")),
            Orgao_Codigo = src.TryGetProperty("orgaoJulgador", out var o1) ? GetNum(o1, "codigo") : "",
            Orgao_Nome = src.TryGetProperty("orgaoJulgador", out var o2) ? GetStr(o2, "nome") : "",
            Orgao_Municipio_IBGE = src.TryGetProperty("orgaoJulgador", out var o3) ? GetNum(o3, "codigoMunicipioIBGE") : "",
            Assuntos = string.Join("; ", ExtrairAssuntos(src))
        };
        return l;
    }

    public void PreencherMovimento(JsonElement mov, bool maisRecente, int ordem)
    {
        string GetStr(JsonElement e, string p) => e.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
        string GetNum(JsonElement e, string p) => e.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.Number ? v.ToString() : GetStr(e, p);

        DataHoraMovimentacao = FormatarData(GetStr(mov, "dataHora"));
        Movimento_Nome = GetStr(mov, "nome");
        Movimento_Codigo = GetNum(mov, "codigo");

        if (mov.TryGetProperty("orgaoJulgador", out var mo))
        {
            Mov_Orgao_Codigo = GetNum(mo, "codigoOrgao");
            Mov_Orgao_Nome = GetStr(mo, "nomeOrgao");
        }

        Complementos_Tabelados = ExtrairComplementos(mov);
        EhMaisRecente = maisRecente ? "Sim" : "Não";
        OrdemMovimentacao = ordem;
    }

    public static List<string> ExtrairAssuntos(JsonElement src)
    {
        var assuntos = new List<string>();
        
        try
        {
            // Verifica se existe a propriedade 'assuntos'
            if (src.TryGetProperty("assuntos", out JsonElement assuntosElement))
            {
                // Se for um array
                if (assuntosElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var assunto in assuntosElement.EnumerateArray())
                    {
                        if (assunto.TryGetProperty("nome", out JsonElement nomeElement))
                        {
                            assuntos.Add(nomeElement.GetString() ?? "");
                        }
                    }
                }
                // Se for um objeto
                else if (assuntosElement.ValueKind == JsonValueKind.Object)
                {
                    if (assuntosElement.TryGetProperty("nome", out JsonElement nomeElement))
                    {
                        assuntos.Add(nomeElement.GetString() ?? "");
                    }
                }
            }
        }
        catch (Exception)
        {
            // Em caso de erro, retorna lista vazia
            return new List<string>();
        }
        
        return assuntos;
    }

    private static string ExtrairComplementos(JsonElement mov)
    {
        if (!mov.TryGetProperty("complementosTabelados", out var arr) || arr.ValueKind != JsonValueKind.Array) return "";
        var itens = new List<string>();
        foreach (var c in arr.EnumerateArray())
        {
            var cod = c.TryGetProperty("codigo", out var cc) && cc.ValueKind == JsonValueKind.Number ? cc.ToString() : (c.TryGetProperty("codigo", out var cc2) ? cc2.GetString() ?? "" : "");
            var nome = c.TryGetProperty("nome", out var nn) ? (nn.GetString() ?? "") : "";
            var desc = c.TryGetProperty("descricao", out var dd) ? (dd.GetString() ?? "") : "";
            var val = c.TryGetProperty("valor", out var vv) ? (vv.ToString()) : "";
            itens.Add($"{cod} - {nome} ({desc}: {val})");
        }
        return string.Join("; ", itens);
    }

    private static string FormatarData(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        try
        {
            if (DateTimeOffset.TryParse(s, out var dto))
                return dto.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
            return s;
        }
        catch { return s; }
    }
}