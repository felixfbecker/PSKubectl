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
        private const string DefaultParamSet = "DefaultParamSet";
        private const string NamespaceObjectSet = "NamespaceObjectSet";
        private const string ResourceObjectSet = "ResourceObjectSet";

        [Alias("Ns")]
        [Parameter(ParameterSetName = DefaultParamSet)]
        public string Namespace { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 0,
            ParameterSetName = NamespaceObjectSet,
            ValueFromPipeline = true)]
        [ValidateNotNull()]
        public NamespaceV1 NamespaceObject { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = DefaultParamSet)]
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = NamespaceObjectSet)]
        public string Kind { get; set; }

        [Parameter(Mandatory = true, Position = 1, ParameterSetName = DefaultParamSet)]
        [Parameter(Mandatory = true, Position = 2, ParameterSetName = NamespaceObjectSet)]
        [SupportsWildcards()]
        public string Name { get; set; }

        [Parameter()]
        public string ApiVersion { get; set; }

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ParameterSetName = ResourceObjectSet)]
        [Alias("Resource")]
        [ValidateNotNull()]
        public object ResourceObject { get; set; }

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);
            if (ParameterSetName == ResourceObjectSet) {
                await DeleteResource(
                    kind: (string) ResourceObject.GetDynamicPropertyValue("Kind"),
                    name: (string) ResourceObject.GetDynamicPropertyValue("Metadata").GetDynamicPropertyValue("Name"),
                    apiVersion: (string) ResourceObject.GetDynamicPropertyValue("ApiVersion"),
                    kubeNamespace: (string) ResourceObject.GetDynamicPropertyValue("Metadata")
                        .GetDynamicPropertyValue("Namespace"),
                    cancellationToken: cancellationToken
                );
            } else {
                var _namespace = NamespaceObject?.Metadata.Name ?? Namespace;

                try {
                    // Correct case
                    Kind = ModelTypes.First(entry => entry.Key.kind.Equals(Kind, StringComparison.OrdinalIgnoreCase))
                        .Key.kind;
                } catch (InvalidOperationException e) {
                    WriteError(new ErrorRecord(
                        new Exception($"Unknown resource kind \"{Kind}\". Known:\n{String.Join("\n", ModelTypes.Keys)}",
                            e), null, ErrorCategory.InvalidArgument, null));
                    return;
                }

                // Use first found ApiVersion if not given
                if (String.IsNullOrEmpty(ApiVersion)) {
                    ApiVersion = ModelTypes
                        .First(entry => entry.Key.kind.Equals(Kind, StringComparison.OrdinalIgnoreCase)).Key.apiVersion;
                    WriteVerbose($"ApiVersion not given, using {ApiVersion}");
                }

                if (WildcardPattern.ContainsWildcardCharacters(Name)) {
                    var resourceList = await client.Dynamic().List(
                        kind: Kind,
                        apiVersion: ApiVersion,
                        kubeNamespace: _namespace,
                        cancellationToken: cancellationToken
                    );
                    var items = resourceList.EnumerateItems();
                    if (WildcardPattern.ContainsWildcardCharacters(Name)) {
                        var pattern = new WildcardPattern(Name);
                        items = items.Where(pod => pattern.IsMatch(pod.Metadata.Name));
                    }

                    await Task.WhenAll(items.Select(resource => DeleteResource(
                        name: resource.Metadata.Name,
                        kind: resource.Kind,
                        apiVersion: resource.ApiVersion,
                        kubeNamespace: resource.Metadata.Namespace,
                        cancellationToken: cancellationToken)
                    ));
                } else {
                    await DeleteResource(
                        name: Name,
                        kind: Kind,
                        apiVersion: ApiVersion,
                        kubeNamespace: _namespace,
                        cancellationToken: cancellationToken
                    );
                }
            }
        }

        private async Task DeleteResource(string name, string kind, string apiVersion, string kubeNamespace = null, CancellationToken cancellationToken = default) {
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
