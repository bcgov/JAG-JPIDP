namespace Pidp.Kafka;

using System.Text;
using Confluent.Kafka;
using Newtonsoft.Json;
using Pidp.Helpers.Serializers;

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
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = ShouldSerializeContractResolver.Instance
        };

        var json = JsonConvert.SerializeObject(data, options);

        return Encoding.UTF8.GetBytes(json);
    }
}
