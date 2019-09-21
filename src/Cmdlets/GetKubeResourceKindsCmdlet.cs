using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace Kubectl.Cmdlets {
    [Cmdlet(VerbsCommon.Get, "KubeResourceKinds")]
    [OutputType(new[] { typeof((string kind, string apiVersion)) })]
    public sealed class GetKubeResourceKindsCmdlet : KubeCmdlet {
        private class ResourceKind {
            public string Kind { get; set; }
            public string ApiVersion { get; set; }
        }

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);
            var kinds = ModelTypes.Keys.Select(pair => new ResourceKind {
                Kind = pair.kind,
                ApiVersion = pair.apiVersion
            });
            foreach (var kind in kinds) {
                WriteObject(kind);
            }
        }
    }
}
