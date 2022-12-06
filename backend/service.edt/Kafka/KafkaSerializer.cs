namespace EdtService.Kafka;

using System.Text;
using Confluent.Kafka;
using Newtonsoft.Json;

internal sealed class KafkaSerializer<T> : ISerializer<T>
{
    public byte[] Serialize(T data, SerializationContext context)
    {
        if (typeof(T) == typeof(Null))
            return null;

        if (typeof(T) == typeof(Ignore))
            throw new NotSupportedException("Not Supported.");

        var json = JsonConvert.SerializeObject(data, Formatting.Indented,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore});

        return Encoding.UTF8.GetBytes(json);
    }
}

