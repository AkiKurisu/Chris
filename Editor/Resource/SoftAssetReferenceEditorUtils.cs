using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.Collections.Generic;
using Chris.Serialization;
using UObject = UnityEngine.Object;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine.Assertions;
using UnityEngine.Pool;

namespace Chris.Resource.Editor
{
    public static class SoftAssetReferenceEditorUtils
    {
        private static readonly Dictionary<string, SoftObjectHandle> RefDic = new();

        static SoftAssetReferenceEditorUtils()
        {
            // Cleanup cache since SoftObjectHandle is not valid anymore
            GlobalObjectManager.OnGlobalObjectCleanup += () => RefDic.Clear();
        }

        /// <summary>
        /// Optimized fast api for load asset from guid in editor
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static UObject GetAssetFromGUID(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;

            if (!RefDic.TryGetValue(guid, out var handle))
            {
                var uObject = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(UObject));
                if (uObject)
                {
                    GlobalObjectManager.RegisterObject(uObject, ref handle);
                    RefDic[guid] = handle;
                    return uObject;
                }
                return null;
            }
            var cacheObject = handle.GetObject() as UObject;
            if (cacheObject) return cacheObject;

            GlobalObjectManager.UnregisterObject(handle);
            var newObject = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(UObject));
            if (newObject)
            {
                GlobalObjectManager.RegisterObject(newObject, ref handle);
                RefDic[guid] = handle;
            }
            return newObject;
        }

        /// <summary>
        /// Get asset from soft asset reference, fallback to addressable system if GUID lookup fails
        /// </summary>
        /// <param name="softAssetReference">The soft asset reference to load</param>
        /// <returns>The loaded asset or null if not found</returns>
        public static UObject GetAsset(SoftAssetReference softAssetReference)
        {
            if (string.IsNullOrEmpty(softAssetReference.Address)) return null;
            var asset = GetAssetFromGUID(softAssetReference.Guid);
            if (!asset)
            {
                asset = ResourceSystem.LoadAssetAsync<UObject>(softAssetReference.Address).WaitForCompletion();
            }
            return asset;
        }

        /// <summary>
        /// Create a soft asset reference from object
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public static SoftAssetReference FromObject(UObject asset, string groupName = null)
        {
            if (!asset)
            {
                return new SoftAssetReference();
            }
            var reference = new SoftAssetReference { Guid = asset.GetAssetGUID(), Locked = true };
            var existingEntry = asset.ToAddressableAssetEntry();
            if (existingEntry != null)
            {
                reference.Address = existingEntry.address;
            }
            else
            {
                AddressableAssetGroup assetGroup;
                if (string.IsNullOrEmpty(groupName))
                    assetGroup = AddressableAssetSettingsDefaultObject.Settings.DefaultGroup;
                else
                    assetGroup = ResourceEditorUtils.GetOrCreateAssetGroup(groupName);
                var entry = assetGroup.AddAsset(asset);
                assetGroup.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, false, true);
                reference.Address = entry.address;
            }
            return reference;
        }

        /// <summary>
        /// Create a generic soft asset reference from object with type safety
        /// </summary>
        /// <typeparam name="T">The type of the asset, must inherit from UnityEngine.Object</typeparam>
        /// <param name="asset">The asset to create reference from</param>
        /// <param name="groupName">Optional addressable group name, uses default if null</param>
        /// <returns>A typed soft asset reference</returns>
        public static SoftAssetReference<T> FromTObject<T>(T asset, string groupName = null) where T : UObject
        {
            return FromObject(asset, groupName);
        }

        /// <summary>
        /// Move <see cref="SoftAssetReference"/> asset to target <see cref="AddressableAssetGroup"/> safe in editor
        /// </summary>
        /// <param name="reference">The soft asset reference to move</param>
        /// <param name="group">The target addressable asset group</param>
        /// <param name="labels">Asset labels to assign to the moved asset</param>
        public static void MoveSoftReferenceObject(ref SoftAssetReference reference, AddressableAssetGroup group, params string[] labels)
        {
            var uObject = GetAssetFromGUID(reference.Guid);
            if (!uObject) return;

            var newEntry = group.AddAsset(uObject, labels);
            reference.Address = newEntry.address;
        }
    }

    public static class ResourceEditorUtils
    {
        public class AssetGroupModificationScope : IDisposable
        {
            private readonly AddressableAssetGroup _group;

            private readonly List<AddressableAssetEntry> _entries;

            private readonly bool _postEvent;

            public AssetGroupModificationScope(AddressableAssetGroup group, bool postEvent)
            {
                _group = group;
                _postEvent = postEvent;
                if (!_group) return;

                _entries = ListPool<AddressableAssetEntry>.Get();
                StartModification(_group, this);
            }

            internal void AddEntry(AddressableAssetEntry entry)
            {
                if (!_group) return;

                _entries.Add(entry);
            }

            public void Dispose()
            {
                if (!_group) return;

                if (_entries.Any())
                {
                    // Notify asset group dirty
                    _group.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, _entries, _postEvent, true);
                }
                ListPool<AddressableAssetEntry>.Release(_entries);
                EndModification(_group);
            }
        }

        private static readonly Dictionary<AddressableAssetGroup, AssetGroupModificationScope> Scopes = new();

        /// <summary>
        /// Create a modification scope and marks the group as modified when any entry was added in the scope.
        /// </summary>
        /// <param name="group">The addressable asset group to modify</param>
        /// <param name="postEvent">Whether to post modification events</param>
        /// <returns>A disposable scope for managing group modifications</returns>
        public static AssetGroupModificationScope Modify(this AddressableAssetGroup group, bool postEvent = false)
        {
            return new AssetGroupModificationScope(group, postEvent);
        }

        private static void StartModification(AddressableAssetGroup group, AssetGroupModificationScope scope)
        {
            Scopes.Add(group, scope);
        }

        private static void EndModification(AddressableAssetGroup group)
        {
            Scopes.Remove(group);
        }

        /// <summary>
        /// Get existing addressable asset group or create a new one if it doesn't exist
        /// </summary>
        /// <param name="groupName">The name of the group to get or create</param>
        /// <returns>The addressable asset group</returns>
        public static AddressableAssetGroup GetOrCreateAssetGroup(string groupName)
        {
            var group = AddressableAssetSettingsDefaultObject.Settings.groups.FirstOrDefault(x => x.name == groupName);
            if (group) return group;
            group = AddressableAssetSettingsDefaultObject.Settings.CreateGroup(groupName, false, false, true, AddressableAssetSettingsDefaultObject.Settings.DefaultGroup.Schemas);
            // Ensure address is included in build
            BundledAssetGroupSchema infoSchema = group.GetSchema<BundledAssetGroupSchema>();
            infoSchema.IncludeAddressInCatalog = true;
            return group;
        }

        /// <summary>
        /// Add an asset to the specified addressable asset group with optional labels
        /// </summary>
        /// <param name="group">The addressable asset group to add the asset to</param>
        /// <param name="asset">The asset to add</param>
        /// <param name="labels">Optional labels to assign to the asset</param>
        /// <returns>The created addressable asset entry or null if failed</returns>
        public static AddressableAssetEntry AddAsset(this AddressableAssetGroup group, UObject asset, params string[] labels)
        {
            Assert.IsNotNull(group);
            if (asset == null) return null;

            var guid = asset.GetAssetGUID();
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"Can't find {asset}!");
                return null;
            }
            var entry = AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(guid, group, false, false);
            if (labels != null)
            {
                for (int i = 0; i < labels.Length; i++)
                {
                    entry.SetLabel(labels[i], true, true, false);
                }
            }

            if (Scopes.TryGetValue(group, out var scope))
            {
                scope.AddEntry(entry);
            }
            return entry;
        }

        /// <summary>
        /// Convert a Unity object to its corresponding addressable asset entry
        /// </summary>
        /// <param name="asset">The Unity object to convert</param>
        /// <returns>The addressable asset entry or null if not found</returns>
        public static AddressableAssetEntry ToAddressableAssetEntry(this UObject asset)
        {
            var entries = new List<AddressableAssetEntry>();
            var assetType = asset.GetType();
            AddressableAssetSettingsDefaultObject.Settings.GetAllAssets(entries, false, null,
                                    e =>
                                    {
                                        if (e == null) return false;
                                        var type = AssetDatabase.GetMainAssetTypeAtPath(e.AssetPath);
                                        if (type == null) return false;
                                        return type == assetType || type.IsSubclassOf(assetType);
                                    });
            string path = AssetDatabase.GetAssetPath(asset);
            return entries.FirstOrDefault(x => x.AssetPath == path);
        }

        /// <summary>
        /// Find an addressable asset entry by address and asset type
        /// </summary>
        /// <param name="address">The addressable address to search for</param>
        /// <param name="assetType">The type of asset to search for</param>
        /// <returns>The matching addressable asset entry or null if not found</returns>
        public static AddressableAssetEntry FindAssetEntry(string address, Type assetType)
        {
            var entries = new List<AddressableAssetEntry>();
            AddressableAssetSettingsDefaultObject.Settings.GetAllAssets(entries, false, null,
                                    e =>
                                    {
                                        if (e == null) return false;
                                        var type = AssetDatabase.GetMainAssetTypeAtPath(e.AssetPath);
                                        if (type == null) return false;
                                        return (type == assetType || type.IsSubclassOf(assetType)) && e.address == address;
                                    });
            return entries.FirstOrDefault();
        }

        /// <summary>
        /// Remove all asset entries from the specified addressable asset group
        /// </summary>
        /// <param name="assetGroup">The addressable asset group to clean up</param>
        public static void CleanupAssetGroup(AddressableAssetGroup assetGroup)
        {
            assetGroup.RemoveAssetEntries(assetGroup.entries.ToArray());
        }

        /// <summary>
        /// Get the GUID of a Unity asset
        /// </summary>
        /// <param name="asset">The Unity object to get GUID for</param>
        /// <returns>The asset GUID string</returns>
        public static string GetAssetGUID(this UObject asset)
        {
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
        }

        /// <summary>
        /// Remove multiple addressable asset groups from the settings
        /// </summary>
        /// <param name="assetGroups">The list of addressable asset groups to remove</param>
        public static void RemoveAssetGroups(AddressableAssetGroup[] assetGroups)
        {
            var groups = new List<AddressableAssetGroup>();
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var group in assetGroups)
                {
                    if (group == null) continue;
                    settings.RemoveGroupInternal(group, true, false);
                    groups.Add(group);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupRemoved, groups, true, true);
        }
    }
}
