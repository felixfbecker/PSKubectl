using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using KubeClient;
using KubeClient.Models;
using KubeClient.ResourceClients;
using Microsoft.Extensions.Logging;

namespace Kubectl.Cmdlets {
    [Cmdlet(VerbsCommon.Get, "KubeConfig")]
    [OutputType(new[] { typeof(K8sConfig) })]
    public sealed class GetKubeConfigCmdlet : AsyncCmdlet {
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);
            K8sConfig config = K8sConfig.Load();
            WriteObject(config);
        }
    }
}
