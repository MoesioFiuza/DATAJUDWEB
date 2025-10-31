using System.Diagnostics;
using DataWeb.Services;
using DataWeb.Exporters;
using DataWeb.Parsers;
using DataWeb.Domain;
using Microsoft.AspNetCore.Http;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5267");

builder.Services.AddOpenApi();
builder.Services.Configure<DataJudOptions>(builder.Configuration.GetSection("DataJud"));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IExcelReader, ExcelReader>();
builder.Services.AddSingleton<IDataJudClient, DataJudClient>();
builder.Services.AddSingleton<IDatajudParser, DatajudParser>();
builder.Services.AddSingleton<IExcelExporter, ExcelExporter>();
builder.Services.AddSingleton<IConsultaUseCase, ConsultaUseCase>();

var app = builder.Build();

app.UseStaticFiles();

app.MapOpenApi();
app.MapScalarApiReference(o => { o.Title = "DataWeb API"; });
app.MapGet("/", () => Results.Redirect("/ui/index.html"));
app.MapGet("/swagger", () => Results.Redirect("/scalar"));

app.MapPost("/upload-xlsx", async (IFormFile file, IConsultaUseCase useCase, IDatajudParser parser, CancellationToken ct) =>
{
	if (file is null || file.Length == 0) return Results.BadRequest("Arquivo vazio.");
	using var stream = file.OpenReadStream();

	var respostasJson = await useCase.ConsultarJsonAsync(stream, paralelismo: 20, ct);
	return Results.Ok(respostasJson);
})
.Accepts<IFormFile>("multipart/form-data")
.Produces<IEnumerable<string>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.WithName("UploadXlsxConsultarJson")
.WithSummary("Upload XLSX → Consulta DataJud por estado → Retorna JSON bruto")
.DisableAntiforgery();

app.MapPost("/upload-xlsx-excel", async (IFormFile file, IConsultaUseCase useCase, IDatajudParser parser, IExcelExporter exporter, CancellationToken ct) =>
{
	if (file is null || file.Length == 0) return Results.BadRequest("Arquivo vazio.");
	using var stream = file.OpenReadStream();
	var respostasJson = await useCase.ConsultarJsonAsync(stream, paralelismo: 20, ct);
	var linhas = parser.ExtrairLinhas(respostasJson);
	var excelBytes = exporter.GerarExcel(linhas);

	return Results.File(
		excelBytes,
		"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
		"resultado.xlsx");
})
.Accepts<IFormFile>("multipart/form-data")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.WithName("UploadXlsxGerarExcel")
.WithSummary("Upload XLSX → Consulta DataJud por estado → Retorna Excel consolidado")
.DisableAntiforgery();

app.MapPost("/upload-xlsx-data", async (IFormFile file, IConsultaUseCase useCase, IDatajudParser parser, CancellationToken ct) =>
{
	if (file is null || file.Length == 0) return Results.BadRequest("Arquivo vazio.");
	using var stream = file.OpenReadStream();

	var respostasJson = await useCase.ConsultarJsonAsync(stream, paralelismo: 20, ct);
	var linhas = parser.ExtrairLinhas(respostasJson);
	return Results.Ok(linhas);
})
.Accepts<IFormFile>("multipart/form-data")
.Produces<IEnumerable<LinhaProcesso>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.WithName("UploadXlsxData")
.WithSummary("Upload XLSX → Retorna linhas tratadas para grid")
.DisableAntiforgery();

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

app.Run();