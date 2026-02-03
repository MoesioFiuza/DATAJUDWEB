using System.Diagnostics;
using DataWeb.Services;
using DataWeb.Exporters;
using DataWeb.Parsers;
using DataWeb.Domain;
using Microsoft.AspNetCore.Http;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var port = builder.Configuration["Kestrel:Endpoints:Http:Url"] 
    ?? builder.Configuration["ASPNETCORE_URLS"] 
    ?? "http://localhost:5267";
builder.WebHost.UseUrls(port);

builder.Services.AddOpenApi();
builder.Services.Configure<DataJudOptions>(builder.Configuration.GetSection("DataJud"));
builder.Services.AddHttpClient();

builder.Services.AddSingleton<IExcelReader, ExcelReader>();
builder.Services.AddSingleton<IDataJudClient, DataJudClient>();
builder.Services.AddSingleton<IDatajudParser, DatajudParser>();
builder.Services.AddSingleton<IExcelExporter, ExcelExporter>();
builder.Services.AddSingleton<IConsultaUseCase, ConsultaUseCase>();

builder.Services.AddSingleton<IJobService, JobService>();
builder.Services.AddHostedService<ProcessamentoBackgroundService>();

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

app.UseCors();
app.UseStaticFiles();

app.MapOpenApi();
app.MapScalarApiReference(o => { o.Title = "DataWeb API"; });

app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow 
}))
.WithName("HealthCheck");

app.MapPost("/api/processar", async (
    IFormFile file,
    HttpRequest request,
    IJobService jobService,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    var userId = request.Headers.TryGetValue("X-User-Id", out var userIdHeader) 
        ? userIdHeader.ToString() 
        : null;

    if (file == null || file.Length == 0)
        return Results.BadRequest(new { error = "Arquivo vazio" });

    if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
        !file.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "Apenas arquivos Excel são aceitos" });

    try
    {
        var jobId = await jobService.CriarJobAsync(
            userId, 
            file.FileName, 
            file.Length, 
            file.OpenReadStream());

        logger.LogInformation("Arquivo recebido: {FileName} ({Size} bytes) - Job: {JobId}", 
            file.FileName, file.Length, jobId);

        return Results.Ok(new
        {
            jobId = jobId,
            status = "pendente",
            message = "Arquivo recebido e aguardando processamento",
            createdAt = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao criar job");
        return Results.Problem(
            title: "Erro ao processar upload",
            detail: ex.Message,
            statusCode: 500);
    }
})
.Accepts<IFormFile>("multipart/form-data")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status500InternalServerError)
.WithName("ProcessarArquivo")
.WithSummary("Envia arquivo para processamento assíncrono")
.DisableAntiforgery();

// Consultar status do job
app.MapGet("/api/jobs/{jobId}", async (
    string jobId,
    IJobService jobService) =>
{
    var job = await jobService.ObterJobAsync(jobId);
    if (job == null)
        return Results.NotFound(new { error = "Job não encontrado" });

    return Results.Ok(new
    {
        jobId = job.Id,
        status = job.Status.ToString().ToLower(),
        fileName = job.NomeArquivo,
        fileSize = job.TamanhoArquivo,
        createdAt = job.CriadoEm,
        startedAt = job.IniciadoEm,
        completedAt = job.ConcluidoEm,
        totalProcessos = job.TotalProcessos,
        processosProcessados = job.ProcessosProcessados,
        progresso = job.TotalProcessos > 0 
            ? (double)job.ProcessosProcessados / job.TotalProcessos * 100 
            : 0,
        erro = job.Erro,
        downloadUrl = job.Status == JobStatus.Concluido 
            ? $"/api/jobs/{jobId}/download" 
            : null
    });
})
.WithName("ConsultarStatusJob")
.WithSummary("Consulta o status de um job de processamento");

app.MapGet("/api/jobs/{jobId}/download", async (
    string jobId,
    IJobService jobService,
    ILogger<Program> logger) =>
{
    var job = await jobService.ObterJobAsync(jobId);
    if (job == null)
        return Results.NotFound(new { error = "Job não encontrado" });

    if (job.Status != JobStatus.Concluido || string.IsNullOrEmpty(job.CaminhoResultado))
        return Results.BadRequest(new { error = "Job ainda não foi concluído" });

    if (!File.Exists(job.CaminhoResultado))
        return Results.NotFound(new { error = "Arquivo de resultado não encontrado" });

    try
    {
        var bytes = await File.ReadAllBytesAsync(job.CaminhoResultado);
        return Results.File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"resultado_{job.NomeArquivo}");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao fazer download do job {JobId}", jobId);
        return Results.Problem(
            title: "Erro ao fazer download",
            detail: ex.Message,
            statusCode: 500);
    }
})
.WithName("DownloadResultado")
.WithSummary("Faz download do resultado processado");

app.MapGet("/api/jobs", async (
    HttpRequest request,
    IJobService jobService) =>
{
    var userId = request.Headers.TryGetValue("X-User-Id", out var userIdHeader) 
        ? userIdHeader.ToString() 
        : null;
    
    var jobs = await jobService.ListarJobsPorUsuarioAsync(userId);
    return Results.Ok(jobs.Select(j => new
    {
        jobId = j.Id,
        status = j.Status.ToString().ToLower(),
        fileName = j.NomeArquivo,
        createdAt = j.CriadoEm,
        completedAt = j.ConcluidoEm,
        downloadUrl = j.Status == JobStatus.Concluido 
            ? $"/api/jobs/{j.Id}/download" 
            : null
    }));
})
.WithName("ListarJobs")
.WithSummary("Lista todos os jobs do usuário");


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
.WithSummary("Upload síncrono (legado)")
.DisableAntiforgery();

app.MapGet("/", () => Results.Redirect("/scalar"));

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