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
    
    public readonly struct ObservableDestroyTriggerUnregister : IDisposableUnregister
    {
        private readonly ObservableDestroyTrigger _trigger;
        
        public ObservableDestroyTriggerUnregister(ObservableDestroyTrigger trigger)
        {
            _trigger = trigger;
        }
        
        public void Register(IDisposable disposable)
        {
            _trigger.AddDisposableOnDestroy(disposable);
        }
    }
    
    public readonly struct CancellationTokenUnregister : IDisposableUnregister
    {
        private readonly CancellationToken _cancellationToken;
        
        public CancellationTokenUnregister(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }
        
        public void Register(IDisposable disposable)
        {
            disposable.RegisterTo(_cancellationToken);
        }
    }
    
    public static class DisposableExtensions
    {
        /// <summary>
        /// Get or create an UnRegister from <see cref="GameObject"/>, listening OnDestroy event
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static ObservableDestroyTriggerUnregister GetUnregister(this GameObject gameObject)
        {
            return new ObservableDestroyTriggerUnregister(gameObject.GetOrAddComponent<ObservableDestroyTrigger>());
        }
        
        /// <summary>
        ///  Get or create an UnRegister from <see cref="MonoBehaviour"/>, listening OnDestroy event
        /// </summary>
        /// <param name="monoBehaviour"></param>
        /// <returns></returns>
        public static CancellationTokenUnregister GetUnregister(this MonoBehaviour monoBehaviour)
        {
            return new CancellationTokenUnregister(monoBehaviour.destroyCancellationToken);
        }
        
        public static T AddTo<T, TK>(this T disposable, TK unRegister) where T : IDisposable where TK : IDisposableUnregister
        {
            unRegister.Register(disposable);
            return disposable;
        }
    }
}
