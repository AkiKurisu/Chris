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
        /// Create a generic soft asset reference from object
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public static SoftAssetReference<T> FromTObject<T>(T asset, string groupName = null) where T : UObject
        {
            return FromObject(asset, groupName);
        }

        /// <summary>
        /// Move <see cref="SoftAssetReference"/> asset to target <see cref="AddressableAssetGroup"/> safe in editor
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="group"></param>
        /// <param name="labels">Asset labels need assigned with</param>
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
        /// <param name="group"></param>
        /// <param name="postEvent"></param>
        /// <returns></returns>
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
        
        public static void CleanupAssetGroup(AddressableAssetGroup assetGroup)
        {
            assetGroup.RemoveAssetEntries(assetGroup.entries.ToArray());
        }

        public static string GetAssetGUID(this UObject asset)
        {
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
        }
    }
}
