using Gateway.Configurations;

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

builder.Services.AddReverseProxy().LoadFromConfig(configurationBuilder.Build().GetSection("ReverseProxy"));


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(opt =>
{
    opt.SwaggerEndpoint("/swagger/projects/1.0/swagger.json", "Catalog service - 1.0");
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
