using DataWeb.Infrastructure.Extensions;
using DataWeb.Parsers;
using DataWeb.Exporters;
using DataWeb.Services;
using Microsoft.AspNetCore.Http.Features;

namespace DataWeb.Extensions;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddDataWebApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.Configure<DataJudOptions>(configuration.GetSection("DataJud"));
        services.Configure<ProcessamentoOptions>(configuration.GetSection("Processamento"));
        services.AddHttpClient();

        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 52_428_800; // 50 MB
        });

        services.AddSingleton<IExcelReader, ExcelReader>();
        services.AddSingleton<IDataJudClient, DataJudClient>();
        services.AddSingleton<IDatajudParser, DatajudParser>();
        services.AddSingleton<IExcelExporter, ExcelExporter>();
        services.AddSingleton<IConsultaUseCase, ConsultaUseCase>();

        services.AddDataWebInfrastructure(configuration);
        services.AddScoped<IApiJsonJobService, ApiJsonJobService>();
        services.AddScoped<IJobResultadoFrontService, JobResultadoFrontService>();
        services.AddHostedService<ApiJsonJobBackgroundService>();

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        return services;
    }

    public static WebApplicationBuilder ConfigureKestrelUrls(this WebApplicationBuilder builder)
    {
        var port = builder.Configuration["Kestrel:Endpoints:Http:Url"]
            ?? builder.Configuration["ASPNETCORE_URLS"]
            ?? "http://localhost:5267";

        builder.WebHost.UseUrls(port);
        return builder;
    }
}
