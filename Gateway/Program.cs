using Gateway.Configurations;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
using OpenTelemetry;
using System.Diagnostics;  // For Activity and ActivitySource
using OpenTelemetry;       // For OpenTelemetry services
using OpenTelemetry.Trace; // For SpanProcessor and related tracing functionality


var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var configurationBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory());
configurationBuilder.AddJsonFile(
    builder.Environment.IsDevelopment()
        ? "appsettings.Development.json"
        : "appsettings.Production.json",
    optional: false, reloadOnChange: true);


const string serviceName = "gateway-api";
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.EnrichWithHttpResponse = (activity, response) =>
                {
                    var downstreamPath = activity.TagObjects.FirstOrDefault(x => x.Key == "url.path").Value?.ToString();
                    var upstreamRoute = activity.TagObjects.FirstOrDefault(x => x.Key == "http.route").Value?.ToString();
                    var method = activity.TagObjects.FirstOrDefault(x => x.Key == "http.request.method").Value?.ToString();

                    activity.DisplayName = $"{method} - {upstreamRoute} => {downstreamPath}";
                };
            })
            .AddHttpClientInstrumentation()
            .AddOtlpExporter()
            .AddJaegerExporter()
            .AddConsoleExporter();
    });

builder.Services.AddReverseProxy().LoadFromConfig(configurationBuilder.Build().GetSection("ReverseProxy"));


var app = builder.Build();

app.Use(async (context, next) =>
{
    var activity = System.Diagnostics.Activity.Current;
    if (activity != null)
    {
        activity.DisplayName = $"Gateway to http://localhost:5000{context.Request.Path}";
    }

    await next();
});


app.UseSwagger();
app.UseSwaggerUI(opt =>
{
    opt.SwaggerEndpoint("/swagger/projects/1.0/swagger.json", "Project service - 1.0");
    opt.SwaggerEndpoint("/swagger/identity/1.0/swagger.json", "Identity service - 1.0");
    // opt.SwaggerEndpoint("/swagger/customers/1.0/swagger.json", "Customer service - 1.0");
    // opt.SwaggerEndpoint("/swagger/orders/1.0/swagger.json", "Order service - 1.0");
    // opt.SwaggerEndpoint("/swagger/warehouses/1.0/swagger.json", "Warehouse service - 1.0");
    // opt.SwaggerEndpoint("/swagger/discounts/1.0/swagger.json", "Discount service - 1.0");
    // opt.SwaggerEndpoint("/swagger/employees/1.0/swagger.json", "Employee service - 1.0");
    // opt.SwaggerEndpoint("/swagger/auths/1.0/swagger.json", "Authentication service - 1.0");
});
app.MapGetSwaggerForYarp(builder.Configuration);
// app.UseSwagger();
// app.UseSwaggerUI();
app.UseCors(x => x
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapReverseProxy();

app.Run();
