using System;
using System.Collections.Generic;
using System.Management.Automation;
using KubeClient;
using KubeClient.Models;
using KubeClient.ResourceClients;
using Microsoft.Extensions.Logging;

namespace Kubectl {
    [Cmdlet(VerbsCommon.Get, "KubePod")]
    [OutputType(new[] { typeof(PodV1) })]
    public sealed class GetKubePodCmdlet : KubeCmdlet {
        [Parameter()]
        public string Namespace { get; set; } = "default";

        [Parameter()]
        public string LabelSelector { get; set; }

        [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; }

        protected override void ProcessRecord() {
            base.ProcessRecord();
            if (String.IsNullOrEmpty(Name)) {
                PodListV1 podList = client.PodsV1().List(
                    kubeNamespace: Namespace,
                    labelSelector: LabelSelector,
                    cancellationToken: CancellationToken
                ).GetAwaiter().GetResult();
                WriteObject(podList);
            } else {
                PodV1 pod = client.PodsV1().Get(
                    name: Name,
                    kubeNamespace: Namespace,
                    cancellationToken: CancellationToken
                ).GetAwaiter().GetResult();
                WriteObject(pod);
            }
        }
    }
}
