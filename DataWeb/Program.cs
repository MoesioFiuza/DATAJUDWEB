using System.Diagnostics;
using DataWeb.Models;
using DataWeb.Services;
using DataWeb.Exporters;
using DataWeb.Parsers;
using DataWeb.Domain.Entities;
using DataWeb.Infrastructure.Extensions;
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

builder.Services.AddDataWebInfrastructure(builder.Configuration);
builder.Services.AddScoped<IApiJsonJobService, ApiJsonJobService>();
builder.Services.AddHostedService<ApiJsonJobBackgroundService>();

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

app.ApplyDataWebMigrations(throwOnError: !app.Environment.IsDevelopment());

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
})
.WithName("ProcessarCnjsApi")
.WithSummary("Processa lista de CNJs (JSON) e retorna planilha Excel")
.Produces(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status422UnprocessableEntity)
.ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapPost("/api/v1/processar/json", async (
    ProcessarCnjsRequest request,
    IApiJsonJobService jobService,
    IOptions<ProcessamentoOptions> options,
    CancellationToken ct) =>
{
    var (error, cnjs) = ProcessarCnjsRequestValidator.Validate(request, options.Value.MaxCnjsApi);
    if (error is not null) return error;

    var created = await jobService.CriarJobAsync(cnjs, ct);
    return Results.Accepted($"/api/v1/processar/json/{created.JobId}/status", created);
})
.WithName("CriarJobJsonApi")
.WithSummary("Cria job assíncrono para processar CNJs e retorna jobId")
.Produces<ApiJsonJobCreatedResponse>(StatusCodes.Status202Accepted)
.ProducesProblem(StatusCodes.Status400BadRequest);

app.MapGet("/api/v1/processar/json/{jobId}/status", async (
    string jobId,
    IApiJsonJobService jobService,
    CancellationToken ct) =>
{
    var status = await jobService.ObterStatusAsync(jobId, ct);
    if (status is null)
        return Results.NotFound(new { error = "Job não encontrado" });

    return Results.Ok(status);
})
.WithName("ConsultarStatusJobJsonApi")
.WithSummary("Consulta status do job JSON pelo jobId")
.Produces<ApiJsonJobStatusResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapGet("/api/v1/processar/json/{jobId}/resultado", async (
    string jobId,
    IApiJsonJobService jobService,
    CancellationToken ct) =>
{
    var status = await jobService.ObterStatusAsync(jobId, ct);
    if (status is null)
        return Results.NotFound(new { error = "Job não encontrado" });

    if (status.Status == DataJudJobStatus.Erro)
    {
        return Results.Problem(
            title: "Job com erro",
            detail: status.Erro ?? "Erro desconhecido",
            statusCode: 422);
    }

    if (status.Status != DataJudJobStatus.Concluido)
    {
        return Results.Conflict(new
        {
            error = "Job ainda não concluído",
            status = status.Status,
            progresso = status.Progresso
        });
    }

    var resultado = await jobService.ObterResultadoAsync(jobId, ct);
    if (resultado is null)
        return Results.NotFound(new { error = "Resultado não encontrado" });

    return Results.Ok(resultado);
})
.WithName("ObterResultadoJobJsonApi")
.WithSummary("Obtém resultado JSON do job quando concluído")
.Produces<ProcessarCnjsJsonResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status409Conflict)
.ProducesProblem(StatusCodes.Status422UnprocessableEntity);

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
