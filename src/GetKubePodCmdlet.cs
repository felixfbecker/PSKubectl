using System;
using System.Collections.Generic;
using System.Management.Automation;
using k8s;
using k8s.Models;

namespace Kubectl
{
    [Cmdlet(VerbsCommon.Get, "KubePod")]
    public class GetKubePodCmdlet : KubeCmdlet
    {
        [Parameter()]
        public string Namespace { get; set; } = "default";

        [Parameter(Position = 0)]
        public string Name { get; set; }

        [Parameter()]
        public SwitchParameter Export
        {
            get { return this.export; }
            set { this.export = value; }
        }
        private bool export = false;

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            var pod = this.client.ReadNamespacedPod(
                name: this.Name,
                namespaceParameter: this.Namespace,
                export: this.export
            );
            this.WriteObject(pod);
        }
    }
}
