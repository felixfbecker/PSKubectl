
using System.Management.Automation;
using KubeClient;

namespace Kubectl {

    public class ValidContextNamesGenerator : IValidateSetValuesGenerator {
        public string[] GetValidValues() {
            return K8sConfig.Load().Contexts.ConvertAll(context => context.Name).ToArray();
        }
    }
}
