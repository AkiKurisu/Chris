using System;
using System.Threading;
using Chris;
using R3.Triggers;
using UnityEngine;
namespace R3.Chris
{
    /// <summary>
    /// Unregister scope interface for managing <see cref="IDisposable"/> 
    /// </summary>
    public interface IDisposableUnregister
    {
        /// <summary>
        /// Register new disposable to this unregister scope
        /// </summary>
        /// <param name="disposable"></param>
        void Register(IDisposable disposable);
    }
    
    public readonly struct DisposableUnregister
    {
        private readonly ObservableDestroyTrigger _trigger;
        
        private readonly CancellationToken _cancellationToken;
        
        public DisposableUnregister(ObservableDestroyTrigger trigger)
        {
            _trigger = trigger;
            _cancellationToken = default;
        }
        
        public DisposableUnregister(CancellationToken cancellationToken)
        {
            _trigger = null;
            _cancellationToken = cancellationToken;
        }
        
        public void Register(IDisposable disposable)
        {
            if (_trigger)
            {
                _trigger.AddDisposableOnDestroy(disposable);
            }
            else
            {
                disposable.RegisterTo(_cancellationToken);
            }
        }
    }
    
    public static class DisposableExtensions
    {
        /// <summary>
        /// Get or create an UnRegister from <see cref="GameObject"/>, listening OnDestroy event
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static DisposableUnregister GetUnregister(this GameObject gameObject)
        {
            return new DisposableUnregister(gameObject.GetOrAddComponent<ObservableDestroyTrigger>());
        }
        
        /// <summary>
        ///  Get or create an UnRegister from <see cref="MonoBehaviour"/>, listening OnDestroy event
        /// </summary>
        /// <param name="monoBehaviour"></param>
        /// <returns></returns>
        public static DisposableUnregister GetUnregister(this MonoBehaviour monoBehaviour)
        {
            return new DisposableUnregister(monoBehaviour.destroyCancellationToken);
        }
        
        public static T AddTo<T>(this T disposable, IDisposableUnregister unRegister) where T : IDisposable
        {
            unRegister.Register(disposable);
            return disposable;
        }
        
        public static T AddTo<T>(this T disposable, DisposableUnregister unRegister) where T : IDisposable
        {
            unRegister.Register(disposable);
            return disposable;
        }
    }
}
