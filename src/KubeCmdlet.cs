using System;
using System.Management.Automation;
using k8s;

namespace Kubectl
{
    public abstract class KubeCmdlet : PSCmdlet
    {
        [Parameter(Mandatory = true), Alias(new[] { "Host" })]
        public string ServerHost { get; set; }

        protected Kubernetes client;

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            var config = new KubernetesClientConfiguration { Host = this.ServerHost };
            this.client = new Kubernetes(config);
        }
    }
}
