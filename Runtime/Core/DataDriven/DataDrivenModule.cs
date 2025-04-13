using Chris.Modules;

namespace Chris.DataDriven
{
    public class DataDrivenModule: RuntimeModule
    {
        public override void Initialize(ModuleConfig config)
        {
#if AF_INITIALIZE_DATATABLE_MANAGER_ON_LOAD
            DataTableManager.Initialize();
#endif
        }
    }
}