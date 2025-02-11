using System;
using System.Threading;
using Chris;
using Chris.Events;
using UnityEngine.UI;
namespace R3.Chris
{
    public static class ObservableExtensions
    {
        #region CallbackEventHandler
        
        /// <summary>
        /// Create Observable for <see cref="CallbackEventHandler"/>
        /// </summary>
        /// <param name="handler"></param>
        /// <typeparam name="TEventType"></typeparam>
        /// <returns></returns>
        public static Observable<TEventType> AsObservable<TEventType>(this CallbackEventHandler handler)
        where TEventType : EventBase<TEventType>, new()
        {
            return handler.AsObservable<TEventType>(TrickleDown.NoTrickleDown);
        }
        
        /// <summary>
        /// Create Observable for <see cref="CallbackEventHandler"/>
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="trickleDown"></param>
        /// <typeparam name="TEventType"></typeparam>
        /// <returns></returns>
        public static Observable<TEventType> AsObservable<TEventType>(this CallbackEventHandler handler, TrickleDown trickleDown)
        where TEventType : EventBase<TEventType>, new()
        {
            CancellationToken cancellationToken = default;
            if (handler is IBehaviourScope behaviourScope && behaviourScope.Behaviour)
                cancellationToken = behaviourScope.Behaviour.destroyCancellationToken;
            return new FromEventHandler<TEventType>(static action => new EventCallback<TEventType>(action),
            callback => handler.RegisterCallback(callback, trickleDown), callback => handler.UnregisterCallback(callback, trickleDown), cancellationToken);
        }
        
        #endregion
        /// <summary>
        /// Subscribe <see cref="Observable{TEventType}"/> and finally dispose event, better performance for <see cref="EventBase"/>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="onNext"></param>
        /// <typeparam name="TEventType"></typeparam>
        /// <returns></returns>
        [StackTraceFrame]
        public static IDisposable SubscribeSafe<TEventType>(this Observable<TEventType> source, EventCallback<TEventType> onNext) where TEventType : EventBase<TEventType>, new()
        {
            var action = new Action<TEventType>(OnNext);
            return source.Subscribe(action);

            void OnNext(TEventType evt)
            {
                onNext(evt);
                evt.Dispose();
            }
        }
        
        /// <summary>
        /// Bind <see cref="ReactiveProperty{Single}"/> to slider
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="slider"></param>
        /// <param name="property"></param>
        /// <param name="unRegister"></param>
        public static void BindProperty<T>(this Slider slider, ReactiveProperty<float> property, T unRegister) where T : IDisposableUnregister
        {
            slider.onValueChanged.AsObservable().Subscribe(e => property.Value = e).AddTo(unRegister);
            property.Subscribe(slider.SetValueWithoutNotify).AddTo(unRegister);
            slider.SetValueWithoutNotify(property.Value);
        }
        
        /// <summary>
        /// Bind <see cref="ReactiveProperty{Int32}"/> to slider
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="slider"></param>
        /// <param name="property"></param>
        /// <param name="unRegister"></param>
        public static void BindProperty<T>(this Slider slider, ReactiveProperty<int> property, T unRegister) where T : IDisposableUnregister
        {
            slider.onValueChanged.AsObservable().Subscribe(e => property.Value = (int)e).AddTo(unRegister);
            property.Subscribe(e => slider.SetValueWithoutNotify(e)).AddTo(unRegister);
            slider.SetValueWithoutNotify(property.Value);
        }
        
        /// <summary>
        /// Bind <see cref="ReactiveProperty{Boolean}"/> to toggle
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="toggle"></param>
        /// <param name="property"></param>
        /// <param name="unRegister"></param>
        public static void BindProperty<T>(this Toggle toggle, ReactiveProperty<bool> property, T unRegister) where T : IDisposableUnregister
        {
            toggle.onValueChanged.AsObservable().Subscribe(e => property.Value = e).AddTo(unRegister);
            property.Subscribe(toggle.SetIsOnWithoutNotify).AddTo(unRegister);
            toggle.SetIsOnWithoutNotify(property.Value);
        }
    }
}
