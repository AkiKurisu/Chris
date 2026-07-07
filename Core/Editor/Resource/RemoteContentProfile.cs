using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Chris.Resource.Editor
{
    /// <summary>
    /// Configuration asset for building one local remote Addressables package.
    /// </summary>
    [CreateAssetMenu(fileName = "RemoteContentProfile", menuName = "Chris/Resource/Remote Content Profile")]
    public class RemoteContentProfile : ScriptableObject
    {
        [SerializeField]
        private string packageName = "RemoteAssets";

        [SerializeField]
        private string version = "1.0.0";

        [SerializeField]
        private string outputRoot = "Export";

        [SerializeField]
        private bool zipOutput = true;

        [SerializeField]
        private List<AddressableAssetGroup> assetGroups = new();

        public string PackageName => packageName;

        public string Version => version;

        public string OutputRoot => outputRoot;

        public bool ZipOutput => zipOutput;

        public string SafePackageName => CreateSafeFileName(packageName, "RemoteAssets");

        public IReadOnlyList<AddressableAssetGroup> AssetGroups => assetGroups;

        /// <summary>
        /// Allows derived profiles to add groups from project-specific metadata.
        /// </summary>
        public virtual void CollectBuildGroups(List<AddressableAssetGroup> groups)
        {
            groups.AddRange(assetGroups);
        }

        public List<AddressableAssetGroup> GetBuildGroups()
        {
            var groups = new List<AddressableAssetGroup>();
            CollectBuildGroups(groups);
            return NormalizeGroups(groups);
        }

        public void AddGroups(IEnumerable<AddressableAssetGroup> groups)
        {
            if (groups == null) return;

            var knownGuids = new HashSet<string>(assetGroups.Where(group => group).Select(group => group.Guid));
            foreach (var group in groups)
            {
                if (!group || string.IsNullOrEmpty(group.Guid) || !knownGuids.Add(group.Guid))
                {
                    continue;
                }

                assetGroups.Add(group);
            }
        }

        public void ClearGroups()
        {
            assetGroups.Clear();
        }

        public void RemoveGroup(AddressableAssetGroup group)
        {
            if (!group) return;

            assetGroups.RemoveAll(existing => existing == group);
        }

        public void RemoveMissingGroups()
        {
            assetGroups.RemoveAll(group => !group);
        }

        public RemoteContentProfileValidation ValidateProfile()
        {
            var validation = new RemoteContentProfileValidation();
            if (string.IsNullOrWhiteSpace(packageName))
            {
                validation.Errors.Add("Package name is empty.");
            }

            if (string.IsNullOrWhiteSpace(version))
            {
                validation.Warnings.Add("Version is empty.");
            }

            if (!AddressableAssetSettingsDefaultObject.Settings)
            {
                validation.Errors.Add("AddressableAssetSettings is missing.");
            }

            var groups = GetBuildGroups();
            if (groups.Count == 0)
            {
                validation.Errors.Add("No Addressable groups are selected.");
            }

            foreach (var group in groups)
            {
                if (!group.HasSchema<BundledAssetGroupSchema>())
                {
                    validation.Errors.Add($"Group '{group.Name}' does not have a BundledAssetGroupSchema.");
                    continue;
                }

                if (group.entries.Count == 0)
                {
                    validation.Warnings.Add($"Group '{group.Name}' has no direct entries.");
                }
            }

            return validation;
        }

        public string GetOutputRootFullPath()
        {
            if (string.IsNullOrWhiteSpace(outputRoot))
            {
                return Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "Export");
            }

            return Path.IsPathRooted(outputRoot)
                ? outputRoot
                : Path.Combine(Path.GetDirectoryName(Application.dataPath)!, outputRoot);
        }

        private static List<AddressableAssetGroup> NormalizeGroups(IEnumerable<AddressableAssetGroup> groups)
        {
            var result = new List<AddressableAssetGroup>();
            var guids = new HashSet<string>();
            foreach (var group in groups)
            {
                if (!group || string.IsNullOrEmpty(group.Guid) || !guids.Add(group.Guid))
                {
                    continue;
                }

                result.Add(group);
            }

            return result;
        }

        internal static string CreateSafeFileName(string value, string fallback)
        {
            var name = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalid, '_');
            }

            return string.IsNullOrWhiteSpace(name) ? fallback : name;
        }
    }

    public sealed class RemoteContentProfileValidation
    {
        public readonly List<string> Errors = new();

        public readonly List<string> Warnings = new();

        public bool IsValid => Errors.Count == 0;
    }
}
