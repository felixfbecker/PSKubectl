using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using KubeClient;
using KubeClient.Models;
using KubeClient.ResourceClients;
using Microsoft.Extensions.Logging;

namespace Kubectl.Cmdlets {
    [Cmdlet(VerbsCommon.Get, "KubeResource")]
    [OutputType(new[] { typeof(KubeResourceV1) })]
    public sealed class GetKubeResourceCmdlet : KubeApiCmdlet {
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string Namespace { get; set; } = "default";

        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        public string Kind { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string ApiVersion { get; set; }

        [Parameter(Position = 1, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards()]
        public string Name { get; set; }

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);
            IEnumerable<KubeResourceV1> resources;
            var getList = String.IsNullOrEmpty(Name) || WildcardPattern.ContainsWildcardCharacters(Name);
            var typeLookup = getList ? ListModelTypes : ModelTypes;
            try {
                // Correct case
                Kind = typeLookup.First(entry => entry.Key.kind.Equals(Kind, StringComparison.OrdinalIgnoreCase)).Key.kind;
            } catch (InvalidOperationException e) {
                WriteError(new ErrorRecord(new Exception($"Unknown resource kind \"{Kind}\". Known:\n{String.Join("\n", typeLookup.Keys)}", e), null, ErrorCategory.InvalidArgument, null));
                return;
            }
            // Use first found ApiVersion if not given
            if (String.IsNullOrEmpty(ApiVersion)) {
                ApiVersion = typeLookup.First(entry => entry.Key.kind.Equals(Kind, StringComparison.OrdinalIgnoreCase)).Key.apiVersion;
                WriteVerbose($"ApiVersion not given, using {ApiVersion}");
            }
            if (getList) {
                var resourceList = await client.Dynamic().List(
                    kind: Kind,
                    apiVersion: ApiVersion,
                    kubeNamespace: Namespace,
                    cancellationToken: cancellationToken
                );
                resources = resourceList.EnumerateItems();
            } else {
                var resource = await client.Dynamic().Get(
                    kind: Kind,
                    apiVersion: ApiVersion,
                    name: Name,
                    kubeNamespace: Namespace,
                    cancellationToken: cancellationToken
                );
                resources = new[] { resource };
            }
            if (WildcardPattern.ContainsWildcardCharacters(Name)) {
                var pattern = new WildcardPattern(Name);
                resources = resources.Where(resource => pattern.IsMatch(resource.Metadata.Name));
            }
            WriteObject(resources, true);
        }
    }
}
