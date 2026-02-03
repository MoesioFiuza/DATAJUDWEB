using ClosedXML.Excel;
using DataWeb.Domain;

namespace DataWeb.Exporters;

public class ExcelExporter : IExcelExporter
{
    private static readonly string[] Cab = new[]
    {
        "ID_Datajud","Número do Processo","Tribunal","Grau","Nível Sigilo",
        "Classe_Código","Classe_Nome","Sistema_Código","Sistema_Nome","Formato_Código","Formato_Nome",
        "Data Ajuizamento","Data/Hora Movimentação",
        "Órgão_Código","Órgão_Nome","Órgão_Município_IBGE",
        "Movimento_Órgão_Código","Movimento_Órgão_Nome",
        "Assuntos","Movimento_Código","Movimento_Nome","Complementos_Tabelados",
        "É Mais Recente","Ordem Movimentação"
    };

    public byte[] GerarExcel(List<LinhaProcesso> linhas)
    {
        using var wb = new XLWorkbook();

        var wsDados = wb.Worksheets.Add("Dados Completos");
        WriteHeader(wsDados, Cab);
        WriteRows(wsDados, linhas);

        var wsRecentes = wb.Worksheets.Add("Movimentações Recentes");
        WriteHeader(wsRecentes, Cab);
        WriteRows(wsRecentes, linhas.Where(l => l.EhMaisRecente == "Sim").ToList());

        var wsResumo = wb.Worksheets.Add("Resumo Processos");
        WriteResumo(wsResumo, linhas);

        var wsAnalise = wb.Worksheets.Add("Análise Movimentações");
        WriteAnalise(wsAnalise, linhas);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void WriteHeader(IXLWorksheet ws, string[] cab)
    {
        for (int i = 0; i < cab.Length; i++) ws.Cell(1, i + 1).Value = cab[i];
        ws.Range(1, 1, 1, cab.Length).Style.Font.Bold = true;
    }

    private static void WriteRows(IXLWorksheet ws, List<LinhaProcesso> ls)
    {
        int r = 2;
        foreach (var l in ls)
        {
            ws.Cell(r, 1).Value = l.ID_Datajud;
            ws.Cell(r, 2).Value = l.NumeroProcesso;
            ws.Cell(r, 3).Value = l.Tribunal;
            ws.Cell(r, 4).Value = l.Grau;
            ws.Cell(r, 5).Value = l.NivelSigilo;
            ws.Cell(r, 6).Value = l.CClasse_Codigo;
            ws.Cell(r, 7).Value = l.CClasse_Nome;
            ws.Cell(r, 8).Value = l.SSistema_Codigo;
            ws.Cell(r, 9).Value = l.SSistema_Nome;
            ws.Cell(r, 10).Value = l.FFormato_Codigo;
            ws.Cell(r, 11).Value = l.FFormato_Nome;
            ws.Cell(r, 12).Value = l.DataAjuizamento;
            ws.Cell(r, 13).Value = l.DataHoraMovimentacao;
            ws.Cell(r, 14).Value = l.Orgao_Codigo;
            ws.Cell(r, 15).Value = l.Orgao_Nome;
            ws.Cell(r, 16).Value = l.Orgao_Municipio_IBGE;
            ws.Cell(r, 17).Value = l.Mov_Orgao_Codigo;
            ws.Cell(r, 18).Value = l.Mov_Orgao_Nome;
            ws.Cell(r, 19).Value = l.Assuntos;
            ws.Cell(r, 20).Value = l.Movimento_Codigo;
            ws.Cell(r, 21).Value = l.Movimento_Nome;
            ws.Cell(r, 22).Value = l.Complementos_Tabelados;
            ws.Cell(r, 23).Value = l.EhMaisRecente;
            ws.Cell(r, 24).Value = l.OrdemMovimentacao;
            r++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void WriteResumo(IXLWorksheet ws, List<LinhaProcesso> ls)
    {
        var cabResumo = new[]
        {
            "Número do Processo","ID Datajud","Tribunal","Grau","Classe","Sistema","Formato","Data Ajuizamento","Órgão Julgador","Assuntos","Total Movimentações"
        };
        WriteHeader(ws, cabResumo);

        var rows = ls.GroupBy(l => l.NumeroProcesso)
            .Select(g => new
            {
                Numero = g.Key,
                Id = g.Select(x => x.ID_Datajud).FirstOrDefault() ?? "",
                Tribunal = g.Select(x => x.Tribunal).FirstOrDefault() ?? "",
                Grau = g.Select(x => x.Grau).FirstOrDefault() ?? "",
                Classe = g.Select(x => x.CClasse_Nome).FirstOrDefault() ?? "",
                Sistema = g.Select(x => x.SSistema_Nome).FirstOrDefault() ?? "",
                Formato = g.Select(x => x.FFormato_Nome).FirstOrDefault() ?? "",
                Ajuiz = g.Select(x => x.DataAjuizamento).FirstOrDefault() ?? "",
                Orgao = g.Select(x => x.Orgao_Nome).FirstOrDefault() ?? "",
                Assuntos = g.Select(x => x.Assuntos).FirstOrDefault() ?? "",
                Total = g.Max(x => x.OrdemMovimentacao)
            }).ToList();

        int r = 2;
        foreach (var row in rows)
        {
            ws.Cell(r, 1).Value = row.Numero;
            ws.Cell(r, 2).Value = row.Id;
            ws.Cell(r, 3).Value = row.Tribunal;
            ws.Cell(r, 4).Value = row.Grau;
            ws.Cell(r, 5).Value = row.Classe;
            ws.Cell(r, 6).Value = row.Sistema;
            ws.Cell(r, 7).Value = row.Formato;
            ws.Cell(r, 8).Value = row.Ajuiz;
            ws.Cell(r, 9).Value = row.Orgao;
            ws.Cell(r, 10).Value = row.Assuntos;
            ws.Cell(r, 11).Value = row.Total;
            r++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void WriteAnalise(IXLWorksheet ws, List<LinhaProcesso> ls)
    {
        var cab = new[] { "Movimento_Nome", "Tribunal", "Quantidade" };
        WriteHeader(ws, cab);

        var rows = ls.GroupBy(l => new { l.Movimento_Nome, l.Tribunal })
            .Select(g => new { g.Key.Movimento_Nome, g.Key.Tribunal, Quantidade = g.Count() })
            .OrderByDescending(x => x.Quantidade)
            .ToList();

        int r = 2;
        foreach (var row in rows)
        {
            ws.Cell(r, 1).Value = row.Movimento_Nome;
            ws.Cell(r, 2).Value = row.Tribunal;
            ws.Cell(r, 3).Value = row.Quantidade;
            r++;
        }
        ws.Columns().AdjustToContents();
    }
}