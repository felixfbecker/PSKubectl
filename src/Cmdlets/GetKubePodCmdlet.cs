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
    [Cmdlet(VerbsCommon.Get, "KubePod")]
    [OutputType(new[] { typeof(PodV1) })]
    public sealed class GetKubePodCmdlet : KubeApiCmdlet {
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

        [Parameter()]
        public string LabelSelector { get; set; }

        [Parameter(
            Position = 0,
            ParameterSetName = DefaultParamSet,
            ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards()]
        public string Name { get; set; }

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);

            var _namespace = NamespaceObject?.Metadata.Name ?? Namespace;

            IEnumerable<PodV1> podList;
            if (String.IsNullOrEmpty(Name) || WildcardPattern.ContainsWildcardCharacters(Name)) {
                podList = await client.PodsV1().List(
                    kubeNamespace: _namespace,
                    labelSelector: LabelSelector,
                    cancellationToken: cancellationToken
                );
            } else {
                PodV1 pod = await client.PodsV1().Get(
                    name: Name,
                    kubeNamespace: _namespace,
                    cancellationToken: cancellationToken
                );
                podList = new[] { pod };
            }
            if (WildcardPattern.ContainsWildcardCharacters(Name)) {
                var pattern = new WildcardPattern(Name);
                podList = podList.Where(pod => pattern.IsMatch(pod.Metadata.Name));
            }

            WriteObject(podList, true);
        }
    }
}
