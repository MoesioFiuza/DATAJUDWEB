using System.Diagnostics;
using DataWeb.Services;
using DataWeb.Exporters;
using DataWeb.Parsers;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

var port = builder.Configuration["Kestrel:Endpoints:Http:Url"] 
    ?? builder.Configuration["ASPNETCORE_URLS"] 
    ?? "http://localhost:5267";
builder.WebHost.UseUrls(port);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<DataJudOptions>(builder.Configuration.GetSection("DataJud"));
builder.Services.AddHttpClient();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52_428_800; // 50 MB
});

builder.Services.AddSingleton<IExcelReader, ExcelReader>();
builder.Services.AddSingleton<IDataJudClient, DataJudClient>();
builder.Services.AddSingleton<IDatajudParser, DatajudParser>();
builder.Services.AddSingleton<IExcelExporter, ExcelExporter>();
builder.Services.AddSingleton<IConsultaUseCase, ConsultaUseCase>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.MapScalarApiReference(
        endpointPrefix: "/scalar", 
        options =>
    {
       options.Title = "DataWeb API";
       options.OpenApiRoutePattern = "/swagger/{documentName}/swagger.json";
    });
}
else
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });
}

var pathBase = builder.Configuration["PathBase"];
if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase(pathBase);
}

app.UseCors();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow 
}))
.WithName("HealthCheck");

app.MapPost("/upload-xlsx-excel", async (
    IFormFile file, 
    IConsultaUseCase useCase, 
    IDatajudParser parser, 
    IExcelExporter exporter, 
    ILogger<Program> logger, 
    CancellationToken ct) =>
{
    try
    {
        if (file is null || file.Length == 0) return Results.BadRequest("Arquivo vazio.");
        using var stream = file.OpenReadStream();
        var respostasJson = await useCase.ConsultarJsonAsync(stream, paralelismo: 20, ct);
        
        logger.LogInformation("Received {Count} JSON responses", respostasJson.Count());
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
        logger.LogError(ex, "JSON structure error at line {Line}", ex.StackTrace?.Split('\n').FirstOrDefault(l => l.Contains(":line"))?.Split(":line").LastOrDefault()?.Trim());
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
})
.Accepts<IFormFile>("multipart/form-data")
.WithName("UploadXlsxGerarExcel")
.WithSummary("Upload de XLSX com CNJs e retorno da planilha consolidada")
.DisableAntiforgery();

app.MapGet("/", (HttpRequest request, IWebHostEnvironment env) =>
{
   if (env.IsDevelopment()) return Results.Redirect("/scalar");

   var prefix = request.PathBase.HasValue ? request.PathBase.Value : "";
   return Results.Redirect($"{prefix}/ui/index.html");
});

if (app.Environment.IsDevelopment())
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "http://localhost:5267/",
                UseShellExecute = true
            });
        }
        catch { }
    });
}

app.Run();
