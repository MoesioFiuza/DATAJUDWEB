using DataWeb.Endpoints;
using DataWeb.Infrastructure.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;
using System.Diagnostics;

namespace DataWeb.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseDataWebPipeline(this WebApplication app)
    {
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

        var pathBase = app.Configuration["PathBase"];
        if (!string.IsNullOrEmpty(pathBase))
            app.UsePathBase(pathBase);

        app.UseCors();
        app.UseStaticFiles();

        return app;
    }

    public static WebApplication MapDataWebEndpoints(this WebApplication app)
    {
        app.MapHealthEndpoints();
        app.MapProcessarEndpoints();
        app.MapProcessarJsonEndpoints();
        app.MapUploadEndpoints();
        app.MapRootEndpoints();
        return app;
    }

    public static WebApplication RegisterDevBrowserLaunch(this WebApplication app)
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
            catch
            {
                // ignorar se o SO não abrir o browser
            }
        });

        return app;
    }
}
