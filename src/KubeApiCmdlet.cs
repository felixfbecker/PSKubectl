using System;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using KubeClient;
using KubeClient.Extensions.KubeConfig;
using KubeClient.Models;
using KubeClient.ResourceClients;
using Microsoft.Extensions.Logging;

namespace Kubectl {
    public abstract class KubeApiCmdlet : KubeCmdlet {
        /// <summary>The Kubernetes API endpoint to connect to</summary>
        [Parameter()]
        public Uri ApiEndPoint { get; set; }

        /// <summary>Skip verification of the server's SSL certificate?</summary>
        [Parameter()]
        public SwitchParameter AllowInsecure { get; set; }

        /// <summary>Log request payloads to the verbose output channel</summary>
        [Parameter()]
        public SwitchParameter LogPayloads { get; set; }

        protected K8sConfig config;

        /// <summary>The API client to be used by child cmdlets</summary>
        protected KubeApiClient client;

        protected override async Task BeginProcessingAsync(CancellationToken cancellationToken) {
            await base.BeginProcessingAsync(cancellationToken);
            config = K8sConfig.Load();
            KubeClientOptions clientOptions = config.ToKubeClientOptions(
                defaultKubeNamespace: "default"
            );
            clientOptions.AllowInsecure = AllowInsecure;
            if (ApiEndPoint != null) {
                clientOptions.ApiEndPoint = ApiEndPoint;
            }
            WriteVerbose($"Using endpoint {clientOptions.ApiEndPoint}");
            // clientOptions.LogHeaders = true;
            clientOptions.LogPayloads = LogPayloads;
            clientOptions.LoggerFactory = LoggerFactory;
            client = KubeApiClient.Create(clientOptions);
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if (this.client != null) {
                this.client.Dispose();
                this.client = null;
            }
        }
    }
}
