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

namespace Kubectl {
    [Cmdlet(VerbsData.Update, "KubeResource", SupportsShouldProcess = true)]
    [OutputType(new[] { typeof(KubeResourceV1) })]
    public sealed class UpdateKubeResourceCmdlet : KubeApiCmdlet {
        private const string lastAppliedConfigAnnotation = "kubectl.kubernetes.io/last-applied-configuration";

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public dynamic Resource;

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);

            dynamic modified = Resource;

            string kind = (string)modified.Kind;
            string apiGroupVersion = (string)modified.ApiVersion;
            string apiVersion = apiGroupVersion.Split('/').Last();

            // Figure out the model class - needed for diffing
            Type type = ModelTypes.GetValueOrDefault((kind, apiVersion));
            if (type == null) {
                WriteError(new ErrorRecord(new Exception($"Unknown (kind: {kind}, apiVersion: {apiVersion}). {ModelTypes.Count} Known:\n{String.Join("\n", ModelTypes.Keys)}"), null, ErrorCategory.InvalidData, Resource));
                return;
            }

            dynamic metadata = modified.Metadata;
            string name = (string)metadata.Name;
            string kubeNamespace = (string)metadata.Namespace;

            // Get current resource state from server
            WriteVerbose($"Getting kind: {kind}, apiVersion: {apiVersion}, name: {name}, namespace: {kubeNamespace}");
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

            WriteVerbose("Patch: " + JsonConvert.SerializeObject(patch, new JsonSerializerSettings
            {
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
