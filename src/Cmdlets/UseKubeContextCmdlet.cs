using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using KubeClient;
using YamlDotNet.Serialization;
using System.IO;
using System.Runtime.InteropServices;

namespace Kubectl.Cmdlets {
    [Cmdlet(VerbsOther.Use, "KubeContext")]
    [OutputType(new[] { typeof(K8sConfig) })]
    public sealed class UseKubeContextCmdlet : AsyncCmdlet {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Alias(new[] { "Name" })]
        [ValidateSet(typeof(ValidContextNamesGenerator))]
        public string Context;

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);
            string configPath = K8sConfig.Locate();
            K8sConfig config = K8sConfig.Load(configPath);
            config.CurrentContextName = Context;
            Serializer serializer = new SerializerBuilder().Build();
            string yaml = serializer.Serialize(config);
            if (ShouldProcess(configPath, "update")) {
                await File.WriteAllTextAsync(configPath, yaml); // Do not pass cancellationToken to not corrupt config file
            }
        }
    }
}
