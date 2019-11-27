using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using KubeClient;
using KubeClient.Models;

namespace Kubectl.Cmdlets {
    [Cmdlet(VerbsCommon.Get, "KubeResource")]
    [OutputType(new[] { typeof(KubeResourceV1) })]
    public sealed class GetKubeResourceCmdlet : KubeApiCmdlet {
        private const string DefaultParamSet = "DefaultParamSet";
        private const string NamespaceObjectSet = "NamespaceObjectSet";

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("Ns")]
        public string Namespace { get; set; } = "default";

        [Parameter(
            Mandatory = true,
            Position = 0,
            ParameterSetName = NamespaceObjectSet,
            ValueFromPipeline = true)]
        [ValidateNotNull()]
        public NamespaceV1 NamespaceObject { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 0,
            ParameterSetName = DefaultParamSet,
            ValueFromPipelineByPropertyName = true)]
        [Parameter(
            Mandatory = true,
            Position = 1,
            ParameterSetName = NamespaceObjectSet,
            ValueFromPipelineByPropertyName = true)]
        public string Kind { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string ApiVersion { get; set; }

        [Parameter(
            Position = 1,
            ParameterSetName = DefaultParamSet,
            ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(
            Position = 2,
            ParameterSetName = NamespaceObjectSet,
            ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards()]
        public string Name { get; set; }

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);

            var _namespace = NamespaceObject?.Metadata.Name ?? Namespace;

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
                    kubeNamespace: _namespace,
                    cancellationToken: cancellationToken
                );
                resources = resourceList.EnumerateItems();
            } else {
                var resource = await client.Dynamic().Get(
                    kind: Kind,
                    apiVersion: ApiVersion,
                    name: Name,
                    kubeNamespace: _namespace,
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
