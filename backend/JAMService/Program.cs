
using JAMService;
using JAMService.Data;
using JAMService.Infrastructure.Kafka;
using Microsoft.EntityFrameworkCore;

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


builder.Services.AddKafkaClients(config);


// keycloak API client setup
// builder.Services.AddKeycloakClient(config);



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


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
