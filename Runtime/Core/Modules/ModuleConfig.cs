using System.Collections.Generic;
using Chris.Serialization;

namespace Chris.Modules
{
    /// <summary>
    /// Config for <see cref="RuntimeModule"/>
    /// </summary>
    [PreferJsonConvert]
    public class ModuleConfig
    {
        private static ModuleConfig _config;

        /// <summary>
        /// Contains module additional data that can be changed during runtime
        /// </summary>
        public Dictionary<string, string> MetaData = new();
        
        public static ModuleConfig Get()
        {
            return _config ??= SaveUtility.LoadOrNew<ModuleConfig>();
        }

        public void Save()
        {
            SaveUtility.Save(this);
        }
    }

}