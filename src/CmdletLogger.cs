using System;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Threading;
using KubeClient;
using KubeClient.Extensions.KubeConfig;
using Microsoft.Extensions.Logging;

namespace Kubectl {
    public class CmdletLogger : ILogger {
        private readonly Cmdlet cmdlet;
        private readonly ConcurrentQueue<Action> queue;

        public CmdletLogger(Cmdlet cmdlet, ConcurrentQueue<Action> queue) {
            this.cmdlet = cmdlet;
            this.queue = queue;
        }

        public IDisposable BeginScope<TState>(TState state) {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) {
            // PowerShell handles the loglevel handling
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
            this.queue.Enqueue(() => {
                switch (logLevel) {
                    case LogLevel.Debug:
                        this.cmdlet.WriteDebug(formatter(state, exception));
                        break;
                    case LogLevel.Trace:
                        this.cmdlet.WriteVerbose(formatter(state, exception));
                        break;
                    case LogLevel.Information:
                        this.cmdlet.WriteInformation(formatter(state, exception), new string[] { eventId.Name });
                        break;
                    case LogLevel.Warning:
                        this.cmdlet.WriteWarning(formatter(state, exception));
                        break;
                    case LogLevel.Error:
                        this.cmdlet.WriteError(new ErrorRecord(exception, exception.GetType().FullName, ErrorCategory.NotSpecified, null));
                        break;
                    case LogLevel.Critical:
                        this.cmdlet.ThrowTerminatingError(new ErrorRecord(exception, exception.GetType().FullName, ErrorCategory.NotSpecified, null));
                        break;
                }
            });
        }
    }
}
