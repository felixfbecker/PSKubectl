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
    [Cmdlet(VerbsCommon.Remove, "KubePod", SupportsShouldProcess = true, DefaultParameterSetName = "Parameters")]
    [OutputType(new[] { typeof(PodV1) })]
    public sealed class RemoveKubePodCmdlet : KubeApiCmdlet {
        [Alias("Ns")]
        [Parameter(ParameterSetName = "Parameters")]
        public string Namespace { get; set; }

        [Parameter(ParameterSetName = "Parameters")]
        public string LabelSelector { get; set; }

        [Parameter(Position = 0, ParameterSetName = "Parameters", ValueFromPipeline = true)]
        [SupportsWildcards()]
        public string Name { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Object")]
        [ValidateNotNull()]
        public object Pod { get; set; }

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);
            if (ParameterSetName == "Parameters") {
                if (LabelSelector != null || WildcardPattern.ContainsWildcardCharacters(Name)) {
                    IEnumerable<PodV1> podList = await client.PodsV1().List(
                        kubeNamespace: Namespace,
                        labelSelector: LabelSelector,
                        cancellationToken: cancellationToken
                    );
                    if (WildcardPattern.ContainsWildcardCharacters(Name)) {
                        var pattern = new WildcardPattern(Name);
                        podList = podList.Where(pod => pattern.IsMatch(pod.Metadata.Name));
                    }
                    await Task.WhenAll(podList.Select(pod => deletePod(pod.Metadata.Name, pod.Metadata.Namespace, cancellationToken)));
                } else {
                    await deletePod(name: Name, kubeNamespace: Namespace, cancellationToken: cancellationToken);
                }
            } else {
                await deletePod(
                    name: (string)Pod.GetDynamicPropertyValue("Metadata").GetDynamicPropertyValue("Name"),
                    kubeNamespace: (string)Pod.GetDynamicPropertyValue("Metadata").GetDynamicPropertyValue("Namespace"),
                    cancellationToken: cancellationToken
                );
            }
        }

        private async Task deletePod(string name, string kubeNamespace, CancellationToken cancellationToken) {
            if (!ShouldProcess($"Deleting pod \"{name}\" in namespace \"{kubeNamespace}\"", $"Delete pod \"{name}\" in namespace \"{kubeNamespace}\"?", "Confirm")) {
                return;
            }
            try {
                PodV1 pod = await client.PodsV1().Delete(name: name, kubeNamespace: kubeNamespace, cancellationToken: cancellationToken);
                WriteObject(pod);
            } catch (Exception e) {
                WriteError(new ErrorRecord(e, null, ErrorCategory.NotSpecified, null));
            }
        }
    }
}
