using System.Collections.Generic;
using Chris.Serialization;
using Newtonsoft.Json;
using UnityEngine;

namespace Chris.Configs
{
    [PreferJsonConvert]
    internal class ConfigFile : Dictionary<string, string>, IConfigFile
    {
        public ConfigFileLocation Location { get; internal set; }
        
        public ConfigFile()
        {
            
        }

        public ConfigFile(ConfigFileLocation location)
        {
            Location = location;
        }
        
        public bool TryGetConfig(IConfigLocation location, out ConfigBase config)
        {
            config = null;
            if (TryGetValue(location.ConfigName, out var data))
            {
                if (location.PreferJsonConvert)
                {
                    config = JsonConvert.DeserializeObject(data, location.Type) as ConfigBase;
                }
                else
                {
                    config = JsonUtility.FromJson(data, location.Type) as ConfigBase;
                }
            }
            return config != null;
        }

        public void SetConfig(IConfigLocation location, ConfigBase config)
        {
            string jsonData;
            if (location.PreferJsonConvert)
            {
                jsonData = JsonConvert.SerializeObject(config);
            }
            else
            {
                jsonData = JsonUtility.ToJson(config);
            }
            this[location.ConfigName] = jsonData;
        }
    }
}