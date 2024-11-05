using System.Reflection;
using JAMService;
using JAMService.Data;
using JAMService.Infrastructure;
using JAMService.Infrastructure.Clients.KeycloakAdminClient;
using JAMService.Infrastructure.Kafka;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


var config = new JAMServiceConfiguration();
builder.Configuration.Bind(config); // Bind configuration
builder.Services.AddLogging(builder => builder.AddConsole());
builder.Services.AddSingleton(config);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<JAMServiceDbContext>(options => options
    .UseNpgsql(config.DatabaseConnectionInfo.JAMServiceConnection, sql => sql.UseNodaTime())
    .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: false));

// configure kafka consumers/producers
builder.Services.AddKafkaClients(config);

// configure JUSTIN ORDS client
builder.Services.ConfigureJUSTINHttpClient(config);


// keycloak API client setup
builder.Services.AddKeycloakClient(config);

// Add Prometheus metrics
builder.Services.AddMetrics();

builder.Services.AddHealthChecks();

var name = Assembly.GetExecutingAssembly().GetName();
var outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

var loggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Filter.ByExcluding("RequestPath like '/health%'")
    .Filter.ByExcluding("RequestPath like '/metrics%'")
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("Assembly", $"{name.Name}")
    .Enrich.WithProperty("Version", $"{name.Version}")
    .WriteTo.Console(
        outputTemplate: outputTemplate,
        theme: AnsiConsoleTheme.Code);


if (!string.IsNullOrEmpty(config.SplunkConfig.Host))
{
    loggerConfiguration.WriteTo.EventCollector(config.SplunkConfig.Host, config.SplunkConfig.CollectorToken);
}

Log.Logger = loggerConfiguration.CreateLogger();

if (string.IsNullOrEmpty(config.SplunkConfig.Host))
{
    Log.Warning("*** Splunk Host is not configured - check Splunk environment *** ");
}

else
{
    Log.Information($"*** Splunk logging to {config.SplunkConfig.Host} ***");
}



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Allow anonymous access to metrics endpoint
app.UseMetricServer();

// Migrate the database on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<JAMServiceDbContext>();
    dbContext.Database.Migrate();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.MapHealthChecks("/health/liveness").AllowAnonymous();
app.MapControllers();

app.Run();
