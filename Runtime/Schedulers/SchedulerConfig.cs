using System;
using Chris.Configs;
using UnityEngine;

namespace Chris.Schedulers
{
    [Serializable]
    [ConfigPath("Chris.Schedulers")]
    public class SchedulerConfig : Config<SchedulerConfig>
    {
        [SerializeField]
        [ConsoleVariable("r.scheduler.stackTrace")]
        internal bool enableStackTrace = true;

        public static bool EnableStackTrace => Get().enableStackTrace;
    }
}