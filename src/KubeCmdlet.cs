using System;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using KubeClient;
using KubeClient.Extensions.KubeConfig;
using Microsoft.Extensions.Logging;

namespace Kubectl {
    public abstract class KubeCmdlet : AsyncCmdlet, IDisposable {
        /// <summary>The Kubernetes API endpoint to connect to</summary>
        [Parameter()]
        public Uri ApiEndPoint { get; set; }

        /// <summary>Skip verification of the server's SSL certificate?</summary>
        [Parameter()]
        public SwitchParameter AllowInsecure { get; set; }

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
            // clientOptions.LogHeaders = true;
            // clientOptions.LogPayloads = true;
            LoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new CmdletLoggerProvider(this));
            client = KubeApiClient.Create(clientOptions, loggerFactory);
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
