
namespace edt.notifications;

using System.Text.Json.Serialization;
using edt.notifications.Configuration;
using edt.notifications.SNS.Producer;
using edt.notifications.SNS.Subscriber;

public class Program
{
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

        builder.Services.AddSingleton<ISQSSubscriber, SQSSubscriber>();
        builder.Services.AddSingleton<ISNSProducer, SNSProducer>();

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
