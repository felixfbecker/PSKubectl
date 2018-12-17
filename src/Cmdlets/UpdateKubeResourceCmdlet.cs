using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using KubeClient;
using KubeClient.Models;
using KubeClient.ResourceClients;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Kubectl.Cmdlets {
    [Cmdlet(VerbsData.Update, "KubeResource", SupportsShouldProcess = true)]
    [OutputType(new[] { typeof(KubeResourceV1) })]
    public sealed class UpdateKubeResourceCmdlet : KubeApiCmdlet {
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Path", ValueFromPipelineByPropertyName = true)]
        [Alias("FullName")]
        [ValidateNotNullOrEmpty()]
        [SupportsWildcards()]
        public string Path { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Object")]
        [ValidateNotNull()]
        public object Resource { get; set; }

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);

            if (Path == null) {
                // Object given
                await updateResource(Resource, cancellationToken);
            } else {
                // File path given
                var deserializer = new KubeYamlDeserializer(Logger, ModelTypes);
                ProviderInfo provider;
                // Resolve wildcards
                var paths = GetResolvedProviderPathFromPSPath(Path, out provider);
                foreach (var path in paths) {
                    WriteVerbose($"Reading object from YAML file {path}");
                    var yaml = await System.IO.File.ReadAllTextAsync(path, cancellationToken);
                    var modified = deserializer.Deserialize(yaml);
                    await updateResource(modified, cancellationToken);
                }
            }
        }

        private async Task updateResource(dynamic modified, CancellationToken cancellationToken) {
            if (modified == null) throw new ArgumentNullException(nameof(modified));

            string kind = (string)modified.Kind;
            if (String.IsNullOrEmpty(kind)) {
                throw new Exception("Input object does not have Kind set");
            }
            string apiVersion = (string)modified.ApiVersion;
            if (String.IsNullOrEmpty(apiVersion)) {
                throw new Exception("Input object does not have ApiVersion set");
            }

            // Figure out the model class - needed for diffing
            Type type = ModelTypes.GetValueOrDefault((kind, apiVersion));
            if (type == null) {
                WriteError(new ErrorRecord(new Exception($"Unknown (kind: {kind}, apiVersion: {apiVersion}). {ModelTypes.Count} Known:\n{String.Join("\n", ModelTypes.Keys)}"), null, ErrorCategory.InvalidData, Resource));
                return;
            }

            dynamic metadata = modified.Metadata;
            string name = (string)metadata.Name;
            if (String.IsNullOrEmpty(name)) {
                throw new Exception("Input object does not have Metadata.Name set");
            }
            string kubeNamespace = (string)metadata.Namespace;

            // Get current resource state from server
            WriteVerbose($"Getting resource \"{name}\" of kind \"{kind}\" from namespace \"{kubeNamespace}\"");
            object current = await client.Dynamic().Get(name, kind, apiVersion, kubeNamespace, cancellationToken);
            if (current == null) {
                WriteError(new ErrorRecord(new Exception($"{kind} ({apiVersion}) \"{name}\" does not exist in namespace \"{kubeNamespace}\""), null, ErrorCategory.InvalidData, Resource));
                return;
            }

            // Generate three-way patch from current to modified
            // TODO do not pass the ContractResolver here once KubeClient allows customizing the serialisation
            var patch = new JsonPatchDocument(new List<Operation>(), new PSObjectAwareContractResolver());
            var comparer = new KubeResourceComparer(LoggerFactory);
            comparer.CreateThreeWayPatchFromLastApplied(current, modified, type, patch, true);

            WriteVerbose("Patch: " + JsonConvert.SerializeObject(patch, new JsonSerializerSettings {
                Formatting = Formatting.Indented,
                Converters = new[] { new PSObjectJsonConverter() }
            }));

            // Send patch to server
            if (ShouldProcess($"Sending patch for {kind} \"{name}\" in namespace \"{kubeNamespace}\"", $"Send patch for {kind} \"{name}\" in namespace \"{kubeNamespace}\"?", "Confirm")) {
                var result = await client.Dynamic().Patch(
                    name: name,
                    kind: kind,
                    apiVersion: apiVersion,
                    patch: patch,
                    kubeNamespace: kubeNamespace,
                    cancellationToken: cancellationToken
                );
                WriteObject(result);
            }
        }
    }
}
