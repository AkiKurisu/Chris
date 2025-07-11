using System;
using Chris.Configs;
using UnityEngine;

namespace Chris.DataDriven
{
    [Serializable]
    [ConfigPath("Chris.DataDriven")]
    public class DataDrivenSettings : Config<DataDrivenSettings>
    {
        [SerializeField]
        internal bool initializeDataTableManagerOnLoad;

        public static bool InitializeDataTableManagerOnLoad => Get().initializeDataTableManagerOnLoad;
    }
}