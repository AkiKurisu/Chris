using Ceres.Graph;
using Chris.Gameplay.Animations;
using Chris.Modules;
using UnityEngine.Scripting;

namespace Chris.Gameplay.Flow
{
    [Preserve]
    internal class GameplayFlowModule: RuntimeModule
    {
        public override void Initialize(ModuleConfig config)
        {
            /* Register port implicit conversation */
            // ========================= Animation =========================== //
            CeresPort<LayerHandle>.MakeCompatibleTo<int>(handle => handle.Id);
            CeresPort<int>.MakeCompatibleTo<LayerHandle>(d => new LayerHandle(d));
            CeresPort<string>.MakeCompatibleTo<LayerHandle>(str => new LayerHandle(str));
            // ========================= Animation =========================== //
        }
    }
}