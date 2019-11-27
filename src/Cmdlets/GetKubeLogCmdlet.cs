using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Management.Automation;
using KubeClient;
using KubeClient.Models;
using KubeClient.ResourceClients;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Kubectl.Cmdlets {
    [Cmdlet(VerbsCommon.Get, "KubeLog")]
    [OutputType(new[] { typeof(string) })]
    public class GetKubeLogCmdlet : KubeApiCmdlet {
        private const string DefaultParamSet = "DefaultParamSet";
        private const string NamespaceObjectSet = "NamespaceObjectSet";

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("Ns")]
        public string Namespace { get; set; }

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
            ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(
            Mandatory = true,
            Position = 1,
            ParameterSetName = NamespaceObjectSet,
            ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; }

        [Parameter(Position = 1)]
        public string Container { get; set; }

        [Parameter()]
        public SwitchParameter Follow { get; set; }

        [Parameter()]
        public int? LimitBytes { get; set; }

        [Parameter()]
        public int? Tail { get; set; }

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            base.BeginProcessing();

            var _namespace = NamespaceObject?.Metadata.Name ?? Namespace;

            if (Follow) {
                IObservable<string> logs = client.PodsV1().StreamLogs(
                    kubeNamespace: _namespace,
                    name: Name,
                    containerName: Container,
                    limitBytes: LimitBytes,
                    tailLines: Tail
                );
                await logs.ObserveOn(SynchronizationContext.Current).ForEachAsync(WriteObject, cancellationToken);
            } else {
                string logs = await client.PodsV1().Logs(
                    kubeNamespace: _namespace,
                    name: Name,
                    containerName: Container,
                    limitBytes: LimitBytes,
                    tailLines: Tail,
                    cancellationToken: cancellationToken
                );
                WriteObject(logs);
            }
        }
    }
}
