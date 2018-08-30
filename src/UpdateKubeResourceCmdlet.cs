using System;
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
    [Cmdlet(VerbsData.Update, "KubeResource")]
    public sealed class UpdateKubeResourceCmdlet : KubeApiCmdlet {
        private const string lastAppliedConfigAnnotation = "kubectl.kubernetes.io/last-applied-configuration";

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public PSObject Resource;

        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken) {
            await base.ProcessRecordAsync(cancellationToken);
            var comparer = new KubeResourceComparer(LoggerFactory);
            KubeResourceV1 modified = (KubeResourceV1)Resource.BaseObject;
            KubeResourceV1 current = await client.Dynamic().Get(modified.Kind, modified.ApiVersion, modified.Metadata.Name, modified.Metadata.Namespace, cancellationToken);

            var patch = new JsonPatchDocument();
            comparer.CreateThreeWayPatchFromLastApplied(modified, current, patch, true);
            if (ShouldProcess(modified.Metadata.Name, "patch")) {
                await client.Dynamic().Patch(
                    kind: modified.Kind,
                    apiVersion: modified.ApiVersion,
                    name: modified.Metadata.Name,
                    kubeNamespace: modified.Metadata.Namespace,
                    patch: patch,
                    cancellationToken: cancellationToken
                );
            }

            // if (Resource is DeploymentV1Beta1) {
            //     DeploymentV1Beta1 modified = (DeploymentV1Beta1)Resource;
            //     DeploymentV1Beta1 current = await client.DeploymentsV1Beta1().Get(Resource.Metadata.Name, Resource.Metadata.Namespace, cancellationToken);

            //     Action<JsonPatchDocument<DeploymentV1Beta1>> patchAction = deploymentPatch => {
            //         var patch = new JsonPatchDocument();
            //         comparer.CreateThreeWayPatch(original, modified, current, patch);
            //         foreach (var operation in patch.Operations) {
            //             deploymentPatch.Operations.Add(new Operation<DeploymentV1Beta1>(operation.op, operation.path, operation.from, operation.value));
            //         }
            //     };

            //     if (ShouldProcess(Resource.Metadata.Name, "patch")) {
            //         await client.DeploymentsV1Beta1().Update(Resource.Metadata.Name, patchAction, Resource.Metadata.Namespace, cancellationToken);
            //     }
            // } else if (Resource is ServiceV1) {
            //     ServiceV1 modified = (ServiceV1)Resource;
            //     ServiceV1 current = await client.ServicesV1().Get(Resource.Metadata.Name, Resource.Metadata.Namespace, cancellationToken);
            //     ServiceV1 original;
            //     string originalJson = current.Metadata.Annotations[lastAppliedConfigAnnotation];
            //     if (!String.IsNullOrEmpty(originalJson)) {
            //         original = JsonConvert.DeserializeObject<ServiceV1>(originalJson);
            //     } else {
            //         original = modified;
            //     }

            //     Action<JsonPatchDocument<ServiceV1>> patchAction = servicePatch => {
            //         var patch = new JsonPatchDocument();
            //         comparer.CreateTwoWayPatch(current, modified, patch, ignoreDeletions: true);
            //         comparer.CreateTwoWayPatch(original, modified, patch, ignoreAdditionsAndModifications: true);
            //         foreach (var operation in patch.Operations) {
            //             servicePatch.Operations.Add(new Operation<ServiceV1>(operation.op, operation.path, operation.from, operation.value));
            //         }
            //     };

            //     if (ShouldProcess(Resource.Metadata.Name, "patch")) {
            //         await client.ServicesV1().Update(Resource.Metadata.Name, patchAction, Resource.Metadata.Namespace, cancellationToken);
            //     }
            //     await client.ServicesV1().Update(Resource.Metadata.Name, patchAction, Resource.Metadata.Namespace, cancellationToken);
            // } else {
            //     throw new Exception($"Unssuported resource kind {Resource.Kind}");
            // }
        }
    }
}
