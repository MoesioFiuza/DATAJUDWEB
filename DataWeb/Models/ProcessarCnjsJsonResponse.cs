using DataWeb.Domain;
using DataWeb.Domain.Entities;
using DataWeb.Utils;

namespace DataWeb.Models;

public record PendenciaCnj(
    string NumeroProcesso,
    string Status,
    string Motivo);

public record ProcessarCnjsJsonResponse(
    int TotalCnjsEnviados,
    int TotalLinhas,
    IReadOnlyList<LinhaProcesso> Processos,
    int TotalEncontrados = 0,
    int TotalNaoEncontrados = 0,
    int TotalErros = 0,
    IReadOnlyList<string>? CnjsNaoEncontrados = null,
    IReadOnlyList<PendenciaCnj>? Pendencias = null)
{
    public static ProcessarCnjsJsonResponse Criar(int totalCnjsEnviados, List<LinhaProcesso> linhas)
    {
        var porCnj = linhas
            .GroupBy(l => CnjFormat.ChaveCnj(l.NumeroProcesso))
            .Select(g => g.First())
            .ToList();

        var encontrados = porCnj.Count(p => p.FoiEncontrado);
        var naoEncontrados = porCnj
            .Where(p => p.Status == DataJudProcessoStatus.NaoEncontrado)
            .ToList();
        var erros = porCnj
            .Where(p => p.Status is DataJudProcessoStatus.Erro or DataJudProcessoStatus.Invalido)
            .ToList();

        var pendencias = porCnj
            .Where(p => !p.FoiEncontrado)
            .Select(p => new PendenciaCnj(
                CnjFormat.FormatarComMascara(p.NumeroProcesso),
                p.Status,
                p.Motivo))
            .ToList();

        return new ProcessarCnjsJsonResponse(
            TotalCnjsEnviados: totalCnjsEnviados,
            TotalLinhas: linhas.Count(l => l.FoiEncontrado),
            Processos: linhas,
            TotalEncontrados: encontrados,
            TotalNaoEncontrados: naoEncontrados.Count,
            TotalErros: erros.Count,
            CnjsNaoEncontrados: naoEncontrados
                .Select(p => CnjFormat.FormatarComMascara(p.NumeroProcesso))
                .ToList(),
            Pendencias: pendencias);
    }
}
