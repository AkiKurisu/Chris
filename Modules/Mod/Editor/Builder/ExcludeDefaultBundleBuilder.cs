using System.Linq;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
namespace Chris.Mod.Editor
{
    [CreateAssetMenu(fileName = "ExcludeDefaultBundleBuilder", menuName = "Mod/Builder/ExcludeDefaultBundleBuilder")]
    public class ExcludeDefaultBundleBuilder : CustomBuilder
    {
        public override string Description => "Exclude default group bundle from build.";
        
        private AddressableAssetGroup defaultGroup;
        
        public override void Build(ModExportConfig exportConfig, string buildPath)
        {
            defaultGroup = AddressableAssetSettingsDefaultObject.Settings.groups.FirstOrDefault(x => !x.HasSchema<BundledAssetGroupSchema>());
            if (defaultGroup) AddressableAssetSettingsDefaultObject.Settings.groups.Remove(defaultGroup);
        }
        
        public override void Cleanup(ModExportConfig exportConfig)
        {
            if (defaultGroup) AddressableAssetSettingsDefaultObject.Settings.groups.Insert(0, defaultGroup);
        }
    }
}