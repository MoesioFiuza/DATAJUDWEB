using DataWeb.Domain.Entities;
using DataWeb.Exporters;
using DataWeb.Models;
using DataWeb.Services;
using Microsoft.Extensions.Options;

namespace DataWeb.Endpoints;

public static class ProcessarJsonEndpoints
{
    public static void MapProcessarJsonEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/processar/json")
            .WithTags("Processar JSON assíncrono");

        group.MapPost("/", CriarJobJsonAsync)
            .WithName("CriarJobJsonApi")
            .WithSummary("Cria job assíncrono para processar CNJs e retorna jobId")
            .Produces<ApiJsonJobCreatedResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{jobId}/status", ConsultarStatusJobAsync)
            .WithName("ConsultarStatusJobJsonApi")
            .WithSummary("Consulta status do job JSON pelo jobId")
            .Produces<ApiJsonJobStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{jobId}/resultado", ObterResultadoJobAsync)
            .WithName("ObterResultadoJobJsonApi")
            .WithSummary("Obtém resultado JSON do job quando concluído")
            .Produces<ProcessarCnjsJsonResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/{jobId}/excel", ObterExcelJobAsync)
            .WithName("ObterExcelJobJsonApi")
            .WithSummary("Obtém planilha Excel do job quando concluído")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> CriarJobJsonAsync(
        ProcessarCnjsRequest request,
        IApiJsonJobService jobService,
        IOptions<ProcessamentoOptions> options,
        CancellationToken ct)
    {
        var (error, cnjs) = ProcessarCnjsRequestValidator.Validate(request, options.Value.MaxCnjsApi);
        if (error is not null) return error;

        var created = await jobService.CriarJobAsync(cnjs, ct);
        return Results.Accepted($"/api/v1/processar/json/{created.JobId}/status", created);
    }

    private static async Task<IResult> ConsultarStatusJobAsync(
        string jobId,
        IApiJsonJobService jobService,
        CancellationToken ct)
    {
        var status = await jobService.ObterStatusAsync(jobId, ct);
        if (status is null)
            return Results.NotFound(new { error = "Job não encontrado" });

        return Results.Ok(status);
    }

    private static async Task<IResult> ObterResultadoJobAsync(
        string jobId,
        IApiJsonJobService jobService,
        CancellationToken ct)
    {
        var validacao = await ValidarJobConcluidoAsync(jobId, jobService, ct);
        if (validacao.Error is not null) return validacao.Error;

        var resultado = await jobService.ObterResultadoAsync(jobId, ct);
        if (resultado is null)
            return Results.NotFound(new { error = "Resultado não encontrado" });

        return Results.Ok(resultado);
    }

    private static async Task<IResult> ObterExcelJobAsync(
        string jobId,
        IApiJsonJobService jobService,
        IExcelExporter exporter,
        CancellationToken ct)
    {
        var validacao = await ValidarJobConcluidoAsync(jobId, jobService, ct);
        if (validacao.Error is not null) return validacao.Error;

        var resultado = await jobService.ObterResultadoAsync(jobId, ct);
        if (resultado is null)
            return Results.NotFound(new { error = "Resultado não encontrado" });

        var excelBytes = exporter.GerarExcel(resultado.Processos.ToList());
        return Results.File(
            excelBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "resultado.xlsx");
    }

    private static async Task<(IResult? Error, ApiJsonJobStatusResponse? Status)> ValidarJobConcluidoAsync(
        string jobId,
        IApiJsonJobService jobService,
        CancellationToken ct)
    {
        var status = await jobService.ObterStatusAsync(jobId, ct);
        if (status is null)
            return (Results.NotFound(new { error = "Job não encontrado" }), null);

        if (status.Status == DataJudJobStatus.Erro)
        {
            return (Results.Problem(
                title: "Job com erro",
                detail: status.Erro ?? "Erro desconhecido",
                statusCode: 422), null);
        }

        if (status.Status != DataJudJobStatus.Concluido)
        {
            return (Results.Conflict(new
            {
                error = "Job ainda não concluído",
                status = status.Status,
                progresso = status.Progresso,
                cnjsProcessados = status.CnjsProcessados,
                totalCnjs = status.TotalCnjs
            }), null);
        }

        return (null, status);
    }
}
