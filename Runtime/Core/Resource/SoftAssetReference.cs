using System;
using UnityEngine;
using Object = UnityEngine.Object;
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
    
    /// <summary>
    /// A lightweight asset reference only use address as identifier
    /// </summary>
    [Serializable]
    public class SoftAssetReference<T> where T : Object
    {
        // ReSharper disable once InconsistentNaming
        public string Address;
        
#if UNITY_EDITOR
        [SerializeField]
        // ReSharper disable once InconsistentNaming
        internal string Guid;
        
        [SerializeField]
        // ReSharper disable once InconsistentNaming
        internal bool Locked = true;
#endif

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

        public static readonly SoftAssetReference Empty = new();

        public ResourceHandle<T> LoadAsync()
        {
            return ResourceSystem.LoadAssetAsync<T>(Address);
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
    public class SoftAssetReference
    {
        // ReSharper disable once InconsistentNaming
        public string Address;
        
#if UNITY_EDITOR
        [SerializeField]
        // ReSharper disable once InconsistentNaming
        internal string Guid;
        
        [SerializeField]
        // ReSharper disable once InconsistentNaming
        internal bool Locked = true;
#endif
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
        
        public static readonly SoftAssetReference Empty = new();

        public ResourceHandle LoadAsync()
        {
            return ResourceSystem.LoadAssetAsync<Object>(Address);
        }

        public static implicit operator SoftAssetReference(string address)
        {
            return new SoftAssetReference
            {
                Address = address
            };
        }

        public override string ToString()
        {
            return Address;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Address);
        }
    }
}
