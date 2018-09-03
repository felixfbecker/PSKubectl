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

namespace Kubectl.Cmdlets {
    [Cmdlet(VerbsData.Compare, "KubeResource")]
    [OutputType(new[] { typeof(Operation) })]
    public sealed class CompareKubeResourceCmdlet : KubeCmdlet {
        [Parameter(Mandatory = true, Position = 0)]
        public dynamic Original { get; set; }

        [Parameter(Mandatory = true, Position = 1)]

        public dynamic Modified { get; set; }

        [Parameter(Mandatory = true, Position = 2, ParameterSetName = "ThreeWay")]
        public dynamic Current { get; set; }

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
            // TODO do not pass the ContractResolver here once KubeClient allows customizing the serialisation
            var patch = new JsonPatchDocument(new List<Operation>(), new PSObjectAwareContractResolver());
            var comparer = new KubeResourceComparer(LoggerFactory);
            string apiGroupVersion = (string)Original.ApiVersion;
            string apiVersion = apiGroupVersion.Split('/').Last();
            string kind = (string)Original.Kind;
            Type type = ModelTypes.GetValueOrDefault((kind, apiVersion));
            if (type == null) {
                WriteError(new ErrorRecord(new Exception($"Unknown (kind: {kind}, apiVersion: {apiVersion}). {ModelTypes.Count} Known:\n{String.Join("\n", ModelTypes.Keys)}"), null, ErrorCategory.InvalidData, null));
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
            foreach (var operation in patch.Operations) {
                WriteObject(operation);
            }
        }
    }
}
