using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
namespace Chris.Mod.Editor
{
    public interface IModBuilder
    {
        /// <summary>
        /// Preprocess for mod assets
        /// </summary>
        /// <param name="exportConfig"></param>
        /// <param name="buildPath"></param>
        void Build(ModExportConfig exportConfig, string buildPath);
        
        /// <summary>
        /// Write meta data
        /// </summary>
        /// <param name="modInfo"></param>
        void Write(ref ModInfo modInfo);
        
        /// <summary>
        /// Clean after build
        /// </summary>
        /// <param name="exportConfig"></param>
        void Cleanup(ModExportConfig exportConfig);
    }
    
    public class ModBuildUtility
    {
        public static string GetAssetGUID(Object asset)
        {
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
        }
        
        public static string AddAssetToGroupSimplify(AddressableAssetGroup group, List<AddressableAssetEntry> entries, Object asset, params string[] labels)
        {
            if (asset == null) return null;
            var guid = GetAssetGUID(asset);
            if (guid == null)
            {
                Debug.Log($"Can't find {asset} !");
                return null;
            }
            var entry = AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(guid, group, false, false);
            if (labels != null)
            {
                for (int i = 0; i < labels.Length; i++) entry.SetLabel(labels[i], true, true, false);
            }
            string address = Path.GetFileNameWithoutExtension(entry.address);
            //Simplify Address
            entry.SetAddress(address, false);
            entries.Add(entry);
            return address;
        }
        
        public static string AddAssetToGroup(AddressableAssetGroup group, List<AddressableAssetEntry> entries, Object asset, params string[] labels)
        {
            if (asset == null) return null;
            var guid = GetAssetGUID(asset);
            if (guid == null)
            {
                Debug.Log($"Can't find {asset} !");
                return null;
            }
            var entry = AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(guid, group, false, false);
            if (labels != null)
            {
                for (int i = 0; i < labels.Length; i++) entry.SetLabel(labels[i], true, true, false);
            }
            //Simplify Address
            entries.Add(entry);
            return entry.address;
        }
        
        public static AddressableAssetGroup GetOrCreateGroup(string groupName)
        {
            var schemas = AddressableAssetSettingsDefaultObject.Settings.DefaultGroup.Schemas;
            return AddressableAssetSettingsDefaultObject.Settings.FindGroup(groupName)
                ?? AddressableAssetSettingsDefaultObject.Settings.CreateGroup(groupName, false, false, true, schemas);
        }
        
        public static void CleanUpAssetGroup(AddressableAssetGroup assetGroup)
        {
            foreach (AddressableAssetEntry entry in assetGroup.entries.ToList())
                assetGroup.RemoveAssetEntry(entry);
        }
    }
}
