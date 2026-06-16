using DataWeb.Domain;
using DataWeb.Utils;

namespace DataWeb.Services;

public static class JobResultadoAgrupador
{
    public static IReadOnlyList<ProcessoAgrupado> Agrupar(IEnumerable<LinhaProcesso> linhas)
    {
        return linhas
            .GroupBy(l => CnjFormat.FormatarNumero(l.NumeroProcesso))
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .Select(g => new ProcessoAgrupado(g.Key, g.ToList()))
            .OrderBy(p => p.NumeroProcessoDigits)
            .ToList();
    }

    public static ProcessoAgrupado? BuscarPorCnj(IReadOnlyList<ProcessoAgrupado> processos, string cnj)
    {
        var digits = CnjFormat.FormatarNumero(cnj);
        if (string.IsNullOrEmpty(digits)) return null;
        return processos.FirstOrDefault(p => p.NumeroProcessoDigits == digits);
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

        var comMovimento = linhas
            .Where(l => l.OrdemMovimentacao > 0)
            .OrderBy(l => l.OrdemMovimentacao)
            .ToList();

        Movimentacoes = comMovimento.Count > 0 ? comMovimento : linhas.Take(1).ToList();
    }

    public string NumeroProcessoFormatado => CnjFormat.FormatarComMascara(NumeroProcessoDigits);

    public LinhaProcesso? MovimentacaoMaisRecente =>
        Movimentacoes.FirstOrDefault(m => m.EhMaisRecente == "Sim") ?? Movimentacoes.FirstOrDefault();
}
