using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
namespace Chris.Mod.Editor
{
    [CreateAssetMenu(fileName = "ModExportConfig", menuName = "Chris/Mod/ModExportConfig")]
    public class ModExportConfig : ScriptableObject
    {
        public string authorName = "Default";
        
        public string modName = "Mod";
        
        public string version = "1.0";
        
        [Multiline]
        public string description;
        
        [Tooltip("Texture need to be set as Readable and format is RGBA32Bit")]
        public Texture2D modIcon;
        
        public CustomBuilder[] customBuilders;
        
        [HideInInspector]
        public string lastExportPath;
        
        internal bool Validate()
        {
            if (string.IsNullOrEmpty(authorName)) return false;
            if (string.IsNullOrEmpty(modName)) return false;
            if (string.IsNullOrEmpty(version)) return false;
            return true;
        }
        
        public AddressableAssetGroup Group => ModBuildUtility.GetOrCreateGroup($"Mod_{modName}");
    }
}
