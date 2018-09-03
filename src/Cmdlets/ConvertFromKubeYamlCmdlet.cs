using System.Threading.Tasks;
using System.IO;
using System.Management.Automation;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System;
using Microsoft.Extensions.Logging;
using System.Threading;
using YamlDotNet.Core.Events;
using YamlDotNet.Core;
using KubeClient.Models;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Reflection;

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
