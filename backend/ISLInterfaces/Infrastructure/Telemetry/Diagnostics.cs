namespace ISLInterfaces.Infrastructure.Telemetry;

using System.Diagnostics;
using Confluent.Kafka;


internal static class Diagnostics
{
    private const string ActivitySourceName = "Confluent.Kafka";
    public static ActivitySource ActivitySource { get; } = new ActivitySource(ActivitySourceName);


    internal static class Producer
    {
        private const string ActivityName = ActivitySourceName + ".MessageProduced";

        internal static Activity Start<TKey, TValue>(string topic, Message<TKey, TValue> message)
        {
            var activity = ActivitySource.StartActivity(ActivityName);

            if (activity == null)
                return null;

            using (activity)
            {
                activity?.AddDefaultOpenTelemetryTags(topic, message);
            }

            return activity;
        }
    }

    private static Activity AddDefaultOpenTelemetryTags<TKey, TValue>(
        this Activity activity,
        string topic,
        Message<TKey, TValue> message)
    {

        return activity;
    }
}
