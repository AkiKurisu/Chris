using System;

namespace Chris.Gameplay.Graphics
{
    /// <summary>
    /// Implement for custom graphics module
    /// </summary>
    public abstract class GraphicsModule : IDisposable
    {
        public virtual void Initialize(GraphicsController graphics, GraphicsConfig config)
        {

        }
        
        public virtual void Dispose()
        {

        }
    }
}