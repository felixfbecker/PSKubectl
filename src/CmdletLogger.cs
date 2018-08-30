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
        private string categoryName;

        public CmdletLogger(Cmdlet cmdlet, string categoryName) {
            this.cmdlet = cmdlet;
            this.categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) {
            // PowerShell handles the loglevel handling
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState logState, Exception exception, Func<TState, Exception, string> formatter) {
            if (!(SynchronizationContext.Current is ThreadAffinitiveSynchronizationContext)) {
                return;
            }
            SynchronizationContext.Current.Post(state => {
                switch (logLevel) {
                    case LogLevel.Trace:
                        this.cmdlet.WriteDebug(categoryName + ": " + formatter(logState, exception));
                        break;
                    case LogLevel.Debug:
                        this.cmdlet.WriteVerbose(categoryName + ": " + formatter(logState, exception));
                        break;
                    case LogLevel.Information:
                        this.cmdlet.WriteInformation(categoryName + ": " + formatter(logState, exception), new string[] { eventId.Name });
                        break;
                    case LogLevel.Warning:
                        this.cmdlet.WriteWarning(categoryName + ": " + formatter(logState, exception));
                        break;
                    case LogLevel.Error:
                        this.cmdlet.WriteError(new ErrorRecord(exception, exception.GetType().FullName, ErrorCategory.NotSpecified, null));
                        break;
                    case LogLevel.Critical:
                        this.cmdlet.ThrowTerminatingError(new ErrorRecord(exception, exception.GetType().FullName, ErrorCategory.NotSpecified, null));
                        break;
                }
            }, null);
        }
    }
}
