using System;
using System.ComponentModel;
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
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Kubectl {
    public sealed class KubeResourceComparer {
        private Dictionary<Type, HashSet<string>> nonUpdateableTypes = new Dictionary<Type, HashSet<string>>
        {
            [typeof(KubeObjectV1)] = new HashSet<string> {
                "ApiVersion",
                "Kind",
            },
            [typeof(KubeResourceV1)] = new HashSet<string> {
                // Status cannot be updated directly, only spec
                // Not that Status is not strictly a property of KubeResourceV1 but effectively all subclasses have it
                "Status",
            },
            [typeof(ObjectMetaV1)] = new HashSet<string> {
                "ResourceVersion",
                "DeletionTimestamp",
                "Generation",
                "Uid",
                "SelfLink",
                "CreationTimestamp",
            },
            // [typeof(ContainerV1)] = new HashSet<string> {
            //     "TerminationMessagePolicy",
            //     "TerminationMessagePath",
            // }
        };
        private ILogger logger;
        public KubeResourceComparer(ILoggerFactory loggerFactory) {
            this.logger = loggerFactory.CreateLogger(nameof(KubeResourceComparer));
        }

        /// <summary>
        /// CreateThreeWayMergePatch reconciles a modified configuration with an original configuration,
        /// while preserving any changes or deletions made to the original configuration in the interim,
        /// and not overridden by the current configuration.
        /// </summary>
        /// <param name="current">The configuration in the current state on the server</param>
        /// <param name="modified">The user-modified local configuration</param>
        /// <param name="annotate">If true, update the annotation in <paramref name="modified">modified</paramref> with the value of modified before diffing</param>
        public void CreateThreeWayPatchFromLastApplied(KubeResourceV1 current, KubeResourceV1 modified, JsonPatchDocument patch, bool annotate) {
            KubeResourceV1 original = null;
            string originalJson = current.Metadata.Annotations.GetValueOrDefault(Annotations.LastAppliedConfig);
            if (!String.IsNullOrEmpty(originalJson)) {
                original = (KubeResourceV1)JsonConvert.DeserializeObject(originalJson, current.GetType());
            }
            if (annotate) {
                modified.Metadata.Annotations[Annotations.LastAppliedConfig] = JsonConvert.SerializeObject(modified);
            }
            CreateThreeWayPatch(original: original, modified: modified, current: current, patch: patch);
        }

        /// <summary>
        /// CreateThreeWayMergePatch reconciles a modified configuration with an original configuration,
        /// while preserving any changes or deletions made to the original configuration in the interim,
        /// and not overridden by the current configuration.
        /// </summary>
        /// <param name="original">The original configuration from the annotation in <paramref name="current">current</paramref></param>
        /// <param name="modified">The user-modified local configuration</param>
        /// <param name="current">The configuration in the current state on the server</param>
        public void CreateThreeWayPatch(object original, object modified, object current, JsonPatchDocument patch) {
            CreateTwoWayPatch(current, modified, patch, ignoreDeletions: true);
            CreateTwoWayPatch(original, modified, patch, ignoreAdditionsAndModifications: true);
        }

        /// <summary>
        /// Configures the passed JSON Patch so that it yields modified when applied to original
        /// - Adding fields to the patch present in modified, missing from original
        /// - Setting fields to the patch present in modified and original with different values
        /// - Delete fields present in original, missing from modified through
        /// - IFF map field - set to nil in patch ???
        /// - IFF list of maps && merge strategy - use deleteDirective for the elements ???
        /// - IFF list of primitives && merge strategy - use parallel deletion list ???
        /// - IFF list of maps or primitives with replace strategy (default) - set patch value to the value in modified ???
        /// - Build $retainKeys directive for fields with retainKeys patch strategy ???
        /// </summary>
        /// <param name="original"></param>
        /// <param name="modified"></param>
        /// <param name="path">The JSON pointer to the currently inspected values</param>
        /// <param name="mergeStrategy">The strategy to use for patching (replace or merge with mergeKey)</param>
        public void CreateTwoWayPatch(object original, object modified, JsonPatchDocument patch, string path = "", MergeStrategyAttribute mergeStrategy = null, bool ignoreDeletions = false, bool ignoreAdditionsAndModifications = false) {
            logger.LogTrace($"Path: {path}");
            if (modified == null && original == null) {
                return;
            }
            if (modified == null && original != null) {
                if (!ignoreDeletions) {
                    patch.Replace(path, modified);
                }
                return;
            }
            if (original == null && modified != null) {
                if (!ignoreAdditionsAndModifications) {
                    patch.Replace(path, modified);
                }
                return;
            }

            // From this point, original and modified are known to be non-null
            if (original.GetType() != modified.GetType() && !(modified is KubeClient.Models.Tracked.ITracked && original.GetType().IsAssignableFrom(modified.GetType()))) {
                // Types are not mergable, replace
                patch.Replace(path, modified);
                return;
            }

            // From this point, `original` and `modified` are known to be the same type (besides tacked/non-tracked) and non-null

            var type = modified.GetType();
            logger.LogTrace($"Type: {original.GetType().FullName} {type.FullName}");

            // string, int, float, bool, enum, DateTime
            if (modified is string || type.IsValueType) {
                logger.LogTrace($"Is value type, comparing {original} <-> {modified}");
                // Replace if changed, otherwise do nothing
                // We NEED to use Equals() here instead of != because the static type is object, meaning the scalar is boxed.
                // Since operators are resolved at compile time, this would use the == implementation for object,
                // while Equals() is dynamically dispatched on the real boxed type.
                if (!original.Equals(modified)) {
                    patch.Replace(path, modified);
                }
                return;
            }

            // From this point, original and modified are known to be reference types

            if (System.Object.ReferenceEquals(original, modified)) {
                // Same object, short cut
                return;
            }

            if (modified is IList) {
                logger.LogTrace("Is List");
                // Handle lists
                // Really just casting to generic IEnumerable get access to more LINQ. It's all object anyway.
                IEnumerable<object> originalEnumerable = ((IList)original).Cast<object>();
                IEnumerable<object> modifiedEnumerable = ((IList)modified).Cast<object>();
                // Check if the list property has a strategic merge strategy attribute
                if (mergeStrategy != null) {
                    if (mergeStrategy.Key != null) {
                        logger.LogTrace("List is unordered set keyed by merge key");
                        // The lists are to be treated like dictionaries, keyed by Key
                        logger.LogTrace($"Merge key: {mergeStrategy.Key}");
                        Func<object, object> keySelector = listElement => {
                            PropertyInfo mergeProperty = findJsonProperty(listElement, mergeStrategy.Key);
                            object value = mergeProperty.GetValue(listElement);
                            if (value == null) {
                                throw new Exception($"Merge key {mergeProperty} on type {listElement.GetType().FullName} cannot be null");
                            }
                            logger.LogTrace($"Merge property value: {value}");
                            return value;
                        };
                        // The merge key value is *not* guaranteed to be string,
                        // for example ContainerPortV1 has the merge key ContainerPort which is type int
                        Dictionary<object, object> originalDict = originalEnumerable.ToDictionary(keySelector);
                        Dictionary<object, object> modifiedDict = modifiedEnumerable.ToDictionary(keySelector);
                        var removeOperations = new List<Action>();
                        int index = 0;
                        foreach (var originalElement in originalEnumerable) {
                            object elementKey = findJsonProperty(originalElement, mergeStrategy.Key).GetValue(originalElement);
                            string elementPath = path + "/" + index;
                            if (!modifiedDict.ContainsKey(elementKey)) {
                                if (!ignoreDeletions) {
                                    // Entry removed in modified
                                    // Check that the value at the given index is really the value we want to modify,
                                    // to make sure indexes were not modified on the server
                                    // Queue these up because they shift array indexes around and for simplicity we want to get the modifications add first
                                    // This makes the patch easier to reason about.
                                    removeOperations.Add(() => patch.Test(elementPath + "/" + escapeJsonPointer(mergeStrategy.Key), elementKey));
                                    removeOperations.Add(() => patch.Remove(elementPath));
                                }
                            } else {
                                // Entry present in both, merge recursively
                                patch.Test(elementPath + "/" + escapeJsonPointer(mergeStrategy.Key), elementKey);
                                var countBefore = patch.Operations.Count;
                                CreateTwoWayPatch(
                                    original: originalElement,
                                    modified: modifiedDict[elementKey],
                                    patch: patch,
                                    path: elementPath,
                                    ignoreDeletions: ignoreDeletions,
                                    ignoreAdditionsAndModifications: ignoreAdditionsAndModifications
                                );
                                if (patch.Operations.Count == countBefore) {
                                    // Test was not needed, element was not modified
                                    patch.Operations.RemoveAt(patch.Operations.Count - 1);
                                }
                            }
                            index++;
                        }
                        // Modifications are done, add remove operations
                        foreach (var action in removeOperations) {
                            action();
                        }
                        if (!ignoreAdditionsAndModifications) {
                            // Entries added in modified
                            foreach (var modifiedEntry in modifiedDict) {
                                if (!originalDict.ContainsKey(modifiedEntry.Key)) {
                                    // An element that was added in modified
                                    patch.Add(path + "/-", modifiedEntry.Value);
                                }
                            }
                        }
                    } else {
                        logger.LogTrace("List is unordered set");
                        // Lists are to be treated like unordered sets
                        HashSet<object> originalSet = originalEnumerable.ToHashSet();
                        HashSet<object> modifiedSet = modifiedEnumerable.ToHashSet();
                        // The index to adress the element on the server after applying every operation in the patch so far.
                        int index = 0;
                        foreach (var originalElement in originalEnumerable) {
                            string elementPath = path + "/" + index;
                            if (!modifiedSet.Contains(originalElement)) {
                                // Deleted from modified
                                if (!ignoreDeletions) {
                                    // When patching indexes, make sure elements didn't get moved around on the server
                                    // Can directly add them here because unordered sets do not use replace operations,
                                    // only remove and adding to the end
                                    patch.Test(elementPath, originalElement);
                                    patch.Remove(elementPath);
                                }
                            }
                            // Present in both: do nothing
                            index++;
                        }
                        if (!ignoreAdditionsAndModifications) {
                            foreach (var modifiedElement in modifiedSet) {
                                if (!originalSet.Contains(modifiedElement)) {
                                    // Added in modified
                                    patch.Add(path + "/-", modifiedElement);
                                }
                            }
                        }
                    }
                } else {
                    logger.LogTrace("List is ordered list");
                    // List is to be treated as an ordered list, e.g. ContainerV1.Command
                    List<object> originalList = originalEnumerable.ToList();
                    List<object> modifiedList = modifiedEnumerable.ToList();
                    var removeOperations = new List<Action>();
                    int index = 0;
                    foreach (var originalElement in originalList.Take(modifiedList.Count)) {
                        string elementPath = path + "/" + index;
                        if (index >= modifiedList.Count) {
                            // Not present in modified, remove
                            if (!ignoreDeletions) {
                                removeOperations.Add(() => patch.Test(elementPath, originalElement));
                                removeOperations.Add(() => patch.Remove(elementPath));
                            }
                        } else {
                            // Present in both, merge recursively
                            // Add a test to check that indexes were not moved on the server
                            patch.Test(elementPath, originalElement);
                            int countBefore = patch.Operations.Count;
                            CreateTwoWayPatch(
                                original: originalElement,
                                modified: modifiedList[index],
                                patch: patch,
                                path: elementPath,
                                ignoreDeletions: ignoreDeletions,
                                ignoreAdditionsAndModifications: ignoreAdditionsAndModifications
                            );
                            if (patch.Operations.Count == countBefore) {
                                // Test was not needed, element was not modified
                                patch.Operations.RemoveAt(patch.Operations.Count - 1);
                            }
                        }
                        index++;
                    }
                    // Modifications are done, register remove operations
                    foreach (var action in removeOperations) {
                        action();
                    }
                    // Continue on modifiedList (if it's longer) to add added elements
                    for (; index < modifiedList.Count; index++) {
                        // Added in modifiedList
                        object addedElement = modifiedList[index];
                        patch.Add(path + "/-", addedElement);
                    }
                }
            } else if (modified is IDictionary) {
                logger.LogTrace("Is Dictionary");
                // Handle maps (e.g. KubeResourceV1.Annotations)
                IDictionary originalDict = (IDictionary)original;
                IDictionary modifiedDict = (IDictionary)modified;
                // Always merge maps
                foreach (DictionaryEntry originalEntry in originalDict) {
                    string entryKey = (string)originalEntry.Key;
                    object entryValue = (object)originalEntry.Value;
                    string entryPath = path + "/" + escapeJsonPointer(entryKey);
                    if (!modifiedDict.Contains(originalEntry.Key)) {
                        if (!ignoreDeletions) {
                            // Entry removed in modified
                            patch.Remove(entryPath);
                        }
                    } else {
                        // Entry present in both, merge recursively
                        CreateTwoWayPatch(
                            original: entryValue,
                            modified: modifiedDict[originalEntry.Key],
                            patch: patch,
                            path: entryPath,
                            ignoreDeletions: ignoreDeletions,
                            ignoreAdditionsAndModifications: ignoreAdditionsAndModifications
                        );
                    }
                }
                if (!ignoreAdditionsAndModifications) {
                    // Entries added in modified
                    foreach (DictionaryEntry modifiedEntry in modifiedDict) {
                        string entryKey = (string)modifiedEntry.Key;
                        object entryValue = (object)modifiedEntry.Value;
                        if (!originalDict.Contains(entryKey)) {
                            // An element that was added in modified
                            patch.Add(path + "/" + escapeJsonPointer(entryKey), entryValue);
                        }
                    }
                }
            } else {
                logger.LogTrace("Is other object");
                if (original is KubeResourceV1 && !String.IsNullOrEmpty(((KubeResourceV1)original).Metadata.ResourceVersion)) {
                    // resourceVersion: a string that identifies the internal version of this object that can be used by
                    // clients to determine when objects have changed. This value MUST be treated as opaque by clients
                    // and passed unmodified back to the server.
                    // https://github.com/kubernetes/community/blob/master/contributors/devel/api-conventions.md#metadata
                    // Add this test before traversing into other properties
                    patch.Test(path + "/resourceVersion", ((KubeResourceV1)original).Metadata.ResourceVersion);
                }
                // KubeObjects, compare properties recursively
                foreach (PropertyInfo originalProp in original.GetType().GetProperties()) {
                    if (original is KubeClient.Models.Tracked.ITracked && originalProp.Name == "__ModifiedProperties__") {
                        continue;
                    }
                    PropertyInfo modifiedProp = modified.GetType().GetProperty(originalProp.Name);
                    JsonPropertyAttribute jsonAttribute = (JsonPropertyAttribute)originalProp.GetCustomAttribute(typeof(JsonPropertyAttribute));
                    string propPath = path + "/" + escapeJsonPointer(jsonAttribute.PropertyName);
                    object originalValue = originalProp.GetValue(original);
                    object modifiedValue = modifiedProp.GetValue(modified);

                    if (!isPropertyUpdateable(original, originalProp)) {
                        continue;
                    }

                    // Pass patch strategy attribute to diff function for the property we're looking at
                    MergeStrategyAttribute attribute = (MergeStrategyAttribute)Attribute.GetCustomAttribute(originalProp, typeof(MergeStrategyAttribute));
                    CreateTwoWayPatch(
                        original: originalValue,
                        modified: modifiedValue,
                        patch: patch,
                        path: propPath,
                        mergeStrategy: attribute,
                        ignoreDeletions: ignoreDeletions,
                        ignoreAdditionsAndModifications: ignoreAdditionsAndModifications
                    );
                }
            }
        }

        private static PropertyInfo findJsonProperty(object obj, string jsonKey) {
            return obj.GetType().GetProperties().First(prop => {
                var attr = (JsonPropertyAttribute)prop.GetCustomAttribute(typeof(JsonPropertyAttribute));
                return attr?.PropertyName == jsonKey;
            });
        }

        private static string escapeJsonPointer(string referenceToken) {
            return referenceToken.Replace("~", "~0").Replace("/", "~1");
        }

        private bool isPropertyUpdateable(object obj, PropertyInfo prop) {
            foreach (var typePropsEntry in nonUpdateableTypes) {
                var type = typePropsEntry.Key;
                var nonUpdateableProperties = typePropsEntry.Value;
                if (type.IsInstanceOfType(obj) && nonUpdateableProperties.Contains(prop.Name)) {
                    return false;
                }
            }
            return true;
        }
    }
}
