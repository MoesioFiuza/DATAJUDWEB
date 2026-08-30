namespace DataWeb.Endpoints;

public static class RootEndpoints
{
    public static void MapRootEndpoints(this WebApplication app)
    {
        app.MapGet("/", (HttpRequest request, IWebHostEnvironment env) =>
        {
            if (env.IsDevelopment())
                return Results.Redirect("/scalar");

            var prefix = request.PathBase.HasValue ? request.PathBase.Value : "";
            return Results.Redirect($"{prefix}/ui/index.html");
        });
    }
}
