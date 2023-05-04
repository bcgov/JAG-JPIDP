namespace edt.casemanagement.Kafka;

using System.Globalization;
using System.Text;
using Confluent.Kafka;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NodaTime.Serialization.JsonNet;

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

        Serilog.Log.Information("Case {0}", dataJson);

        var settings = new JsonSerializerSettings
        {
            Converters = { new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal, DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK" } }
        };


        return JsonConvert.DeserializeObject<T>(dataJson, settings);
    }
}
