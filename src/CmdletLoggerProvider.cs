using System;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Kubectl {
    public class CmdletLoggerProvider : ILoggerProvider {
        private readonly Cmdlet cmdlet;

        public CmdletLoggerProvider(Cmdlet cmdlet) {
            this.cmdlet = cmdlet;
        }

        public ILogger CreateLogger(string categoryName) {
            return new CmdletLogger(cmdlet);
        }

        public void Dispose() { }
    }
}

