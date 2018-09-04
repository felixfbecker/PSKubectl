using System;
using System.Management.Automation;

namespace Kubectl {
    public static class ObjectExtensions {
        /// <summary>
        /// Retrieves a property value from a .NET object or a PSObject
        /// If the property does not exist on the .NET object, will throw an Exception.
        /// If the property does not exist on a PSObject, will return null.
        /// </summary>
        public static object GetDynamicPropertyValue(this object obj, string propertyName) {
            if (obj == null) {
                throw new ArgumentNullException(nameof(obj));
            }
            if (propertyName == null) {
                throw new ArgumentNullException(nameof(obj));
            }

            if (obj is PSObject psObject) {
                return psObject.Properties[propertyName]?.Value;
            }
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop == null) {
                throw new Exception($"Cannot get property value of unknown property {propertyName} on object of type {obj.GetType().Name}");
            }
            return prop.GetValue(obj);
        }
        /// <summary>
        /// Sets a property value on a .NET object or a PSObject
        /// If the property does not exist on the .NET object, will throw an Exception.
        /// If the property does not exist on a PSObject, will return null.
        /// </summary>
        public static void SetDynamicPropertyValue(this object obj, string propertyName, object value) {
            if (obj == null) {
                throw new ArgumentNullException(nameof(obj));
            }
            if (propertyName == null) {
                throw new ArgumentNullException(nameof(obj));
            }

            if (obj is PSObject psObject) {
                psObject.Properties.Add(new PSNoteProperty(propertyName, value));
                return;
            }
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop == null) {
                throw new Exception($"Cannot set property value of unknown property {propertyName} on object of type {obj.GetType().Name}");
            }
            prop.SetValue(obj, value);
        }
    }
}
