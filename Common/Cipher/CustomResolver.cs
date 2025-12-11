using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GbService.Common.Cipher
{
    public class CustomResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty jsonProperty = base.CreateProperty(member, memberSerialization);
            PropertyInfo propertyInfo = member as PropertyInfo;
            jsonProperty.ShouldSerialize = ((object obj) => true);
            return jsonProperty;
        }
    }
}
