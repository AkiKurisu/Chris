using System;
using Chris.Configs;

namespace Chris.RuntimeConsole
{
    [Serializable]
    [ConfigPath("Chris.RuntimeConsole")]
    public class RuntimeConsoleConfig: Config<RuntimeConsoleConfig>
    {
        public bool enableConsoleInReleaseBuild;
    }
}