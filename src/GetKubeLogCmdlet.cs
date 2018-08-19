using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Management.Automation;
using KubeClient;
using KubeClient.Models;
using KubeClient.ResourceClients;
using Microsoft.Extensions.Logging;

namespace Kubectl {
    [Cmdlet(VerbsCommon.Get, "KubeLog")]
    [OutputType(new[] { typeof(string) })]
    public class GetKubePodLogCmdlet : KubeCmdlet {
        [Parameter()]
        public string Namespace { get; set; }

        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        [Parameter(Position = 1)]
        public string Container { get; set; }

        [Parameter()]
        public SwitchParameter Follow { get; set; }

        protected override void ProcessRecord() {
            base.BeginProcessing();
            if (Follow) {
                IObservable<string> logs = client.PodsV1().StreamLogs(
                    kubeNamespace: Namespace,
                    name: Name,
                    containerName: Container
                );
                // TODO this makes it impossible to Ctrl+C until another log comes in
                foreach (String chunk in logs.ToEnumerable()) {
                    WriteObject(chunk);
                    if (CancellationToken.IsCancellationRequested) {
                        return;
                    }
                }
            } else {
                string logs = client.PodsV1().Logs(
                    kubeNamespace: Namespace,
                    name: Name,
                    containerName: Container,
                    cancellationToken: CancellationToken
                ).GetAwaiter().GetResult();
                this.WriteObject(logs);
            }
        }
    }
}
