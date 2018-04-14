using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using k8s;
using k8s.Models;

namespace Kubectl
{
    [Cmdlet(VerbsCommon.Get, "KubePodLog")]
    public class GetKubePodLogCmdlet : KubeCmdlet
    {
        [Parameter()]
        public string Namespace { get; set; } = "default";

        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        [Parameter(Position = 1, Mandatory = true)]
        public string Container { get; set; }

        [Parameter()]
        public SwitchParameter Follow { get; set; }

        [Parameter()]
        public int? TailLines { get; set; }

        private Stream log;
        private StreamReader reader;

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            this.log = this.client.ReadNamespacedPodLog(
                namespaceParameter: this.Namespace,
                name: this.Name,
                container: this.Container,
                follow: this.Follow,
                tailLines: this.TailLines
            );
            this.reader = new StreamReader(this.log);
            while (!reader.EndOfStream)
            {
                this.WriteObject(reader.ReadLine());
            }
            this.WriteObject(this.log);
        }

        protected override void EndProcessing()
        {
            this.reader.Close();
            this.log.Close();
        }
    }
}
