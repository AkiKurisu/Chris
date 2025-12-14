using Chris.Modules;
using UnityEngine.Scripting;

namespace Chris.DataDriven
{
    [Preserve]
    internal class DataDrivenModule: RuntimeModule
    {
        public override void Initialize(ModuleConfig config)
        {
            if (DataDrivenConfig.InitializeDataTableManagerOnLoad)
            {
                DataTableManager.Initialize();
            }
        }
    }
}