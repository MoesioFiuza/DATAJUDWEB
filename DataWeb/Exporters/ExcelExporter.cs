using ClosedXML.Excel;
using DataWeb.Domain;
using DataWeb.Domain.Entities;
using DataWeb.Utils;

namespace DataWeb.Exporters;

public class ExcelExporter : IExcelExporter
{
    private static readonly string[] CabLegado =
    {
        "ID_Datajud","Número do Processo","Tribunal","Grau","Nível Sigilo",
        "Classe_Código","Classe_Nome","Sistema_Código","Sistema_Nome","Formato_Código","Formato_Nome",
        "Data Ajuizamento","Data/Hora Movimentação",
        "Órgão_Código","Órgão_Nome","Órgão_Município_IBGE",
        "Movimento_Órgão_Código","Movimento_Órgão_Nome",
        "Assuntos","Movimento_Código","Movimento_Nome","Complementos_Tabelados",
        "É Mais Recente","Ordem Movimentação"
    };

    private static readonly string[] CabDadosCompletosFixo =
    {
        "Número CNJ",
        "Status",
        "Motivo",
        "Tribunal",
        "Grau",
        "Ramo da Justiça",
        "Nível de Sigilo",
        "Classe",
        "Sistema",
        "Formato",
        "Órgão Julgador",
        "Assuntos",
        "Data Ajuizamento",
        "Total Movimentações",
        "ID Datajud"
    };

    private const int ColunasFixasDadosCompletos = 15;
    private const int ColunasPorMovimentacao = 3;

    public byte[] GerarExcel(List<LinhaProcesso> linhas)
    {
        using var wb = new XLWorkbook();

        var wsDados = wb.Worksheets.Add("Dados Completos");
        WriteDadosCompletosWide(wsDados, linhas);

        var wsPendencias = wb.Worksheets.Add("Não encontrados");
        WriteNaoEncontrados(wsPendencias, linhas);

        var wsRecentes = wb.Worksheets.Add("Movimentações Recentes");
        WriteHeader(wsRecentes, CabLegado);
        WriteRowsLegado(wsRecentes, linhas.Where(l => l.FoiEncontrado && l.EhMaisRecente == "Sim").ToList());

        var wsResumo = wb.Worksheets.Add("Resumo Processos");
        WriteResumo(wsResumo, linhas);

        var wsAnalise = wb.Worksheets.Add("Análise Movimentações");
        WriteAnalise(wsAnalise, linhas);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void WriteDadosCompletosWide(IXLWorksheet ws, List<LinhaProcesso> linhas)
    {
        var grupos = linhas
            .GroupBy(l => CnjFormat.ChaveCnj(l.NumeroProcesso))
            .Select(g => new ProcessoAgrupado(g.Key, g.ToList()))
            .OrderBy(p => p.FoiEncontrado ? 0 : 1)
            .ThenBy(p => p.NumeroProcesso)
            .ToList();

        var maxMovimentacoes = grupos.Count == 0
            ? 0
            : grupos.Max(p => p.Movimentacoes.Count);

        WriteHeaderDadosCompletos(ws, maxMovimentacoes);

        int row = 2;
        foreach (var processo in grupos)
        {
            WriteLinhaProcessoWide(ws, row, processo, maxMovimentacoes);
            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private static void WriteHeaderDadosCompletos(IXLWorksheet ws, int maxMovimentacoes)
    {
        for (int i = 0; i < CabDadosCompletosFixo.Length; i++)
            ws.Cell(1, i + 1).Value = CabDadosCompletosFixo[i];

        int col = ColunasFixasDadosCompletos + 1;
        for (int m = 1; m <= maxMovimentacoes; m++)
        {
            ws.Cell(1, col).Value = $"Mov {m} - Data";
            ws.Cell(1, col + 1).Value = $"Mov {m} - Descrição";
            ws.Cell(1, col + 2).Value = $"Mov {m} - Complementos";
            col += ColunasPorMovimentacao;
        }

        var totalCols = ColunasFixasDadosCompletos + maxMovimentacoes * ColunasPorMovimentacao;
        if (totalCols > 0)
            ws.Range(1, 1, 1, totalCols).Style.Font.Bold = true;
    }

    private static void WriteLinhaProcessoWide(
        IXLWorksheet ws,
        int row,
        ProcessoAgrupado processo,
        int maxMovimentacoes)
    {
        var baseInfo = processo.Base;

        ws.Cell(row, 1).Value = CnjFormat.FormatarComMascara(processo.NumeroProcesso);
        ws.Cell(row, 2).Value = RotuloStatus(processo.Status);
        ws.Cell(row, 3).Value = processo.Motivo;
        PintarStatus(ws.Cell(row, 2), processo.Status);
        ws.Cell(row, 4).Value = baseInfo.Tribunal;
        ws.Cell(row, 5).Value = CnjFormat.ObterGrauTratado(baseInfo.Grau);
        ws.Cell(row, 6).Value = processo.FoiEncontrado
            ? CnjFormat.ObterRamoJustica(processo.NumeroProcesso)
            : "";
        ws.Cell(row, 7).Value = CnjFormat.ObterNivelSigiloTratado(baseInfo.NivelSigilo);
        ws.Cell(row, 8).Value = baseInfo.CClasse_Nome;
        ws.Cell(row, 9).Value = baseInfo.SSistema_Nome;
        ws.Cell(row, 10).Value = baseInfo.FFormato_Nome;
        ws.Cell(row, 11).Value = baseInfo.Orgao_Nome;
        ws.Cell(row, 12).Value = baseInfo.Assuntos;
        ws.Cell(row, 13).Value = baseInfo.DataAjuizamento;
        ws.Cell(row, 14).Value = processo.FoiEncontrado ? processo.Movimentacoes.Count : 0;
        ws.Cell(row, 15).Value = baseInfo.ID_Datajud;

        int col = ColunasFixasDadosCompletos + 1;
        foreach (var mov in processo.Movimentacoes)
        {
            ws.Cell(row, col).Value = mov.DataHoraMovimentacao;
            ws.Cell(row, col + 1).Value = mov.Movimento_Nome;
            ws.Cell(row, col + 2).Value = mov.Complementos_Tabelados;
            col += ColunasPorMovimentacao;
        }
    }

    private static void WriteHeader(IXLWorksheet ws, string[] cab)
    {
        for (int i = 0; i < cab.Length; i++) ws.Cell(1, i + 1).Value = cab[i];
        ws.Range(1, 1, 1, cab.Length).Style.Font.Bold = true;
    }

    private static void WriteRowsLegado(IXLWorksheet ws, List<LinhaProcesso> ls)
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
            "Número do Processo","Status","Motivo","ID Datajud","Tribunal","Grau","Classe","Sistema","Formato","Data Ajuizamento","Órgão Julgador","Assuntos","Total Movimentações"
        };
        WriteHeader(ws, cabResumo);

        var rows = ls.GroupBy(l => CnjFormat.ChaveCnj(l.NumeroProcesso))
            .Select(g =>
            {
                var primeiro = g.OrderBy(x => x.OrdemMovimentacao).First();
                return new
                {
                    Numero = CnjFormat.FormatarComMascara(g.Key),
                    Status = primeiro.FoiEncontrado ? DataJudProcessoStatus.Encontrado : primeiro.Status,
                    Motivo = primeiro.Motivo,
                    Id = g.Select(x => x.ID_Datajud).FirstOrDefault() ?? "",
                    Tribunal = g.Select(x => x.Tribunal).FirstOrDefault() ?? "",
                    Grau = g.Select(x => x.Grau).FirstOrDefault() ?? "",
                    Classe = g.Select(x => x.CClasse_Nome).FirstOrDefault() ?? "",
                    Sistema = g.Select(x => x.SSistema_Nome).FirstOrDefault() ?? "",
                    Formato = g.Select(x => x.FFormato_Nome).FirstOrDefault() ?? "",
                    Ajuiz = g.Select(x => x.DataAjuizamento).FirstOrDefault() ?? "",
                    Orgao = g.Select(x => x.Orgao_Nome).FirstOrDefault() ?? "",
                    Assuntos = g.Select(x => x.Assuntos).FirstOrDefault() ?? "",
                    Total = primeiro.FoiEncontrado ? g.Max(x => x.OrdemMovimentacao) : 0
                };
            })
            .OrderBy(x => x.Status == DataJudProcessoStatus.Encontrado ? 0 : 1)
            .ThenBy(x => x.Numero)
            .ToList();

        int r = 2;
        foreach (var row in rows)
        {
            ws.Cell(r, 1).Value = row.Numero;
            ws.Cell(r, 2).Value = RotuloStatus(row.Status);
            ws.Cell(r, 3).Value = row.Motivo;
            PintarStatus(ws.Cell(r, 2), row.Status);
            ws.Cell(r, 4).Value = row.Id;
            ws.Cell(r, 5).Value = row.Tribunal;
            ws.Cell(r, 6).Value = row.Grau;
            ws.Cell(r, 7).Value = row.Classe;
            ws.Cell(r, 8).Value = row.Sistema;
            ws.Cell(r, 9).Value = row.Formato;
            ws.Cell(r, 10).Value = row.Ajuiz;
            ws.Cell(r, 11).Value = row.Orgao;
            ws.Cell(r, 12).Value = row.Assuntos;
            ws.Cell(r, 13).Value = row.Total;
            r++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void WriteAnalise(IXLWorksheet ws, List<LinhaProcesso> ls)
    {
        var cab = new[] { "Movimento_Nome", "Tribunal", "Quantidade" };
        WriteHeader(ws, cab);

        var rows = ls.Where(l => l.FoiEncontrado)
            .GroupBy(l => new { l.Movimento_Nome, l.Tribunal })
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

    private static void WriteNaoEncontrados(IXLWorksheet ws, List<LinhaProcesso> linhas)
    {
        var cab = new[] { "Número CNJ", "Status", "Motivo" };
        WriteHeader(ws, cab);

        var pendencias = linhas
            .GroupBy(l => CnjFormat.ChaveCnj(l.NumeroProcesso))
            .Select(g => g.OrderBy(x => x.OrdemMovimentacao).First())
            .Where(p => !p.FoiEncontrado)
            .OrderBy(p => p.Status)
            .ThenBy(p => p.NumeroProcesso)
            .ToList();

        var porCnj = linhas
            .GroupBy(l => CnjFormat.ChaveCnj(l.NumeroProcesso))
            .Select(g => g.First())
            .ToList();

        ws.Cell(1, 5).Value = "Resumo da consulta";
        ws.Cell(1, 5).Style.Font.Bold = true;
        ws.Cell(2, 5).Value = "Enviados";
        ws.Cell(2, 6).Value = porCnj.Count;
        ws.Cell(3, 5).Value = "Encontrados";
        ws.Cell(3, 6).Value = porCnj.Count(p => p.FoiEncontrado);
        ws.Cell(4, 5).Value = "Não encontrados";
        ws.Cell(4, 6).Value = porCnj.Count(p => p.Status == DataJudProcessoStatus.NaoEncontrado);
        ws.Cell(5, 5).Value = "Erros / inválidos";
        ws.Cell(5, 6).Value = porCnj.Count(p => p.Status is DataJudProcessoStatus.Erro or DataJudProcessoStatus.Invalido);

        int r = 2;
        foreach (var p in pendencias)
        {
            ws.Cell(r, 1).Value = CnjFormat.FormatarComMascara(p.NumeroProcesso);
            ws.Cell(r, 2).Value = RotuloStatus(p.Status);
            ws.Cell(r, 3).Value = p.Motivo;
            PintarStatus(ws.Cell(r, 2), p.Status);
            r++;
        }

        if (pendencias.Count == 0)
        {
            ws.Cell(2, 1).Value = "(nenhuma pendência — todos os CNJs foram encontrados)";
        }

        ws.Columns().AdjustToContents();
    }

    private static string RotuloStatus(string status) => status switch
    {
        DataJudProcessoStatus.Encontrado or "" => "Encontrado",
        DataJudProcessoStatus.NaoEncontrado => "Não encontrado",
        DataJudProcessoStatus.Erro => "Erro",
        DataJudProcessoStatus.Invalido => "Inválido",
        _ => status
    };

    private static void PintarStatus(IXLCell cell, string status)
    {
        if (status is DataJudProcessoStatus.NaoEncontrado)
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("FFF8D7DA");
        else if (status is DataJudProcessoStatus.Erro or DataJudProcessoStatus.Invalido)
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("FFFFE5CC");
        else if (status is DataJudProcessoStatus.Encontrado or "")
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("FFD1E7DD");
    }

    private sealed class ProcessoAgrupado
    {
        public string NumeroProcesso { get; }
        public LinhaProcesso Base { get; }
        public IReadOnlyList<LinhaProcesso> Movimentacoes { get; }

        public ProcessoAgrupado(string numeroProcesso, List<LinhaProcesso> linhas)
        {
            NumeroProcesso = numeroProcesso;
            Base = linhas.OrderBy(l => l.OrdemMovimentacao).First();

            if (!Base.FoiEncontrado)
            {
                Movimentacoes = Array.Empty<LinhaProcesso>();
                return;
            }

            var comMovimento = linhas
                .Where(l => l.OrdemMovimentacao > 0)
                .OrderBy(l => l.OrdemMovimentacao)
                .ToList();

            Movimentacoes = comMovimento.Count > 0 ? comMovimento : linhas.Take(1).ToList();
        }

        public bool FoiEncontrado => Base.FoiEncontrado;
        public string Status => string.IsNullOrWhiteSpace(Base.Status)
            ? DataJudProcessoStatus.Encontrado
            : Base.Status;
        public string Motivo => Base.Motivo;
    }
}
