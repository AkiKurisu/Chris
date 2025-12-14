using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
#if (UNITY_6000_0_OR_NEWER && !ENABLE_JSON_CATALOG)
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.Util;
#else
using UnityEngine;
#endif

namespace Chris.Resource.Editor
{
    public class AddressableAssetBuilder : IResourceBuilder
    {
        private bool _buildRemoteCatalog;

        private Dictionary<BundledAssetGroupSchema, bool> _includeInBuildMap;

        public void Build(ResourceExportContext context)
        {
            string buildPath = context.BuildPath;
            // Force enable remote catalog
            _buildRemoteCatalog = AddressableAssetSettingsDefaultObject.Settings.BuildRemoteCatalog;
            AddressableAssetSettingsDefaultObject.Settings.BuildRemoteCatalog = true;
            AddressableAssetSettingsDefaultObject.Settings.RemoteCatalogBuildPath.SetVariableByName(AddressableAssetSettingsDefaultObject.Settings, AddressableAssetSettings.kRemoteBuildPath);
            AddressableAssetSettingsDefaultObject.Settings.RemoteCatalogLoadPath.SetVariableByName(AddressableAssetSettingsDefaultObject.Settings, AddressableAssetSettings.kRemoteLoadPath);

            _includeInBuildMap = new Dictionary<BundledAssetGroupSchema, bool>();
            foreach (var group in AddressableAssetSettingsDefaultObject.Settings.groups)
            {
                if (group.HasSchema<BundledAssetGroupSchema>())
                {
                    var schema = group.GetSchema<BundledAssetGroupSchema>();
                    _includeInBuildMap[schema] = schema.IncludeInBuild;
                    schema.IncludeInBuild = context.AssetGroupFilter(group);
                }
            }

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            settings.profileSettings.SetValue(settings.activeProfileId, "Remote.LoadPath", ResourceSystem.DynamicLoadPath);
            settings.profileSettings.SetValue(settings.activeProfileId, "Remote.BuildPath", buildPath);
        }

        public void Cleanup(ResourceExportContext context)
        {
            // Reset build setting
            AddressableAssetSettingsDefaultObject.Settings.BuildRemoteCatalog = _buildRemoteCatalog;

            foreach (var group in AddressableAssetSettingsDefaultObject.Settings.groups)
            {
                if (group.HasSchema<BundledAssetGroupSchema>())
                {
                    var schema = group.GetSchema<BundledAssetGroupSchema>();
                    schema.IncludeInBuild = _includeInBuildMap[schema];
                }
            }
            _includeInBuildMap.Clear();
            EditorUtility.SetDirty(AddressableAssetSettingsDefaultObject.Settings);
            AssetDatabase.SaveAssetIfDirty(AddressableAssetSettingsDefaultObject.Settings);
            {
                var bundles = Directory.GetFiles(Addressables.BuildPath, "*.bundle", SearchOption.AllDirectories);
                var bundleNames = bundles.Select(Path.GetFileName).ToList();
                // Copy default bundles to build path
                foreach (string bundleFilePath in bundles)
                {
                    string bundleFileName = Path.GetFileName(bundleFilePath);
                    string destinationFilePath = Path.Combine(context.BuildPath, bundleFileName);
                    File.Copy(bundleFilePath, destinationFilePath, true);
                }

                // Modify catalog to update bundle paths
#if (!UNITY_6000_0_OR_NEWER || ENABLE_JSON_CATALOG)
                // JSON Catalog processing (Unity < 6 or Unity 6 with JSON enabled)
                var catalogPath = Directory.GetFiles(context.BuildPath, "*.json")[0];
                var catalog = JsonUtility.FromJson<ContentCatalogData>(File.ReadAllText(catalogPath));
                for (int i = 0; i < catalog.InternalIds.Length; ++i)
                {
                    foreach (var bundleName in bundleNames)
                    {
                        if (catalog.InternalIds[i].Contains(bundleName))
                        {
                            catalog.InternalIds[i] = $"{ResourceSystem.DynamicLoadPath}/{bundleName}";
                            break;
                        }
                    }
                }
                File.Delete(catalogPath);
                string newCatalogPath = Path.Combine(context.BuildPath, "catalog.json");
                File.WriteAllText(newCatalogPath, JsonUtility.ToJson(catalog));
                // Replace hash file
                string hashPath = catalogPath.Replace(".json", ".hash");
                File.Copy(hashPath, newCatalogPath.Replace(".json", ".hash"));
                File.Delete(hashPath);
#else
                // Binary Catalog processing for Unity 6 (without JSON enabled)
                var catalogPath = Directory.GetFiles(context.BuildPath, "*.bin")[0];
                
                // Load the binary catalog
                var data = File.ReadAllBytes(catalogPath);
                var reader = new BinaryStorageBuffer.Reader(data, 1024, 1024, new ContentCatalogData.Serializer().WithInternalIdResolvingDisabled());
                var catalogData = reader.ReadObject<ContentCatalogData>(0, out _, false);
                
                // Create locator to access binary catalog data
                var locator = catalogData.CreateCustomLocator();
                
                // Build a map of primary key to location and keys
                var pkToLoc = new Dictionary<string, (IResourceLocation, HashSet<object>)>();
                foreach (var key in locator.Keys)
                {
                    if (locator.Locate(key, typeof(object), out var locs))
                    {
                        foreach (var loc in locs)
                        {
                            if (!pkToLoc.TryGetValue(loc.PrimaryKey, out var locKeys))
                                pkToLoc.Add(loc.PrimaryKey, locKeys = (loc, new HashSet<object>()));
                            locKeys.Item2.Add(key);
                        }
                    }
                }
                
                // Create new modified entries
                var modifiedEntries = new List<ContentCatalogDataEntry>();
                foreach (var kvp in pkToLoc)
                {
                    var loc = kvp.Value.Item1;
                    string modifiedInternalId = loc.InternalId;
                    
                    // Check if this entry references any of our bundles
                    foreach (var bundleName in bundleNames)
                    {
                        if (loc.InternalId.Contains(bundleName))
                        {
                            modifiedInternalId = $"{ResourceSystem.DynamicLoadPath}/{bundleName}";
                            break;
                        }
                    }
                    
                    // Collect dependencies
                    List<object> deps = null;
                    if (loc.HasDependencies)
                    {
                        deps = new List<object>();
                        foreach (var d in loc.Dependencies)
                            deps.Add(d.PrimaryKey);
                    }
                    
                    // Create new entry with modified InternalId
                    var newEntry = new ContentCatalogDataEntry(
                        loc.ResourceType,
                        modifiedInternalId,
                        loc.ProviderId,
                        kvp.Value.Item2, // keys
                        deps,
                        loc.Data
                    );
                    modifiedEntries.Add(newEntry);
                }
                
                // Create new catalog with modified data
                var newCatalog = new ContentCatalogData(modifiedEntries, catalogData.ProviderId)
                {
                    BuildResultHash = catalogData.BuildResultHash,
                    InstanceProviderData = catalogData.InstanceProviderData,
                    SceneProviderData = catalogData.SceneProviderData,
                    ResourceProviderData = catalogData.ResourceProviderData
                };
                newCatalog.SetData(modifiedEntries);
                
                // Save the modified catalog
                File.Delete(catalogPath);
                string newCatalogPath = Path.Combine(context.BuildPath, "catalog.bin");
                newCatalog.SaveToFile(newCatalogPath);
                
                // Replace hash file
                string hashPath = catalogPath.Replace(".bin", ".hash");
                File.Copy(hashPath, newCatalogPath.Replace(".bin", ".hash"), true);
                File.Delete(hashPath);
#endif
            }
        }
    }
}
