namespace JAMService.Infrastructure.Kafka;

using System.Globalization;
using System.Text;
using Confluent.Kafka;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Text;

internal sealed class KafkaDeserializer<T> : IDeserializer<T>
{
    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (typeof(T) == typeof(Null))
        {
            if (data.Length > 0)
                throw new ArgumentException("The data is null.");
            return default;
        }

        if (typeof(T) == typeof(Ignore))
            return default;

        var dataJson = Encoding.UTF8.GetString(data);

        var settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new InstantConverter() }
        };

        return JsonConvert.DeserializeObject<T>(dataJson, settings);
    }
}

public class InstantConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Instant);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
            var instantString = (string)reader.Value;

            try
            {
                return InstantPattern.ExtendedIso.Parse(instantString).Value;
            }
            catch (UnparsableValueException ex)
            {

                Serilog.Log.Debug($"Failed to parse instant value {instantString} {ex.Message} - will try alternate method");
                // try to parse as regular date

                var dateTime = DateTime.ParseExact(instantString, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal);
                var localTime = TimeZoneInfo.ConvertTimeToUtc(dateTime, TimeZoneInfo.Local);
                var instant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(localTime, DateTimeKind.Utc));
                return instant;


            }
        }
        else if (reader.TokenType == JsonToken.Date)
        {
            var dateTime = (DateTime)reader.Value;
            var instant = Instant.FromDateTimeUtc(dateTime.ToUniversalTime());
            return instant;
        }

        throw new JsonSerializationException($"Unexpected token type '{reader.TokenType}' when parsing Instant.");
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var instant = (Instant)value;
        var instantString = InstantPattern.ExtendedIso.Format(instant);
        writer.WriteValue(instantString);
    }
}
