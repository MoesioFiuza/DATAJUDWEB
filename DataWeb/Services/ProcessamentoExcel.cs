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
        Func<Task<IReadOnlyList<string>>> obterRespostas,
        CancellationToken ct)
    {
        try
        {
            var respostasJson = await obterRespostas();

            logger.LogInformation("Received {Count} JSON responses", respostasJson.Count);
            foreach (var (index, json) in respostasJson.Select((json, i) => (i, json)).Take(3))
            {
                logger.LogInformation("JSON {Index}: {JsonLength} characters", index, json.Length);
                var preview = json.Length > 300 ? json.Substring(0, 300) + "..." : json;
                logger.LogInformation("JSON {Index} preview: {JsonPreview}", index, preview);
            }

            var linhas = parser.ExtrairLinhas(respostasJson);
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
