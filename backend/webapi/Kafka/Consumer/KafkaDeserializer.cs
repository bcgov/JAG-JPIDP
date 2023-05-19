namespace Pidp.Kafka.Consumer;

using System.Text;
using Confluent.Kafka;
using Newtonsoft.Json;
using NodaTime.Text;
using NodaTime;

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
            return InstantPattern.ExtendedIso.Parse(instantString).Value;
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
