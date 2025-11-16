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

        private static ISerializeFormatter _formatter;
        
        public static ISerializeFormatter GetConfigSerializer()
        {
            if (_formatter == null)
            {
                var config = Get();
                if (config.configSerializer.IsValid())
                {
                    _formatter = config.configSerializer.GetObject();
                }
                else
                {
                    _formatter = TextSerializeFormatter.Instance;
                }
            }

            return _formatter;
        }
    }
}