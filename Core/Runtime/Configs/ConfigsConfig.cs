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

        [SerializeField]
        internal string password;
    }
}