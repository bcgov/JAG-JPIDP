namespace DIAMCornetService.Infrastructure;

using System.Text;
using Confluent.Kafka;
using Newtonsoft.Json;

internal sealed class KafkaSerializer<T> : ISerializer<T>
{
    public byte[]? Serialize(T data, SerializationContext context)
    {
        if (typeof(T) == typeof(Null))
        {
            return null;
        }

        if (typeof(T) == typeof(Ignore))
        {
            throw new NotSupportedException("Not Supported.");
        }

        var options = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        var json = JsonConvert.SerializeObject(data, options);

        return Encoding.UTF8.GetBytes(json);
    }
}
