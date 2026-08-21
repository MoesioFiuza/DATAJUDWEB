using DataWeb.Domain;
using DataWeb.Exporters;
using DataWeb.Parsers;

namespace DataWeb.Services;

public static class ProcessamentoExcel
{
    public static async Task<IResult> ProcessarAsync(
        IConsultaUseCase useCase,
        IDatajudParser parser,
        IExcelExporter exporter,
        ILogger logger,
        Func<Task<IReadOnlyList<RespostaCnj>>> obterRespostas,
        CancellationToken ct)
    {
        try
        {
            var respostas = await obterRespostas();

            logger.LogInformation("Received {Count} CNJ responses", respostas.Count);
            foreach (var (index, resp) in respostas.Select((r, i) => (i, r)).Take(3))
            {
                var json = resp.Json ?? resp.Erro ?? "";
                logger.LogInformation("CNJ {Index} ({Cnj}): {JsonLength} characters, status {Status}",
                    index, resp.Cnj, json.Length, resp.StatusPrevio);
            }

            var linhas = parser.ExtrairLinhas(respostas);
            var excelBytes = exporter.GerarExcel(linhas);

            return Results.File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "resultado.xlsx");
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.LogError(ex, "JSON parsing error");
            return Results.Problem(
                title: "Erro ao processar JSON",
                detail: $"Formato JSON inválido: {ex.Message}",
                statusCode: 400);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("element of type"))
        {
            logger.LogError(ex, "JSON structure error");
            return Results.Problem(
                title: "Erro de estrutura JSON",
                detail: "A estrutura do JSON retornado pela API não está no formato esperado. Verifique se a API do DataJud mudou sua estrutura de resposta.",
                statusCode: 422);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Processamento cancelado (timeout ou cliente desconectou). Use POST /api/v1/processar/json para lotes grandes.");
            return Results.Problem(
                title: "Processamento interrompido",
                detail: "A requisição foi cancelada antes de concluir (timeout do proxy ou cliente). Para lotes grandes, use o fluxo assíncrono: POST /api/v1/processar/json.",
                statusCode: StatusCodes.Status504GatewayTimeout);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during processing");
            return Results.Problem(
                title: "Erro interno",
                detail: $"Erro inesperado: {ex.Message}",
                statusCode: 500);
        }
    }
}
