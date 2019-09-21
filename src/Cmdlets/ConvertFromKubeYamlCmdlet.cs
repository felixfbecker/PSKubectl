using System.Threading.Tasks;
using System.Management.Automation;
using System.Threading;
using KubeClient.Models;

namespace Kubectl.Cmdlets {
    [Cmdlet(VerbsData.ConvertFrom, "KubeYaml")]
    [OutputType(new[] { typeof(KubeResourceV1) })]
    public class ConvertFromKubeYamlCmdlet : KubeCmdlet {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public string InputObject { get; set; }

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);
            var deserializer = new KubeYamlDeserializer(Logger, ModelTypes);
            var resource = deserializer.Deserialize(InputObject);
            WriteObject(resource);
        }
    }
}
