using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Kubectl {
    public sealed class ConfigHelpers {
        public static string LocateConfig() {
            string homeDirectoryVariableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "UserProfile" : "HOME";
            string homeDirectory = Environment.GetEnvironmentVariable(homeDirectoryVariableName);
            if (String.IsNullOrWhiteSpace(homeDirectory)) {
                throw new Exception($"Cannot determine home directory for the current user (environment variable '{homeDirectoryVariableName}' is empty or not defined).");
            }
            return Path.Combine(homeDirectory, ".kube", "config");
        }
    }
}
