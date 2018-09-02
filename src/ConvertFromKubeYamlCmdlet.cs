using System.Threading.Tasks;
using System.IO;
using System.Management.Automation;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System;
using Microsoft.Extensions.Logging;
using System.Threading;
using YamlDotNet.Core.Events;
using YamlDotNet.Core;
using KubeClient.Models;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Reflection;

namespace Kubectl {
    [Cmdlet(VerbsData.ConvertFrom, "KubeYaml")]
    [OutputType(new[] { typeof(KubeResourceV1) })]
    public class ConvertFromKubeYamlCmdlet : KubeCmdlet {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public string InputObject { get; set; }

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .IgnoreUnmatchedProperties()
                .Build();
            // Deserialize to Dictionary first to check the kind field to determine the type
            Dictionary<string, object> dict = deserializer.Deserialize<Dictionary<string, object>>(InputObject);
            string kind = (string)dict["kind"];
            string apiGroupVersion = (string)dict["apiVersion"];
            string apiVersion = apiGroupVersion.Split('/').Last();
            WriteVerbose($"apiVersion {apiVersion}");
            Type type = ModelTypes.GetValueOrDefault((kind, apiVersion));
            if (type == null) {
                WriteError(new ErrorRecord(new Exception($"Unknown (kind: {kind}, apiVersion: {apiVersion}). {ModelTypes.Count} Known:\n{String.Join("\n", ModelTypes.Keys)}"), null, ErrorCategory.InvalidData, InputObject));
                return;
            }
            var resource = toPSObject(dict, type);
            WriteObject(resource);
        }

        /// <summary>
        /// Converts a structure of Dictionaries and Lists recursively to PSObjects with type names,
        /// Dictionaries and Lists depending on the given type
        /// </summary>
        private object toPSObject(object value, Type type) {
            Logger.LogTrace($"Type {type.Name}");
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            if (value == null) {
                return value;
            }
            if (type == typeof(string) || type.IsValueType) {
                Logger.LogTrace("Is scalar");
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
            if (type.IsGenericType) {
                Logger.LogTrace("Is generic");
                if (type.GetGenericTypeDefinition() == typeof(List<>)) {
                    Logger.LogTrace("Is list");
                    var valueType = type.GetGenericArguments()[0];
                    return ((IList)value).Cast<object>().Select(element => toPSObject(element, valueType)).ToList();
                }
                if (type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
                    Logger.LogTrace("Is map");
                    var valueType = type.GetGenericArguments()[1];
                    // Shadowed type is map too
                    var dict = new Dictionary<string, object>();
                    foreach (DictionaryEntry entry in ((IDictionary)value)) {
                        dict.Add((string)entry.Key, toPSObject(entry.Value, valueType));
                    }
                    return dict;
                }
            }
            Logger.LogTrace("Is other object");
            // Shadowed type is a kube model object, only copy the properties set in the map for diffing purposes
            var psObject = new PSObject();
            foreach (DictionaryEntry entry in ((IDictionary)value)) {
                var key = (string)entry.Key;
                var prop = ModelHelpers.FindJsonProperty(type, key);
                if (prop == null) {
                    throw new Exception($"Unknown property {key} on type {type.Name}");
                }
                Logger.LogTrace($"Property {prop.Name}");
                psObject.Properties.Add(new PSNoteProperty(prop.Name, toPSObject(entry.Value, prop.PropertyType)));
            }
            // Add type name for output formatting
            psObject.TypeNames.Insert(0, type.FullName);
            return psObject;
        }
    }
}
