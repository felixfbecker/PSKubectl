using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using KubeClient;
using KubeClient.Models;
using KubeClient.ResourceClients;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Kubectl {
    [Cmdlet(VerbsData.Compare, "KubeResource")]
    [OutputType(new[] { typeof(JsonPatchDocument) })]
    public sealed class CompareKubeResourceCmdlet : KubeCmdlet {
        [Parameter(Mandatory = true, Position = 0)]
        public object Original { get; set; }

        [Parameter(Mandatory = true, Position = 1)]

        public object Modified { get; set; }

        [Parameter(Mandatory = true, Position = 2, ParameterSetName = "ThreeWay")]
        public object Current { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ThreeWayFromLastApplied")]
        public SwitchParameter ThreeWayFromLastApplied { get; set; }

        [Parameter(ParameterSetName = "ThreeWayFromLastApplied")]
        public SwitchParameter Annotate { get; set; }

        [Parameter(ParameterSetName = "TwoWay")]
        public SwitchParameter IgnoreDeletions { get; set; }

        [Parameter(ParameterSetName = "TwoWay")]
        public SwitchParameter IgnoreAdditionsAndModifications { get; set; }

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);
            var patch = new JsonPatchDocument();
            var comparer = new KubeResourceComparer(LoggerFactory);
            string apiGroupVersion = (string)Original.GetPropertyValue("ApiVersion");
            string apiVersion = apiGroupVersion.Split('/').Last();
            string kind = (string)Original.GetPropertyValue("Kind");
            Type type = modelTypes.GetValueOrDefault((kind, apiVersion));
            if (type == null) {
                WriteError(new ErrorRecord(new Exception($"Unknown (kind: {kind}, apiVersion: {apiVersion}). {modelTypes.Count} Known:\n{String.Join("\n", modelTypes.Keys)}"), null, ErrorCategory.InvalidData, null));
                return;
            }
            if (ThreeWayFromLastApplied) {
                comparer.CreateThreeWayPatchFromLastApplied(
                    current: Original,
                    modified: Modified,
                    type: type,
                    patch: patch,
                    annotate: Annotate
                );
            } else if (Current == null) {
                comparer.CreateTwoWayPatch(
                    original: Original,
                    modified: Modified,
                    type: type,
                    patch: patch,
                    ignoreDeletions: IgnoreDeletions,
                    ignoreAdditionsAndModifications: IgnoreAdditionsAndModifications
                );
            } else {
                comparer.CreateThreeWayPatch(
                    original: Original,
                    modified: Modified,
                    current: Current,
                    type: type,
                    patch: patch
                );
            }
            WriteObject(patch);
        }
    }
}
