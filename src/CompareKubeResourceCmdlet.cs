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
        public KubeResourceV1 Original { get; set; }

        [Parameter(Mandatory = true, Position = 1)]

        public KubeResourceV1 Modified { get; set; }

        [Parameter(Mandatory = true, Position = 2, ParameterSetName = "ThreeWay")]
        public KubeResourceV1 Current { get; set; }

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
            if (ThreeWayFromLastApplied) {
                comparer.CreateThreeWayPatchFromLastApplied(
                    current: Original,
                    modified: Modified,
                    patch: patch,
                    annotate: Annotate
                );
            } else if (Current == null) {
                comparer.CreateTwoWayPatch(
                    original: Original,
                    modified: Modified,
                    patch: patch,
                    ignoreDeletions: IgnoreDeletions,
                    ignoreAdditionsAndModifications: IgnoreAdditionsAndModifications
                );
            } else {
                comparer.CreateThreeWayPatch(
                    original: Original,
                    modified: Modified,
                    current: Current,
                    patch: patch
                );
            }
            WriteObject(patch);
        }
    }
}
