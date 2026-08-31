using DataWeb.Domain;
using DataWeb.Domain.Entities;
using DataWeb.Infrastructure.Repositories;
using DataWeb.Models;
using DataWeb.Utils;

namespace DataWeb.Services;

public interface IJobResultadoFrontService
{
    Task<(IResult? Error, JobResultadoResumoResponse? Resumo)> ObterResumoAsync(string jobId, CancellationToken ct = default);
    Task<(IResult? Error, PaginatedResponse<ProcessoListaItem>? Pagina)> ListarProcessosAsync(
        string jobId, int page, int pageSize, CancellationToken ct = default);
    Task<(IResult? Error, ProcessoDetalheResponse? Detalhe)> ObterProcessoAsync(
        string jobId, string cnj, CancellationToken ct = default);
    Task<(IResult? Error, PaginatedResponse<MovimentacaoItem>? Pagina)> ListarMovimentacoesAsync(
        string jobId, string cnj, int page, int pageSize, CancellationToken ct = default);
}

public class JobResultadoFrontService : IJobResultadoFrontService
{
    private const int MaxPageSize = 100;
    private readonly IJobRepository _repository;

    public JobResultadoFrontService(IJobRepository repository)
    {
        _repository = repository;
    }

    public async Task<(IResult? Error, JobResultadoResumoResponse? Resumo)> ObterResumoAsync(
        string jobId, CancellationToken ct = default)
    {
        var (error, agrupados, totalCnjs) = await CarregarAgrupadosAsync(jobId, ct);
        if (error is not null || agrupados is null) return (error, null);

        var encontrados = agrupados.Count(p => p.FoiEncontrado);
        var naoEncontrados = agrupados.Count(p => p.Status == DataJudProcessoStatus.NaoEncontrado);
        var erros = agrupados.Count - encontrados - naoEncontrados;
        var totalMov = agrupados.Where(p => p.FoiEncontrado).Sum(p => p.Movimentacoes.Count);
        var pendencias = agrupados
            .Where(p => !p.FoiEncontrado)
            .Select(p => new PendenciaCnj(p.NumeroProcessoFormatado, p.Status, p.Motivo ?? ""))
            .ToList();

        return (null, new JobResultadoResumoResponse(
            jobId,
            totalCnjs,
            encontrados,
            totalMov,
            encontrados,
            naoEncontrados,
            erros,
            pendencias,
            CriarLinks(jobId)));
    }

    public async Task<(IResult? Error, PaginatedResponse<ProcessoListaItem>? Pagina)> ListarProcessosAsync(
        string jobId, int page, int pageSize, CancellationToken ct = default)
    {
        var (error, agrupados, _) = await CarregarAgrupadosAsync(jobId, ct);
        if (error is not null || agrupados is null) return (error, null);

        var (pagina, tamanho) = NormalizarPaginacao(page, pageSize);
        var items = agrupados
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .Select(MapListaItem)
            .ToList();

        return (null, CriarPagina(items, pagina, tamanho, agrupados.Count));
    }

    public async Task<(IResult? Error, ProcessoDetalheResponse? Detalhe)> ObterProcessoAsync(
        string jobId, string cnj, CancellationToken ct = default)
    {
        cnj = Uri.UnescapeDataString(cnj);
        var (error, agrupados, _) = await CarregarAgrupadosAsync(jobId, ct);
        if (error is not null) return (error, null);

        var processo = JobResultadoAgrupador.BuscarPorCnj(agrupados!, cnj);
        if (processo is null)
            return (Results.NotFound(new { error = "CNJ não faz parte deste job" }), null);

        return (null, MapDetalhe(jobId, processo));
    }

    public async Task<(IResult? Error, PaginatedResponse<MovimentacaoItem>? Pagina)> ListarMovimentacoesAsync(
        string jobId, string cnj, int page, int pageSize, CancellationToken ct = default)
    {
        cnj = Uri.UnescapeDataString(cnj);
        var (error, agrupados, _) = await CarregarAgrupadosAsync(jobId, ct);
        if (error is not null) return (error, null);

        var processo = JobResultadoAgrupador.BuscarPorCnj(agrupados!, cnj);
        if (processo is null)
            return (Results.NotFound(new { error = "CNJ não faz parte deste job" }), null);

        var (pagina, tamanho) = NormalizarPaginacao(page, pageSize);
        var movs = processo.Movimentacoes;
        var items = movs
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .Select(MapMovimentacao)
            .ToList();

        return (null, CriarPagina(items, pagina, tamanho, movs.Count));
    }

    private async Task<(IResult? Error, IReadOnlyList<ProcessoAgrupado>? Agrupados, int TotalCnjs)> CarregarAgrupadosAsync(
        string jobId, CancellationToken ct)
    {
        var job = await _repository.GetByIdAsync(jobId);
        if (job is null || job.JobKind != DataJudJobKind.ApiJson)
            return (Results.NotFound(new { error = "Job não encontrado" }), null, 0);

        if (job.Status == DataJudJobStatus.Erro)
        {
            return (Results.Problem(
                title: "Job com erro",
                detail: job.ErrorMessage ?? "Erro desconhecido",
                statusCode: 422), null, 0);
        }

        if (job.Status != DataJudJobStatus.Concluido)
        {
            return (Results.Conflict(new
            {
                error = "Job ainda não concluído",
                status = job.Status,
                cnjsProcessados = job.ProcessosProcessados,
                totalCnjs = job.TotalProcessos
            }), null, 0);
        }

        var resultado = await ObterResultadoDoJobAsync(job);
        if (resultado is null)
            return (Results.NotFound(new { error = "Resultado não encontrado" }), null, 0);

        var agrupados = JobResultadoAgrupador.Agrupar(resultado.Processos);
        return (null, agrupados, resultado.TotalCnjsEnviados);
    }

    private static async Task<ProcessarCnjsJsonResponse?> ObterResultadoDoJobAsync(DataJudJob job)
    {
        if (string.IsNullOrWhiteSpace(job.ResultJson)) return null;
        return await Task.FromResult(
            System.Text.Json.JsonSerializer.Deserialize<ProcessarCnjsJsonResponse>(
                job.ResultJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }));
    }

    private static (int Page, int PageSize) NormalizarPaginacao(int page, int pageSize)
    {
        var pagina = page < 1 ? 1 : page;
        var tamanho = pageSize < 1 ? 25 : Math.Min(pageSize, MaxPageSize);
        return (pagina, tamanho);
    }

    private static PaginatedResponse<T> CriarPagina<T>(IReadOnlyList<T> items, int page, int pageSize, int total)
    {
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
        return new PaginatedResponse<T>(page, pageSize, total, totalPages, items);
    }

    private static JobResultadoLinks CriarLinks(string jobId) =>
        new(
            $"/api/v1/processar/json/{jobId}/resultado/processos",
            $"/api/v1/processar/json/{jobId}/excel");

    private static ProcessoListaItem MapListaItem(ProcessoAgrupado p)
    {
        var recente = p.MovimentacaoMaisRecente;
        var b = p.Base;
        return new ProcessoListaItem(
            p.NumeroProcessoFormatado,
            p.Status,
            p.Motivo,
            b.Tribunal,
            CnjFormat.ObterGrauTratado(b.Grau),
            CnjFormat.ObterRamoJustica(p.NumeroProcessoDigits),
            CnjFormat.ObterNivelSigiloTratado(b.NivelSigilo),
            b.CClasse_Nome,
            b.Orgao_Nome,
            b.DataAjuizamento,
            b.Assuntos,
            p.Movimentacoes.Count,
            recente?.Movimento_Nome,
            recente?.DataHoraMovimentacao);
    }

    private static ProcessoDetalheResponse MapDetalhe(string jobId, ProcessoAgrupado p)
    {
        var b = p.Base;
        var cnj = Uri.EscapeDataString(p.NumeroProcessoFormatado);
        return new ProcessoDetalheResponse(
            p.NumeroProcessoFormatado,
            p.Status,
            p.Motivo,
            b.ID_Datajud,
            b.Tribunal,
            CnjFormat.ObterGrauTratado(b.Grau),
            CnjFormat.ObterRamoJustica(p.NumeroProcessoDigits),
            CnjFormat.ObterNivelSigiloTratado(b.NivelSigilo),
            b.CClasse_Codigo,
            b.CClasse_Nome,
            b.SSistema_Nome,
            b.FFormato_Nome,
            b.Orgao_Codigo,
            b.Orgao_Nome,
            b.Orgao_Municipio_IBGE,
            b.DataAjuizamento,
            b.Assuntos,
            p.Movimentacoes.Count,
            $"/api/v1/processar/json/{jobId}/resultado/processos/{cnj}/movimentacoes");
    }

    private static MovimentacaoItem MapMovimentacao(LinhaProcesso m) =>
        new(
            m.OrdemMovimentacao,
            m.DataHoraMovimentacao,
            m.Movimento_Codigo,
            m.Movimento_Nome,
            m.Mov_Orgao_Codigo,
            m.Mov_Orgao_Nome,
            m.Complementos_Tabelados,
            m.EhMaisRecente == "Sim");
}
