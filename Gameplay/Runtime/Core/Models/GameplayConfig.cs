using System;
using Chris.Configs;

namespace Chris.Gameplay
{
    /// <summary>
    /// Gameplay config
    /// </summary>
    [Serializable]
    [ConfigPath("Chris.Gameplay")]
    public class GameplayConfig: Config<GameplayConfig>
    {
        /// <summary>
        /// Whether to enable per-actor remote update.
        /// </summary>
        public bool enableRemoteUpdate;
        
        /// <summary>
        /// Whether to ensure that world subsystem is initialized before getting the system instance.
        /// </summary>
        public bool subsystemForceInitializeBeforeGet = true;
    }
}