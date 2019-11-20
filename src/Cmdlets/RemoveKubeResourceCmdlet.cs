using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using KubeClient;
using KubeClient.Models;
using KubeClient.ResourceClients;
using Microsoft.Extensions.Logging;

namespace Kubectl.Cmdlets {
    [Cmdlet(VerbsCommon.Remove, "KubeResource", SupportsShouldProcess = true, DefaultParameterSetName = "Parameters")]
    [OutputType(new[] { typeof(PodV1) })]
    public sealed class RemoveKubeResourceCmdlet : KubeApiCmdlet {
        [Alias("Ns")]
        [Parameter(ParameterSetName = "Parameters")]
        public string Namespace { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Parameters")]
        public string Kind { get; set; }

        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "Parameters")]
        [SupportsWildcards()]
        public string Name { get; set; }

        [Parameter()]
        public string ApiVersion { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Object")]
        [ValidateNotNull()]
        public object Resource { get; set; }

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);
            if (ParameterSetName == "Parameters") {
                try {
                    // Correct case
                    Kind = ModelTypes.First(entry => entry.Key.kind.Equals(Kind, StringComparison.OrdinalIgnoreCase)).Key.kind;
                } catch (InvalidOperationException e) {
                    WriteError(new ErrorRecord(new Exception($"Unknown resource kind \"{Kind}\". Known:\n{String.Join("\n", ModelTypes.Keys)}", e), null, ErrorCategory.InvalidArgument, null));
                    return;
                }
                // Use first found ApiVersion if not given
                if (String.IsNullOrEmpty(ApiVersion)) {
                    ApiVersion = ModelTypes.First(entry => entry.Key.kind.Equals(Kind, StringComparison.OrdinalIgnoreCase)).Key.apiVersion;
                    WriteVerbose($"ApiVersion not given, using {ApiVersion}");
                }
                if (WildcardPattern.ContainsWildcardCharacters(Name)) {
                    var resourceList = await client.Dynamic().List(
                        kind: Kind,
                        apiVersion: ApiVersion,
                        kubeNamespace: Namespace,
                        cancellationToken: cancellationToken
                    );
                    var items = resourceList.EnumerateItems();
                    if (WildcardPattern.ContainsWildcardCharacters(Name)) {
                        var pattern = new WildcardPattern(Name);
                        items = items.Where(pod => pattern.IsMatch(pod.Metadata.Name));
                    }
                    await Task.WhenAll(items.Select(resource => deleteResource(
                        name: resource.Metadata.Name,
                        kind: resource.Kind,
                        apiVersion: resource.ApiVersion,
                        kubeNamespace: resource.Metadata.Namespace,
                        cancellationToken: cancellationToken)
                    ));
                } else {
                    await deleteResource(
                        name: Name,
                        kind: Kind,
                        apiVersion: ApiVersion,
                        kubeNamespace: Namespace,
                        cancellationToken: cancellationToken
                    );
                }
            } else {
                await deleteResource(
                    kind: (string)Resource.GetDynamicPropertyValue("Kind"),
                    name: (string)Resource.GetDynamicPropertyValue("Metadata").GetDynamicPropertyValue("Name"),
                    apiVersion: (string)Resource.GetDynamicPropertyValue("ApiVersion"),
                    kubeNamespace: (string)Resource.GetDynamicPropertyValue("Metadata").GetDynamicPropertyValue("Namespace"),
                    cancellationToken: cancellationToken
                );
            }
        }

        private async Task deleteResource(string name, string kind, string apiVersion, string kubeNamespace = null, CancellationToken cancellationToken = default) {
            if (!ShouldProcess($"Deleting {kind} \"{name}\" in namespace \"{kubeNamespace}\"", $"Delete {kind} \"{name}\" in namespace \"{kubeNamespace}\"?", "Confirm")) {
                return;
            }
            try {
                var resource = await client.Dynamic().Delete(
                    name: name,
                    kind: kind,
                    apiVersion: apiVersion,
                    kubeNamespace: kubeNamespace,
                    cancellationToken: cancellationToken
                );
                WriteObject(resource);
            } catch (Exception e) {
                WriteError(new ErrorRecord(e, null, ErrorCategory.NotSpecified, null));
            }
        }
    }
}
