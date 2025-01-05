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
namespace Chris.Resource.Editor
{
    public static class SoftAssetReferenceEditorUtils
    {
        private static readonly Dictionary<string, SoftObjectHandle> refDic = new();

        static SoftAssetReferenceEditorUtils()
        {
            // Cleanup cache since SoftObjectHandle is not valid anymore
            GlobalObjectManager.OnGlobalObjectCleanup += () => refDic.Clear();
        }
        
        /// <summary>
        /// Optimized fast api for load asset from guid in editor
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static UObject GetAssetFromGUID(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;

            if (!refDic.TryGetValue(guid, out var handle))
            {
                var uObject = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(UObject));
                if (uObject)
                {
                    GlobalObjectManager.RegisterObject(uObject, ref handle);
                    refDic[guid] = handle;
                    return uObject;
                }
                return null;
            }
            var cacheObject = handle.GetObject();
            if (cacheObject) return cacheObject;

            GlobalObjectManager.UnregisterObject(handle);
            var newObject = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(UObject));
            if (newObject)
            {
                GlobalObjectManager.RegisterObject(newObject, ref handle);
                refDic[guid] = handle;
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
            return (SoftAssetReference<T>)FromObject(asset, groupName);
        }
        
        /// <summary>
        /// Move reference object safe in editor
        /// </summary>
        /// <param name="reference"></param>
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
                Debug.LogError($"[Resource Editor] Can't find {asset} !");
                return null;
            }
            var entry = AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(guid, group, false, false);
            if (labels != null)
            {
                for (int i = 0; i < labels.Length; i++) entry.SetLabel(labels[i], true, true, false);
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

        public static string GetAssetGUID(this UObject asset)
        {
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
        }
    }
}
