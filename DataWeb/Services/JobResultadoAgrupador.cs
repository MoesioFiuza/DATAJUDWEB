using DataWeb.Domain;
using DataWeb.Domain.Entities;
using DataWeb.Utils;

namespace DataWeb.Services;

public static class JobResultadoAgrupador
{
    public static IReadOnlyList<ProcessoAgrupado> Agrupar(IEnumerable<LinhaProcesso> linhas)
    {
        return linhas
            .GroupBy(l => CnjFormat.ChaveCnj(l.NumeroProcesso))
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .Select(g => new ProcessoAgrupado(g.Key, g.ToList()))
            .OrderBy(p => p.FoiEncontrado ? 0 : 1)
            .ThenBy(p => p.NumeroProcessoDigits)
            .ToList();
    }

    public static ProcessoAgrupado? BuscarPorCnj(IReadOnlyList<ProcessoAgrupado> processos, string cnj)
    {
        var chave = CnjFormat.ChaveCnj(cnj);
        if (string.IsNullOrEmpty(chave)) return null;
        return processos.FirstOrDefault(p => p.NumeroProcessoDigits == chave);
    }
}

public sealed class ProcessoAgrupado
{
    public string NumeroProcessoDigits { get; }
    public LinhaProcesso Base { get; }
    public IReadOnlyList<LinhaProcesso> Movimentacoes { get; }

    public ProcessoAgrupado(string numeroProcessoDigits, List<LinhaProcesso> linhas)
    {
        NumeroProcessoDigits = numeroProcessoDigits;
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
    public string? Motivo => string.IsNullOrWhiteSpace(Base.Motivo) ? null : Base.Motivo;

    public string NumeroProcessoFormatado => CnjFormat.FormatarComMascara(NumeroProcessoDigits);

    public LinhaProcesso? MovimentacaoMaisRecente =>
        Movimentacoes.FirstOrDefault(m => m.EhMaisRecente == "Sim") ?? Movimentacoes.FirstOrDefault();
}
