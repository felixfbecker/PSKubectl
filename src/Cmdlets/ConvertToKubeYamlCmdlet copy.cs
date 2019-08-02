using System.Threading.Tasks;
using System.Management.Automation;
using System.Threading;


namespace Kubectl.Cmdlets {
    [Cmdlet(VerbsData.ConvertTo, "KubeYaml")]
    [OutputType(new[] { typeof(string) })]
    public class ConvertToKubeYamlCmdlet : KubeCmdlet {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public object InputObject { get; set; }

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);
            var serializer = new KubeYamlSerializer(Logger);
            var yaml = serializer.Serialize(InputObject);
            WriteObject(yaml);
        }
    }
}
