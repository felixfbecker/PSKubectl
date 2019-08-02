using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Kubectl {
    public class KubeYamlSerializer {
        private ILogger logger;
        private Serializer serializer;

        public KubeYamlSerializer(ILogger logger) {
            this.logger = logger;
            serializer = new SerializerBuilder()
                .WithTypeInspector(inspector => new PSObjectTypeInspector(inspector))
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();
        }

        /// <summary>
        /// Serializes the given Kubernetes resource PSObject
        /// </summary>
        public string Serialize(object obj) {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return serializer.Serialize(obj);
        }

        private sealed class PSObjectPropertyDescriptor : IPropertyDescriptor {
            private PSPropertyInfo propertyInfo;

            public string Name {
                // TODO for some reason CamelCaseNamingConvention doesn't do anything
                get { return char.ToLower(this.propertyInfo.Name[0]) + this.propertyInfo.Name.Substring(1); }
            }

            public bool CanWrite {
                get { return this.propertyInfo.IsSettable; }
            }

            public Type Type {
                // PSObject properties don't have a static type
                // TODO should this look into BaseObject?
                get { return this.propertyInfo.Value.GetType(); }
            }

            public Type TypeOverride { get; set; }
            public int Order { get; set; }
            public ScalarStyle ScalarStyle { get; set; }

            public PSObjectPropertyDescriptor(PSPropertyInfo propInfo) {
                this.propertyInfo = propInfo;
                ScalarStyle = ScalarStyle.Any;
            }

            public T GetCustomAttribute<T>() where T : Attribute {
                // PSObject members don't have attributes
                // TODO should this look into BaseObject?
                return null;
            }

            public IObjectDescriptor Read(object target) {
                var propertyValue = ((PSObject)target).Properties[propertyInfo.Name].Value;
                // var actualType = TypeOverride ?? _typeResolver.Resolve(Type, propertyValue);
                return new ObjectDescriptor(propertyValue, TypeOverride ?? Type, Type, ScalarStyle);
            }

            public void Write(object target, object value) {
                ((PSObject)target).Properties[propertyInfo.Name].Value = value;
            }
        }

        private sealed class PSObjectTypeInspector : ITypeInspector {
            private ITypeInspector innerTypeDescriptor;

            public PSObjectTypeInspector(ITypeInspector innerTypeDescriptor) {
                this.innerTypeDescriptor = innerTypeDescriptor;
            }

            public IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container) {
                if (container is PSObject psObject) {
                    return psObject.Properties
                        .Where(prop => prop.MemberType == PSMemberTypes.NoteProperty || prop.MemberType == PSMemberTypes.Property)
                        .Cast<PSPropertyInfo>()
                        .Select((prop, index) => new PSObjectPropertyDescriptor(prop));
                }
                return innerTypeDescriptor.GetProperties(type, container);
            }

            public IPropertyDescriptor GetProperty(Type type, object container, string name, bool ignoreUnmatched) {
                if (container is PSObject psObject) {
                    return new PSObjectPropertyDescriptor(psObject.Properties[name]);
                }
                return innerTypeDescriptor.GetProperty(type, container, name, ignoreUnmatched);
            }
        }
    }
}
