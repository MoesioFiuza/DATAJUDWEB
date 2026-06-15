using DataWeb.Exporters;
using DataWeb.Models;
using DataWeb.Parsers;
using DataWeb.Services;
using Microsoft.Extensions.Options;

namespace DataWeb.Endpoints;

public static class ProcessarEndpoints
{
    public static void MapProcessarEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/processar", ProcessarCnjsExcelAsync)
            .WithName("ProcessarCnjsApi")
            .WithSummary("Processa lista de CNJs (JSON) e retorna planilha Excel")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status504GatewayTimeout);
    }

    private static async Task<IResult> ProcessarCnjsExcelAsync(
        ProcessarCnjsRequest request,
        IConsultaUseCase useCase,
        IDatajudParser parser,
        IExcelExporter exporter,
        IOptions<ProcessamentoOptions> options,
        ILogger<Log> logger,
        CancellationToken ct)
    {
        var (error, cnjs) = ProcessarCnjsRequestValidator.Validate(request, options.Value.MaxCnjsApi);
        if (error is not null) return error;

        var paralelismo = options.Value.Paralelismo;
        logger.LogInformation("API v1 Excel: processando {Count} CNJs", cnjs.Count);

        return await ProcessamentoExcel.ProcessarAsync(
            useCase,
            parser,
            exporter,
            logger,
            () => useCase.ConsultarPorCnjsAsync(cnjs, paralelismo, ct),
            ct);
    }

    private sealed class Log;
}
