using System.Collections.Generic;
using Chris.Configs;

namespace Chris.Modules
{
    /// <summary>
    /// Config for <see cref="RuntimeModule"/>
    /// </summary>
    public class ModuleConfig: Config<ModuleConfig>
    {
        private static ModuleConfig _config;

        /// <summary>
        /// Contains module additional data that can be changed during runtime
        /// </summary>
        public Dictionary<string, string> MetaData = new();
        
        public static ModuleConfig Get()
        {
            return ConfigSystem.GetConfig<ModuleConfig>();
        }
    }

}