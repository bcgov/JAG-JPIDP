namespace Common.Kafka.Deserializer;
using System;
using System.Globalization;
using System.Text;
using Confluent.Kafka;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Extensions;

public sealed class DefaultKafkaDeserializer<T> : IDeserializer<T>
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


        if (typeof(T) == typeof(Guid))
        {
            if (!Guid.TryParse(dataJson, out var guid))
            {
                throw new ArgumentException("The data is not a valid Guid.");
            }
            return (T)(object)guid;
        }

        if (typeof(T) == typeof(Instant))
        {
            var parsed = DateTime.Parse(dataJson, null, DateTimeStyles.RoundtripKind);
            return (T)(object)parsed.ToInstant();
        }

        Serilog.Log.Information("Message {0}", dataJson);


        return JsonConvert.DeserializeObject<T>(dataJson);
    }
}
