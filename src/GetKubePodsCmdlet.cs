using System;
using System.Collections.Generic;
using System.Management.Automation;
using k8s;
using k8s.Models;

namespace Kubectl
{
    [Cmdlet(VerbsCommon.Get, "KubePods")]
    public class GetKubePodsCmdlet : KubeCmdlet
    {
        [Parameter()]
        public string Namespace { get; set; } = "default";

        [Parameter()]
        public SwitchParameter Export
        {
            get { return this.export; }
            set { this.export = value; }
        }
        private bool export = false;

        [Parameter()]
        public string Selector { get; set; }

        [Parameter()]
        public string FieldSelector { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            var podList = this.client.ListNamespacedPod(
                namespaceParameter: this.Namespace,
                labelSelector: this.Selector,
                fieldSelector: this.FieldSelector
            );
            // TODO make lists enumerable
            foreach (var pod in podList.Items)
            {
                this.WriteObject(pod);
            }
        }
    }
}
