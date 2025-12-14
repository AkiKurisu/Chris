using System;
using UnityEngine;

namespace Chris.Resource
{
    [AttributeUsage(AttributeTargets.Field)]
    public class AssetReferenceConstraintAttribute : PropertyAttribute
    {
        /// <summary>
        /// Asset type to select
        /// </summary>
        public Type AssetType { get; private set; }

        /// <summary>
        /// Formatter method to get customized address
        /// </summary>
        public string Formatter { get; private set; }

        /// <summary>
        /// Group to register referenced asset, default use AddressableAssetSettingsDefaultObject.Settings.DefaultGroup
        /// </summary>
        public string Group { get; private set; }

        /// <summary>
        /// Enable to move asset entry to defined group if already in other asset group
        /// </summary>
        /// <value></value>
        public bool ForceGroup { get; private set; }

        public AssetReferenceConstraintAttribute(Type assetType = null, string formatter = null, 
            string group = null, bool forceGroup = false)
        {
            AssetType = assetType;
            Formatter = formatter;
            Group = group;
            ForceGroup = forceGroup;
        }
    }
}