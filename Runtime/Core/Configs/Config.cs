using System;
using Chris.Serialization;

namespace Chris.Configs
{
    /// <summary>
    /// Base class for global configs
    /// </summary>
    public abstract class Config
    {
        protected static ulong NextConfigId;
        
        internal class Location: IConfigLocation
        {
            public readonly string Name;

            public readonly Type Type;

            public readonly bool PreferJsonConvert;
        
            public Location(string name, Type type, bool preferJsonConvert)
            {
                Name = name;
                Type = type;
                PreferJsonConvert = preferJsonConvert;
            }
        }
    }
    
    /// <summary>
    /// Generic global config
    /// </summary>
    /// <typeparam name="TConfig"></typeparam>
    public abstract class Config<TConfig>: Config 
        where TConfig: Config<TConfig>, new()
    {
        private const string ConfigName = nameof(TConfig);

        internal static readonly ulong ConfigTypeId;

        internal static readonly Location ConfigLocation = new(ConfigName, typeof(TConfig), SaveLoadSerializer.TypeCache<TConfig>.PreferJsonConvert);

        static Config()
        {
            ConfigTypeId = ++NextConfigId;
        }
        
        /// <summary>
        /// Save config to persistent save path
        /// </summary>
        public void Save()
        {
            ConfigsModule.PersistentSerializer.Save(ConfigName, (TConfig)this);
        }
    }
}
