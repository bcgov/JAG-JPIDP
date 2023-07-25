namespace Pidp.Helpers.Serializers;

using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Reflection;
using System.Collections;
using Pidp.Models;

public class ShouldSerializeContractResolver : CamelCasePropertyNamesContractResolver
{
    public static readonly ShouldSerializeContractResolver Instance = new();

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);

        if (property.PropertyType != typeof(string) &&
         typeof(IDictionary).IsAssignableFrom(property.PropertyType))
        {

            property.ShouldSerialize = instance =>
        {
            IDictionary enumerable = null;
            // this value could be in a public field or public property
            switch (member.MemberType)
            {
                case MemberTypes.Property:
                    enumerable = instance
                        .GetType()
                        .GetProperty(member.Name)
                        ?.GetValue(instance, null) as IDictionary;
                    break;
                case MemberTypes.Field:
                    enumerable = instance
                        .GetType()
                        .GetField(member.Name)
                        .GetValue(instance) as IDictionary;
                    break;
            }

            return enumerable.Keys.Count > 0;
        };
        }
        return property;
    }
}
