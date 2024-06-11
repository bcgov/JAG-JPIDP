using System.Reflection;
using System.Text.Json.Serialization;
using DIAMConfiguration.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NodaTime;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var name = Assembly.GetExecutingAssembly().GetName();
var outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
var path = Environment.GetEnvironmentVariable("LogFilePath") ?? "logs";





// Add services to the container.

var dbConnection = builder.Configuration.GetValue<string>("ConfigDatabase");
builder.Services.AddSingleton<IClock>(SystemClock.Instance);


builder.Services.AddDbContext<DIAMConfigurationDataStoreDbContext>(options => options
    .UseNpgsql(dbConnection, npg =>
    {
        npg.MigrationsHistoryTable(HistoryRepository.DefaultTableName, builder.Configuration.GetValue<string>("ConfigSchema"));
        npg.UseNodaTime();
    })
    .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: false));

builder.Host.UseSerilog((hostContext, services, configuration) =>
{
    configuration.ReadFrom.Configuration(hostContext.Configuration);
});


builder.Services.AddHealthChecks()
.AddCheck("liveness", () => HealthCheckResult.Healthy())
.AddNpgSql(dbConnection, tags: new[] { "services" }).ForwardToPrometheus();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(opt =>
{
    opt.AddPolicy(name: "CorsPolicy", builder =>
    {
        builder.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseCors("CorsPolicy");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => $"DIAM Configuration {Assembly.GetExecutingAssembly().GetName().Version?.ToString()}");

app.MapHealthChecks("/health/liveness");
app.MapMetrics("/metrics");

app.UseSerilogRequestLogging(options => options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
{
});

app.UseHttpsRedirection();

app.MapControllers();


//app.UseAuthorization();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DIAMConfigurationDataStoreDbContext>();
    try
    {
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        Log.Error($"Database migration failure {string.Join(",", ex.Message)}");
        throw;
    }
}


Log.Logger.Information("### DIAM Configuration complete");

app.Run();


