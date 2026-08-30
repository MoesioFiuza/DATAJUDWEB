using DataWeb.Infrastructure.Data;
using DataWeb.Models;

namespace DataWeb.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        }))
        .WithName("HealthCheck")
        .WithSummary("Health check simples (load balancer)");

        app.MapGet("/status", ObterStatusServicosAsync)
            .WithName("ServiceStatus")
            .WithSummary("Status dos serviços (API, banco, DataJud)")
            .Produces<ServiceStatusResponse>(StatusCodes.Status200OK);

        app.MapGet("/api/v1/status", ObterStatusServicosAsync)
            .WithName("ServiceStatusApi")
            .WithSummary("Status dos serviços — alias para integrações")
            .Produces<ServiceStatusResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> ObterStatusServicosAsync(DataWebDbContext db, IConfiguration config)
    {
        var database = "offline";
        try
        {
            if (await db.Database.CanConnectAsync())
                database = "online";
        }
        catch
        {
            database = "offline";
        }

        var dataJudConfigured = !string.IsNullOrWhiteSpace(config["DataJud:ApiKey"]);
        var overall = database == "online" ? "healthy" : "degraded";

        var response = new ServiceStatusResponse(
            overall,
            new ServiceStatusDetails("online", database, dataJudConfigured),
            DateTime.UtcNow);

        return Results.Ok(response);
    }
}
