using System;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Threading;
using KubeClient;
using KubeClient.Extensions.KubeConfig;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;

namespace Kubectl {
    public abstract class KubeCmdlet : PSCmdlet, IDisposable {
        /// <summary>The Kubernetes API endpoint to connect to</summary>
        [Parameter()]
        public Uri ApiEndPoint { get; set; }

        /// <summary>Skip verification of the server's SSL certificate?</summary>
        [Parameter()]
        public SwitchParameter AllowInsecure { get; set; }

        /// <summary>The API client to be used by child cmdlets</summary>
        protected KubeApiClient client;

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        /// <summary>A CancellationToken that gets cancelled when `StopProcessing()` is called. Can be passed to API requests.</summary>
        protected CancellationToken CancellationToken { get { return cancellationTokenSource.Token; } }

        protected ILogger Logger;

        protected override void BeginProcessing() {
            base.BeginProcessing();
            K8sConfig config = K8sConfig.Load();
            KubeClientOptions clientOptions = config.ToKubeClientOptions(
                defaultKubeNamespace: "default"
            );
            clientOptions.AllowInsecure = AllowInsecure;
            if (ApiEndPoint != null) {
                clientOptions.ApiEndPoint = ApiEndPoint;
            }
            LoggerFactory loggerFactory = null;
#if DEBUG
            loggerFactory = new LoggerFactory();
            String logFile = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "logs", "PSKubectl-{Date}.log"));
            WriteVerbose($"Debug build, logging to {logFile}");
            loggerFactory.AddFile(logFile, LogLevel.Trace);
            // this.loggerProvider = new CmdletLoggerProvider(this);
            // loggerFactory.AddProvider(loggerProvider);
            clientOptions.LogHeaders = true;
            clientOptions.LogPayloads = true;
#endif
            client = KubeApiClient.Create(clientOptions, loggerFactory);
        }

        protected override void EndProcessing() {
            base.EndProcessing();
        }

        protected sealed override void StopProcessing() {
            this.cancellationTokenSource.Cancel();
        }

        public virtual void Dispose() {
            this.client.Dispose();
            this.cancellationTokenSource.Dispose();
        }
    }
}
