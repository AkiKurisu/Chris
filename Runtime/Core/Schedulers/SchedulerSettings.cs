using System;
using Chris.Configs;
using UnityEngine;

namespace Chris.Schedulers
{
    [Serializable]
    [ConfigPath("Chris.Schedulers")]
    public class SchedulerSettings : Config<SchedulerSettings>
    {
        [SerializeField]
        [ConsoleVariable("r.scheduler.stackTrace")]
        internal bool schedulerStackTrace = true;

        public static bool SchedulerStackTrace => Get().schedulerStackTrace;
    }
}