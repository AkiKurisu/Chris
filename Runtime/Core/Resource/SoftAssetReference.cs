using System;
using UnityEngine;
using UObject = UnityEngine.Object;

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

        public AssetReferenceConstraintAttribute(Type assetType = null, string formatter = null, string group = null, bool forceGroup = false)
        {
            AssetType = assetType;
            Formatter = formatter;
            Group = group;
            ForceGroup = forceGroup;
        }
    }

    [Serializable]
    public class SoftAssetReferenceBase
    {
        public string Address;

#if UNITY_EDITOR
        [SerializeField]
                internal string Guid;

        [SerializeField]
                internal bool Locked = true;
#endif

        public override string ToString()
        {
            return Address;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Address);
        }
    }

    /// <summary>
    /// A lightweight asset reference only use address as identifier
    /// </summary>
    [Serializable]
    public class SoftAssetReference<T> : SoftAssetReferenceBase where T : UObject
    {
        /// <summary>
        /// Create asset reference from address
        /// </summary>
        /// <param name="address"></param>
        public SoftAssetReference(string address)
        {
            Address = address;
#if UNITY_EDITOR
            Guid = string.Empty;
            Locked = false;
#endif
        }

        public SoftAssetReference()
        {

        }

        /// <summary>
        /// Cache asset handle for preventing duplicated request
        /// </summary>
        private ResourceHandle<T> _resourceHandle;

        /// <summary>
        /// Load <see cref="T"/> async
        /// </summary>
        /// <returns>Handle of asset</returns>
        public ResourceHandle<T> LoadAsync()
        {
            if (_resourceHandle.IsValid()) return _resourceHandle;
            return _resourceHandle = ResourceSystem.LoadAssetAsync<T>(Address);
        }

        public static implicit operator SoftAssetReference<T>(string address)
        {
            return new SoftAssetReference<T>
            {
                Address = address
            };
        }

        public static implicit operator SoftAssetReference<T>(SoftAssetReference assetReference)
        {
            return new SoftAssetReference<T>
            {
                Address = assetReference.Address,
#if UNITY_EDITOR
                Guid = assetReference.Guid,
                Locked = assetReference.Locked
#endif
            };
        }

        public static implicit operator SoftAssetReference(SoftAssetReference<T> assetReference)
        {
            return new SoftAssetReference
            {
                Address = assetReference.Address,
#if UNITY_EDITOR
                Guid = assetReference.Guid,
                Locked = assetReference.Locked
#endif
            };
        }
    }

    /// <summary>
    /// A lightweight asset reference only use address as identifier
    /// </summary>
    [Serializable]
    public class SoftAssetReference : SoftAssetReferenceBase
    {
        /// <summary>
        /// Create asset reference from address
        /// </summary>
        /// <param name="address"></param>
        public SoftAssetReference(string address)
        {
            Address = address;
#if UNITY_EDITOR
            Guid = string.Empty;
            Locked = false;
#endif
        }

        public SoftAssetReference()
        {

        }

        /// <summary>
        /// Cache asset handle for preventing duplicated request
        /// </summary>
        private ResourceHandle _resourceHandle;

        public ResourceHandle LoadAsync()
        {
            if (_resourceHandle.IsValid()) return _resourceHandle;
            return _resourceHandle = ResourceSystem.LoadAssetAsync<UObject>(Address);
        }

        public static implicit operator SoftAssetReference(string address)
        {
            return new SoftAssetReference
            {
                Address = address
            };
        }
    }
}
