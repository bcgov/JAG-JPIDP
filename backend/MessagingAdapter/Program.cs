
namespace MessagingAdapter;

using System.Text.Json.Serialization;
using MessagingAdapter.AWS.Producer;
using MessagingAdapter.AWS.Subscriber;
using MessagingAdapter.Configuration;
using MessagingAdapter.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

public class Program
{
    private const string DbConnection = "DataStoreConnection";

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);



        // Add services to the container.
        builder.Services.Configure<PublisherOptions>(
        builder.Configuration.GetSection(PublisherOptions.Publisher));
        builder.Services.AddControllers().AddJsonOptions(opts =>
        {
            var enumConverter = new JsonStringEnumConverter();
            opts.JsonSerializerOptions.Converters.Add(enumConverter);
        });
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();



        var dbConnection = builder.Configuration.GetValue<string>(DbConnection);


        builder.Services.AddDbContext<MessagingAdapterContext>(options => options
            .UseNpgsql(dbConnection, npg =>
            {
                npg.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "messaging-adapter");
                npg.UseNodaTime();
            }).EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: false));

        builder.Services.AddScoped<ISQSSubscriber, SQSSubscriber>();
        builder.Services.AddScoped<ISNSProducer, SNSProducer>();


        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
