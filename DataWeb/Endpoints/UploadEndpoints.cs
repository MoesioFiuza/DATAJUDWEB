using DataWeb.Exporters;
using DataWeb.Parsers;
using DataWeb.Services;

namespace DataWeb.Endpoints;

public static class UploadEndpoints
{
    public static void MapUploadEndpoints(this WebApplication app)
    {
        app.MapPost("/upload-xlsx-excel", UploadXlsxGerarExcelAsync)
            .Accepts<IFormFile>("multipart/form-data")
            .WithName("UploadXlsxGerarExcel")
            .WithSummary("Upload de XLSX com CNJs e retorno da planilha consolidada")
            .DisableAntiforgery();
    }

    private static async Task<IResult> UploadXlsxGerarExcelAsync(
        IFormFile file,
        IConsultaUseCase useCase,
        IDatajudParser parser,
        IExcelExporter exporter,
        ILogger<Log> logger,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest("Arquivo vazio.");

        using var stream = file.OpenReadStream();
        return await ProcessamentoExcel.ProcessarAsync(
            useCase,
            parser,
            exporter,
            logger,
            () => useCase.ConsultarJsonAsync(stream, paralelismo: 20, ct),
            ct);
    }

    private sealed class Log;
}
