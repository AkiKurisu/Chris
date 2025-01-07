using System;
using Chris.DataDriven;
using Chris.Resource;
using UnityEngine;
namespace Chris.Gameplay.Level
{
    [Serializable, AddressableDataTable(address:LevelSceneDataTableManager.TableKey)]
    public class LevelSceneRow : IDataTableRow
    {
        [LevelName]
        public string levelName;
        
        /// <summary>
        /// Soft reference (address) to the real scene asset
        /// </summary>
#if UNITY_EDITOR
        [AssetReferenceConstraint(typeof(UnityEditor.SceneAsset))]
#endif
        public SoftAssetReference reference;
        
        public LoadLevelMode loadMode;
        
        public LoadLevelPolicy loadPolicy = LoadLevelPolicy.AllPlatform;
        
        public bool ValidateLoadPolicy()
        {
            if (Application.isMobilePlatform) return loadPolicy.HasFlag(LoadLevelPolicy.Mobile);
            if (Application.isConsolePlatform) return loadPolicy.HasFlag(LoadLevelPolicy.Console);
            return loadPolicy.HasFlag(LoadLevelPolicy.PC);
        }
    }
}
