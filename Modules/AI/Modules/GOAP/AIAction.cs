using Kurisu.GOAP;
namespace Kurisu.Framework.AI.GOAP
{
    public abstract class AIAction : GOAPAction
    {
        protected GOAPAIController Controller { get; private set; }
        public void Setup(GOAPAIController controller)
        {
            Controller = controller;
            OnSetup();
        }
        protected virtual void OnSetup() { }
    }
}