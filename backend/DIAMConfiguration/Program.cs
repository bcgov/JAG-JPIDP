using System.Reflection;
using System.Text.Json.Serialization;
using DIAMConfiguration.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NodaTime;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var dbConnection = builder.Configuration.GetValue<string>("ConfigDatabase");
builder.Services.AddSingleton<IClock>(SystemClock.Instance);


builder.Services.AddDbContext<DIAMConfigurationDataStoreDbContext>(options => options
    .UseNpgsql(dbConnection, npg => npg.UseNodaTime())
    .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: false));

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => $"DIAM Configuration {Assembly.GetExecutingAssembly().GetName().Version?.ToString()}");

app.MapHealthChecks("/health/liveness");
app.MapMetrics("/health/metrics");


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


Log.Logger.Information("### Approval Flow Configuration complete");

app.Run();
