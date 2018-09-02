using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Kubectl {
    public static class ModelHelpers {
        public static PropertyInfo FindJsonProperty(Type objectType, string jsonProperty) {
            var property = objectType.GetProperties().FirstOrDefault(prop => {
                var attr = (JsonPropertyAttribute)prop.GetCustomAttribute(typeof(JsonPropertyAttribute));
                return attr?.PropertyName == jsonProperty;
            });
            if (property == null) {
                throw new Exception($"Could not find property with JsonProperty \"{jsonProperty}\" on type \"{objectType.Name}\"");
            }
            return property;
        }
    }
}
