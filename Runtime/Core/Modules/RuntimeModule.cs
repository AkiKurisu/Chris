namespace Chris.Modules
{
    /// <summary>
    /// RuntimeModule is a base class for implementing logic before scene loaded in order to initialize earlier.
    /// </summary>
    public abstract class RuntimeModule
    {
        /// <summary>
        /// Module initialization entry
        /// </summary>
        /// <param name="config"></param>
        public abstract void Initialize(ModuleConfig config);
    }
}