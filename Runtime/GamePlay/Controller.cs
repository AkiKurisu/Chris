using UnityEngine;
namespace Kurisu.Framework
{
    public class Controller : MonoBehaviour
    {
        private Actor actor;
        public virtual bool IsBot()
        {
            return false;
        }
        public void SetActor(Actor actor)
        {
            if (this.actor != null)
            {
                this.actor.UnbindController(this);
                this.actor = null;
            }
            this.actor = actor;
            actor.BindController(this);
        }
        protected virtual void OnDestroy()
        {
            if (actor) actor.UnbindController(this);
            actor = null;
        }
        public TActor GetTActor<TActor>() where TActor : Actor
        {
            return actor as TActor;
        }
        public Actor GetActor()
        {
            return actor;
        }
    }
}
