using System;
using System.Collections.Concurrent;
using System.Management.Automation;
using Microsoft.Extensions.Logging;

namespace Kubectl {
    public class CmdletLoggerProvider : ILoggerProvider {
        private readonly Cmdlet cmdlet;
        private ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();

        public CmdletLoggerProvider(Cmdlet cmdlet) {
            this.cmdlet = cmdlet;
        }

        public ILogger CreateLogger(string categoryName) {
            var logger = new CmdletLogger(this.cmdlet, this.queue);
            return logger;
        }

        public void Flush() {
            foreach (var action in this.queue) {
                action();
            }
        }

        public void Dispose() {
            this.queue.Clear();
        }
    }
}

