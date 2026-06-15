using DataWeb.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureKestrelUrls();
builder.Services.AddDataWebApplication(builder.Configuration);

var app = builder.Build();

app.UseDataWebPipeline();
app.MapDataWebEndpoints();

if (app.Environment.IsDevelopment())
    app.RegisterDevBrowserLaunch();

app.Run();
