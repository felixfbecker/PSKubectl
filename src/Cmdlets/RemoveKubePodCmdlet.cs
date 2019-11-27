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
    [Cmdlet(VerbsCommon.Remove, "KubePod", SupportsShouldProcess = true, DefaultParameterSetName = "Parameters")]
    [OutputType(new[] { typeof(PodV1) })]
    public sealed class RemoveKubePodCmdlet : KubeApiCmdlet {
        private const string DefaultParamSet = "DefaultParamSet";
        private const string NamespaceObjectSet = "NamespaceObjectSet";
        private const string PodObjectSet = "PodObjectSet";

        [Parameter(ParameterSetName = DefaultParamSet)]
        [Alias("Ns")]
        public string Namespace { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 0,
            ParameterSetName = NamespaceObjectSet,
            ValueFromPipeline = true)]
        [ValidateNotNull()]
        public NamespaceV1 NamespaceObject { get; set; }

        [Parameter(ParameterSetName = DefaultParamSet)]
        public string LabelSelector { get; set; }

        [Parameter(Position = 0, ParameterSetName = DefaultParamSet, ValueFromPipeline = true)]
        [Parameter(Position = 1, ParameterSetName = NamespaceObjectSet, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards()]
        public string Name { get; set; }

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ParameterSetName = PodObjectSet)]
        [Alias("Pod")]
        [ValidateNotNull()]
        public object PodObject { get; set; }

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);
            if (ParameterSetName == PodObjectSet) {
                await DeletePod(
                    name: (string) PodObject.GetDynamicPropertyValue("Metadata").GetDynamicPropertyValue("Name"),
                    kubeNamespace: (string) PodObject.GetDynamicPropertyValue("Metadata")
                        .GetDynamicPropertyValue("Namespace"),
                    cancellationToken: cancellationToken
                );
            } else {
                var _namespace = NamespaceObject?.Metadata.Name ?? Namespace;

                if (Name != null) {
                    Name = Regex.Replace(Name, "^pods?/", "", RegexOptions.IgnoreCase);
                }

                if (LabelSelector != null || WildcardPattern.ContainsWildcardCharacters(Name)) {
                    IEnumerable<PodV1> podList = await client.PodsV1().List(
                        kubeNamespace: _namespace,
                        labelSelector: LabelSelector,
                        cancellationToken: cancellationToken
                    );
                    if (WildcardPattern.ContainsWildcardCharacters(Name)) {
                        var pattern = new WildcardPattern(Name);
                        podList = podList.Where(pod => pattern.IsMatch(pod.Metadata.Name));
                    }

                    await Task.WhenAll(podList.Select(pod =>
                        DeletePod(pod.Metadata.Name, pod.Metadata.Namespace, cancellationToken)));
                } else {
                    await DeletePod(name: Name, kubeNamespace: _namespace, cancellationToken: cancellationToken);
                }
            }
        }

        private async Task DeletePod(string name, string kubeNamespace, CancellationToken cancellationToken) {
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
