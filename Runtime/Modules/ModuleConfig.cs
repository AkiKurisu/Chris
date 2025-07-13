using System.Collections.Generic;
using Chris.Configs;
using Chris.Serialization;

namespace Chris.Modules
{
    /// <summary>
    /// Config for <see cref="RuntimeModule"/>
    /// </summary>
    [PreferJsonConvert]
    [ConfigPath("Chris.Modules")]
    public class ModuleConfig: Config<ModuleConfig>
    {
        /// <summary>
        /// Contains module additional data that can be changed during runtime
        /// </summary>
        public Dictionary<string, string> MetaData { get; set; } = new();
    }
}