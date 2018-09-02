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

namespace Kubectl {
    [Cmdlet(VerbsCommon.Get, "KubeResourceKinds")]
    [OutputType(new[] { typeof((string kind, string apiVersion)) })]
    public sealed class GetKubeResourceKindsCmdlet : KubeCmdlet {
        private class ResourceKind {
            public string Kind { get; set; }
            public string ApiVersion { get; set; }
        }

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);
            var kinds = ModelTypes.Keys.Select(pair => new ResourceKind
            {
                Kind = pair.kind,
                ApiVersion = pair.apiVersion
            });
            WriteObject(kinds);
        }
    }
}
