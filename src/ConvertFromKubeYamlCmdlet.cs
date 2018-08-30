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

namespace Kubectl {
    [Cmdlet(VerbsData.ConvertFrom, "KubeYaml")]
    [OutputType(new[] { typeof(KubeResourceV1) })]
    public class ConvertFromKubeYamlCmdlet : KubeApiCmdlet {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public string InputObject;

        private Dictionary<(string kind, string apiVersion), Type> modelTypes = ModelMetadata.KubeObject.BuildKindToTypeLookup(typeof(KubeObjectV1).Assembly);

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);
            var serializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .IgnoreUnmatchedProperties()
                .Build();
            // Deserialize to Dictionary first to check the kind field to determine the type
            Dictionary<string, object> obj = serializer.Deserialize<Dictionary<string, object>>(InputObject);
            string apiVersion = (string)obj["apiVersion"];
            string kind = (string)obj["kind"];
            Type type = modelTypes.GetValueOrDefault((kind, apiVersion));
            if (type == null) {
                WriteError(new ErrorRecord(new Exception($"Unknown apiVersion/kind {apiVersion}/{kind}. {modelTypes.Count} Known: {String.Join(", ", modelTypes.Keys)}"), null, ErrorCategory.InvalidData, InputObject));
                return;
            }
            KubeResourceV1 resource = (KubeResourceV1)serializer.Deserialize(InputObject, type);
            WriteObject(resource);
        }
    }
}
