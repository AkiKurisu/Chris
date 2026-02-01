using Chris.Modules;
using UnityEngine.Scripting;

namespace Chris.DataDriven
{
    [Preserve]
    internal class DataDrivenModule: RuntimeModule
    {
        public override void Initialize()
        {
            if (DataDrivenConfig.InitializeDataTableManagerOnLoad)
            {
                DataTableManager.Initialize();
            }
        }
    }
}