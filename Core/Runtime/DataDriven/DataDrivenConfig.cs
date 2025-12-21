using System;
using Chris.Configs;
using UnityEngine;

namespace Chris.DataDriven
{
    [Serializable]
    [ConfigPath("Chris.DataDriven")]
    public class DataDrivenConfig : Config<DataDrivenConfig>
    {
        [SerializeField]
        internal bool initializeDataTableManagerOnLoad;
        
        [SerializeField]
        internal bool validateDataTableBeforeLoad = true;

        public static bool InitializeDataTableManagerOnLoad => Get().initializeDataTableManagerOnLoad;
        
        public static bool ValidateDataTableBeforeLoad => Get().validateDataTableBeforeLoad;
    }
}