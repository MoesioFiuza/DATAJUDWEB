using DataWeb.Infrastructure.Data;
using DataWeb.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DataWeb.Infrastructure.Extensions;

// Extensões para IServiceCollection para facilitar a configuração dos serviços de infraestrutura.
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataWebInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Obter connection string do appsettings
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' não encontrada. " +
                "Configure em appsettings.json ou appsettings.Development.json");
        }

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));
        
        services.AddDbContext<DataWebDbContext>(options =>
        {
            options.UseMySql(
                connectionString,
                serverVersion,
                mySqlOptions =>
                {
                    // Configurações específicas do MySQL
                    mySqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);

                    // Timeout de comando
                    mySqlOptions.CommandTimeout(30);
                });

            // Em desenvolvimento, habilitar logs detalhados
            #if DEBUG
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
            #endif
        });

        // Registrar repositórios
        services.AddScoped<IJobRepository, JobRepository>();

        return services;
    }

    public static WebApplication ApplyDataWebMigrations(this WebApplication app, bool throwOnError = false)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataWebDbContext>>();

        try
        {
            var context = scope.ServiceProvider.GetRequiredService<DataWebDbContext>();
            
            logger.LogInformation("Verificando migrations pendentes...");

            // Verifica se existem migrations pendentes
            var pendingMigrations = context.Database.GetPendingMigrations().ToList();

            if (pendingMigrations.Any())
            {
                logger.LogInformation("Aplicando {Count} migration(s): {Migrations}",
                    pendingMigrations.Count,
                    string.Join(", ", pendingMigrations));

                context.Database.Migrate();

                logger.LogInformation("Migrations aplicadas com sucesso!");
            }
            else
            {
                logger.LogInformation("Nenhuma migration pendente.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao aplicar migrations. Verifique a connection string e se o banco está acessível.");
            
            if (throwOnError)
            {
                throw;
            }
            
            logger.LogWarning("A aplicação continuará sem aplicar migrations. Execute 'dotnet ef database update' manualmente.");
        }

        return app;
    }
}
