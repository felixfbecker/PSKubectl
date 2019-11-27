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
    [Cmdlet(VerbsCommon.Get, "KubeNamespace")]
    [OutputType(new[] { typeof(NamespaceV1) })]
    public sealed class GetKubeNamespaceCmdlet : KubeApiCmdlet {
        [Parameter()]
        public string LabelSelector { get; set; }

        [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards()]
        public string Name { get; set; }

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);
            IEnumerable<NamespaceV1> namespaces;
            if (String.IsNullOrEmpty(Name) || WildcardPattern.ContainsWildcardCharacters(Name)) {
                namespaces = await client.NamespacesV1().List(
                    labelSelector: LabelSelector,
                    cancellationToken: cancellationToken
                );
            } else {
                NamespaceV1 ns = await client.NamespacesV1().Get(
                    name: Name,
                    cancellationToken: cancellationToken
                );
                namespaces = new[] { ns };
            }
            if (WildcardPattern.ContainsWildcardCharacters(Name)) {
                var pattern = new WildcardPattern(Name);
                namespaces = namespaces.Where(pod => pattern.IsMatch(pod.Metadata.Name));
            }
            WriteObject(namespaces, true);
        }
    }
}
