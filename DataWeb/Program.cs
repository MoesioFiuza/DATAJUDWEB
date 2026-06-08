using System.Diagnostics;
using DataWeb.Models;
using DataWeb.Services;
using DataWeb.Exporters;
using DataWeb.Parsers;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var port = builder.Configuration["Kestrel:Endpoints:Http:Url"] 
    ?? builder.Configuration["ASPNETCORE_URLS"] 
    ?? "http://localhost:5267";
builder.WebHost.UseUrls(port);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<DataJudOptions>(builder.Configuration.GetSection("DataJud"));
builder.Services.Configure<ProcessamentoOptions>(builder.Configuration.GetSection("Processamento"));
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

app.MapPost("/api/v1/processar", async (
    ProcessarCnjsRequest request,
    IConsultaUseCase useCase,
    IDatajudParser parser,
    IExcelExporter exporter,
    IOptions<ProcessamentoOptions> options,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    var cnjs = request.Cnjs?
        .Where(c => !string.IsNullOrWhiteSpace(c))
        .Select(c => c.Trim())
        .ToList();

    if (cnjs is null || cnjs.Count == 0)
    {
        return Results.Problem(
            title: "Requisição inválida",
            detail: "Informe ao menos um CNJ no campo 'cnjs'.",
            statusCode: 400);
    }

    var maxCnjs = options.Value.MaxCnjsApi;
    if (cnjs.Count > maxCnjs)
    {
        return Results.Problem(
            title: "Limite excedido",
            detail: $"Máximo de {maxCnjs} CNJs por requisição. Enviados: {cnjs.Count}.",
            statusCode: 400);
    }

    var paralelismo = options.Value.Paralelismo;
    logger.LogInformation("API v1: processando {Count} CNJs", cnjs.Count);

    return await ProcessamentoExcel.ProcessarAsync(
        useCase,
        parser,
        exporter,
        logger,
        () => useCase.ConsultarPorCnjsAsync(cnjs, paralelismo, ct),
        ct);
})
.WithName("ProcessarCnjsApi")
.WithSummary("Processa lista de CNJs (JSON) e retorna planilha Excel")
.Produces(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status422UnprocessableEntity)
.ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapPost("/upload-xlsx-excel", async (
    IFormFile file, 
    IConsultaUseCase useCase, 
    IDatajudParser parser, 
    IExcelExporter exporter, 
    ILogger<Program> logger, 
    CancellationToken ct) =>
{
    if (file is null || file.Length == 0) return Results.BadRequest("Arquivo vazio.");

    using var stream = file.OpenReadStream();
    return await ProcessamentoExcel.ProcessarAsync(
        useCase,
        parser,
        exporter,
        logger,
        () => useCase.ConsultarJsonAsync(stream, paralelismo: 20, ct),
        ct);
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
