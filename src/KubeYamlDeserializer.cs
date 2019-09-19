using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using KubeClient.Models;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Kubectl {
    public class KubeYamlDeserializer {
        private ILogger logger;
        private Dictionary<(string kind, string apiVersion), Type> modelTypes;
        private Deserializer deserializer;

        public KubeYamlDeserializer(ILogger logger, Dictionary<(string kind, string apiVersion), Type> modelTypes) {
            this.logger = logger;
            this.modelTypes = modelTypes;
            deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .IgnoreUnmatchedProperties()
                .Build();
        }

        public object Deserialize(string yaml) {
            if (yaml == null) throw new ArgumentNullException(nameof(yaml));

            // Deserialize to Dictionary first to check the kind field to determine the type
            Dictionary<string, object> dict = deserializer.Deserialize<Dictionary<string, object>>(yaml);
            string kind = (string)dict["kind"];
            string apiGroupVersion = (string)dict["apiVersion"];
            string apiVersion = apiGroupVersion.Split('/').Last();
            logger.LogDebug($"apiVersion {apiVersion}");
            if (!modelTypes.TryGetValue((kind, apiVersion), out Type type)) {
                throw new Exception($"Unknown (kind: {kind}, apiVersion: {apiVersion}). {modelTypes.Count} Known:\n{String.Join("\n", modelTypes.Keys)}");
            }
            return toPSObject(dict, type);
        }


        /// <summary>
        /// Converts a structure of Dictionaries and Lists recursively to PSObjects with type names,
        /// Dictionaries and Lists depending on the given type
        /// </summary>
        private object toPSObject(object value, Type type) {
            logger.LogTrace($"Type {type.Name}");
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            if (value == null) {
                return value;
            }
            if (type == typeof(string) || type.IsValueType) {
                logger.LogTrace("Is scalar");
                if (value.GetType() != typeof(string) && !value.GetType().IsValueType) {
                    throw new Exception($"Invalid type: Expected {type.Name}, got {value.GetType().Name}");
                }
                // Convert.ChangeType() can't cast to Nullable, need to unwrap
                if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>))) {
                    type = Nullable.GetUnderlyingType(type);
                }
                // YAML is often ambiguous with scalar types,
                // e.g. HTTPGetActionV1.Port is officially string but often specified as int
                // Be tolerant here by trying to cast
                return Convert.ChangeType(value, type);
            }
            if (type == typeof(Int32OrStringV1)) {
                if (value is int) {
                    return value;
                }
                if (value is string stringValue) {
                    if (Int32.TryParse(stringValue, out int intValue)) {
                        return intValue;
                    }
                    return stringValue;
                }
                throw new Exception($"Invalid type: Expected int or string, got {value.GetType().Name}");
            }
            if (type.IsGenericType) {
                logger.LogTrace("Is generic");
                if (type.GetGenericTypeDefinition() == typeof(List<>)) {
                    logger.LogTrace("Is list");
                    var valueType = type.GetGenericArguments()[0];
                    return ((IList)value).Cast<object>().Select(element => toPSObject(element, valueType)).ToList();
                }
                if (type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
                    logger.LogTrace("Is map");
                    var valueType = type.GetGenericArguments()[1];
                    // Shadowed type is map too
                    var dict = new Dictionary<string, object>();
                    foreach (DictionaryEntry entry in ((IDictionary)value)) {
                        dict.Add((string)entry.Key, toPSObject(entry.Value, valueType));
                    }
                    return dict;
                }
            }
            logger.LogTrace("Is other object");
            // Shadowed type is a kube model object, only copy the properties set in the map for diffing purposes
            var psObject = new PSObject();
            foreach (DictionaryEntry entry in ((IDictionary)value)) {
                var key = (string)entry.Key;
                var prop = ModelHelpers.FindJsonProperty(type, key);
                if (prop == null) {
                    throw new Exception($"Unknown property {key} on type {type.Name}");
                }
                logger.LogTrace($"Property {prop.Name}");
                psObject.Properties.Add(new PSNoteProperty(prop.Name, toPSObject(entry.Value, prop.PropertyType)));
            }
            // Add type name for output formatting
            for (var t = type; t != null; t = t.BaseType) {
                psObject.TypeNames.Insert(0, t.FullName);
            }
            return psObject;
        }
    }
}
