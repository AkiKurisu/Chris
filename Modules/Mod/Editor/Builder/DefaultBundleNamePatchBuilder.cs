using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEngine;
namespace Chris.Mod.Editor
{
    [CreateAssetMenu(fileName = "DefaultBundleNamePatchBuilder", menuName = "Mod/Builder/DefaultBundleNamePatchBuilder")]
    public class DefaultBundleNamePatchBuilder : CustomBuilder
    {
        public override string Description => "Use mod name for naming shader bundle and monoScript bundle." +
                " If you build mod in source project and use project hash as name, use this builder for preventing bundle conflict.";
        
        public override void Build(ModExportConfig exportConfig, string buildPath)
        {
            AddressableAssetSettingsDefaultObject.Settings.ShaderBundleNaming = ShaderBundleNaming.Custom;
            AddressableAssetSettingsDefaultObject.Settings.ShaderBundleCustomNaming = $"Mod_{exportConfig.modName}_Shader";
            AddressableAssetSettingsDefaultObject.Settings.MonoScriptBundleNaming = MonoScriptBundleNaming.Custom;
            AddressableAssetSettingsDefaultObject.Settings.MonoScriptBundleCustomNaming = $"Mod_{exportConfig.modName}_MonoScript";
        }

        public override void Cleanup(ModExportConfig exportConfig)
        {
            AddressableAssetSettingsDefaultObject.Settings.ShaderBundleNaming = ShaderBundleNaming.ProjectName;
            AddressableAssetSettingsDefaultObject.Settings.MonoScriptBundleNaming = MonoScriptBundleNaming.ProjectName;
        }
    }
}