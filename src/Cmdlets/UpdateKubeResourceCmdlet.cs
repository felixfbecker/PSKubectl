using System;
using System.IO;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using KubeClient;
using KubeClient.Models;
using Microsoft.Extensions.Logging;

namespace Kubectl.Cmdlets {
    [Cmdlet(VerbsData.Update, "KubeResource", SupportsShouldProcess = true)]
    [OutputType(new[] { typeof(KubeResourceV1) })]
    public sealed class UpdateKubeResourceCmdlet : KubeApiCmdlet {
        private static readonly string fieldManager = "kubectl";

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Path", ValueFromPipelineByPropertyName = true)]
        [Alias("FullName")]
        [ValidateNotNullOrEmpty()]
        [SupportsWildcards()]
        public string Path { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Object")]
        [ValidateNotNull()]
        public object Resource { get; set; }

        private KubeYamlDeserializer deserializer;
        private KubeYamlSerializer serializer;

        protected override async Task BeginProcessingAsync(CancellationToken cancellationToken) {
            await base.BeginProcessingAsync(cancellationToken);

            deserializer = new KubeYamlDeserializer(Logger, ModelTypes);
            serializer = new KubeYamlSerializer(Logger);
        }

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);

            if (Path == null) {
                // Object given
                var yaml = Resource is KubeResourceV1 ? null : serializer.Serialize(Resource);
                await updateResource(Resource, yaml, cancellationToken);
            } else {
                // File path given
                ProviderInfo provider;
                // Resolve wildcards
                var paths = GetResolvedProviderPathFromPSPath(Path, out provider);
                foreach (var path in paths) {
                    WriteVerbose($"Reading object from YAML file {path}");
                    string yaml;
                    using (var streamReader = File.OpenText(path)) {
                        yaml = await streamReader.ReadToEndAsync();
                    }
                    var resource = deserializer.Deserialize(yaml);
                    await updateResource(resource, yaml, cancellationToken);
                }
            }
        }

        private async Task updateResource(object resource, string yaml, CancellationToken cancellationToken) {
            if (resource == null) throw new ArgumentNullException(nameof(resource));

            string kind = (string)resource.GetDynamicPropertyValue("Kind");
            if (String.IsNullOrEmpty(kind)) {
                throw new Exception("Input object does not have Kind set");
            }
            string apiVersion = (string)resource.GetDynamicPropertyValue("ApiVersion");
            if (String.IsNullOrEmpty(apiVersion)) {
                throw new Exception("Input object does not have ApiVersion set");
            }
            Logger.LogTrace($"ApiVersion {apiVersion}");

            object metadata = resource.GetDynamicPropertyValue("Metadata");
            if (metadata == null) {
                throw new Exception("Input object does not have Metadata set");
            }
            string name = (string)metadata.GetDynamicPropertyValue("Name");
            if (String.IsNullOrEmpty(name)) {
                throw new Exception("Input object does not have Metadata.Name set");
            }
            string kubeNamespace = (string)metadata.GetDynamicPropertyValue("Namespace");

            // Send to server
            if (ShouldProcess($"Apply {kind} \"{name}\" in namespace \"{kubeNamespace}\"", $"Apply {kind} \"{name}\" in namespace \"{kubeNamespace}\"?", "Confirm")) {
                if (resource is KubeResourceV1 res) {
                    Logger.LogTrace("Sending KubeResourceV1 object");
                    WriteObject(await client.Dynamic().Apply(
                        resource: (KubeResourceV1)resource,
                        fieldManager: fieldManager,
                        cancellationToken: cancellationToken
                    ));
                } else {
                    Logger.LogTrace("Sending raw YAML");
                    WriteObject(await client.Dynamic().ApplyYaml(
                        name: name,
                        kind: kind,
                        apiVersion: apiVersion,
                        yaml: yaml,
                        fieldManager: fieldManager,
                        kubeNamespace: kubeNamespace,
                        cancellationToken: cancellationToken
                    ));
                }
            }
        }
    }
}
