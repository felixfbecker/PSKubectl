using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using KubeClient;
using KubeClient.Models;
using KubeClient.ResourceClients;
using Microsoft.Extensions.Logging;

namespace Kubectl {
    [Cmdlet(VerbsCommon.Get, "KubePod")]
    [OutputType(new[] { typeof(PodV1) })]
    public sealed class GetKubePodCmdlet : KubeCmdlet {
        [Parameter()]
        public string Namespace { get; set; }

        [Parameter()]
        public string LabelSelector { get; set; }

        [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; }

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);
            if (String.IsNullOrEmpty(Name)) {
                PodListV1 podList = await client.PodsV1().List(
                    kubeNamespace: Namespace,
                    labelSelector: LabelSelector,
                    cancellationToken: cancellationToken
                );
                WriteObject(podList);
            } else {
                PodV1 pod = await client.PodsV1().Get(
                    name: Name,
                    kubeNamespace: Namespace,
                    cancellationToken: cancellationToken
                );
                WriteObject(pod);
            }
        }
    }
}
