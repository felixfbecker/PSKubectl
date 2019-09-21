using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using KubeClient;
using YamlDotNet.Serialization;
using System.IO;

namespace Kubectl.Cmdlets {
    [Cmdlet(VerbsCommon.Set, "KubeConfig", SupportsShouldProcess = true)]
    [OutputType(new[] { typeof(K8sConfig) })]
    public sealed class SetKubeConfigCmdlet : AsyncCmdlet {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public K8sConfig Config;

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);
            var serializer = new SerializerBuilder().Build();
            string yaml = serializer.Serialize(Config);
            string configPath = K8sConfig.Locate();
            if (ShouldProcess(configPath, "update")) {
                using (var streamWriter = new StreamWriter(configPath)) {
                    await streamWriter.WriteAsync(yaml); // Do not pass cancellationToken to not corrupt config file
                }
            }
        }
    }
}
