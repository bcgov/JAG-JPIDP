using JAMService;
using JAMService.Data;
using JAMService.Infrastructure;
using JAMService.Infrastructure.Clients.KeycloakAdminClient;
using JAMService.Infrastructure.Kafka;
using Microsoft.EntityFrameworkCore;
using Prometheus;

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

app.MapControllers();


app.Run();
