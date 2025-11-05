using System;
using Chris.Serialization;
using UnityEngine;

namespace Chris.Configs
{
    [Serializable]
    [ConfigPath("Chris.Configs")]
    public class ConfigsConfig : Config<ConfigsConfig>
    {
        [SerializeField]
        internal SerializedType<ISerializeFormatter> configSerializer = SerializedType<ISerializeFormatter>.FromType(typeof(TextSerializeFormatter));

        public static ISerializeFormatter GetConfigSerializer()
        {
            var config = Get();
            if (config.configSerializer.IsValid())
            {
                return config.configSerializer.GetObject();
            }
            // Fallback to default
            return TextSerializeFormatter.Instance;
        }
    }
}