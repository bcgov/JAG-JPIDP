
namespace DIAMCornetService;

using Asp.Versioning;
using DIAMCornetService.Data;
using DIAMCornetService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;
using Serilog;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);


        // Load config
        var config = new DIAMCornetServiceConfiguration();
        builder.Configuration.Bind(config); // Bind configuration
        builder.Services.AddSingleton(config);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddKafkaClients(config);
        builder.Services.AddHttpClientServices(config);
        builder.Services.AddCornetServices(config);
        builder.Services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ApiVersionReader = new HeaderApiVersionReader("api-version");
        });


        builder.Services.AddDbContext<DIAMCornetDbContext>(options => options
            .UseNpgsql(config.ConnectionStrings.DIAMCornetDatabase, sql => sql.UseNodaTime())
            .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: false));

        // Validate EF migrations on startup
        using (var serviceScope = builder.Services.BuildServiceProvider().CreateScope())
        {
            var dbContext = serviceScope.ServiceProvider.GetRequiredService<DIAMCornetDbContext>();
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

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        app.UseHttpsRedirection();
        app.UseCors();
        app.UseAuthorization();
        app.MapMetrics();
        app.MapHealthChecks("/health/liveness").AllowAnonymous();


        app.MapControllers();

        app.Run();
    }

}
