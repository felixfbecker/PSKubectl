using System;
using System.Management.Automation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kubectl {
    /// <summary>JsonConverter that writes out PSObjects like normal objects</summary>
    public class PSObjectJsonConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return typeof(PSObject).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            var psObject = (PSObject)value;
            writer.WriteStartObject();
            foreach (var prop in psObject.Properties) {
                // Do not include ScriptProperties defined in Types.ps1xml files, only the members added by PSKubectl
                if (!prop.IsGettable || (prop.MemberType != PSMemberTypes.NoteProperty && prop.MemberType != PSMemberTypes.Property)) {
                    continue;
                }
                var propNameCamelCase = char.ToLower(prop.Name[0]) + prop.Name.Substring(1);
                writer.WritePropertyName(propNameCamelCase);
                serializer.Serialize(writer, prop.Value);
            }
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            throw new NotImplementedException();
        }

        public override bool CanRead { get { return false; } }
    }
}
