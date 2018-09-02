using System;
using System.Management.Automation;
using Newtonsoft.Json.Serialization;

namespace Kubectl {
    public class PSObjectAwareContractResolver : DefaultContractResolver {
        protected override JsonContract CreateContract(Type objectType) {
            JsonContract contract = base.CreateContract(objectType);
            if (objectType.IsSubclassOf(typeof(PSObject))) {
                contract.Converter = new PSObjectJsonConverter();
            }
            return contract;
        }
    }
}
