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
        /// Create <see cref="Observable{TEventType}"/> for <see cref="CallbackEventHandler"/>
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
        /// <param name="slider"></param>
        /// <param name="property"></param>
        /// <param name="unRegister"></param>
        public static void BindProperty(this Slider slider, ReactiveProperty<float> property, IDisposableUnregister unRegister)
        {
            slider.onValueChanged.AsObservable().Subscribe(e => property.Value = e).AddTo(unRegister);
            property.Subscribe(slider.SetValueWithoutNotify).AddTo(unRegister);
            slider.SetValueWithoutNotify(property.Value);
        }
        
        /// <summary>
        /// Bind <see cref="ReactiveProperty{Int32}"/> to slider
        /// </summary>
        /// <param name="slider"></param>
        /// <param name="property"></param>
        /// <param name="unRegister"></param>
        public static void BindProperty(this Slider slider, ReactiveProperty<int> property, IDisposableUnregister unRegister)
        {
            slider.onValueChanged.AsObservable().Subscribe(e => property.Value = (int)e).AddTo(unRegister);
            property.Subscribe(e => slider.SetValueWithoutNotify(e)).AddTo(unRegister);
            slider.SetValueWithoutNotify(property.Value);
        }
        
        /// <summary>
        /// Bind <see cref="ReactiveProperty{Boolean}"/> to toggle
        /// </summary>
        /// <param name="toggle"></param>
        /// <param name="property"></param>
        /// <param name="unRegister"></param>
        public static void BindProperty(this Toggle toggle, ReactiveProperty<bool> property, IDisposableUnregister unRegister)
        {
            toggle.onValueChanged.AsObservable().Subscribe(e => property.Value = e).AddTo(unRegister);
            property.Subscribe(toggle.SetIsOnWithoutNotify).AddTo(unRegister);
            toggle.SetIsOnWithoutNotify(property.Value);
        }
        
        /// <summary>
        /// Bind <see cref="ReactiveProperty{Single}"/> to slider
        /// </summary>
        /// <param name="slider"></param>
        /// <param name="property"></param>
        /// <param name="unRegister"></param>
        public static void BindProperty(this Slider slider, ReactiveProperty<float> property, DisposableUnregister unRegister) 
        {
            slider.onValueChanged.AsObservable().Subscribe(e => property.Value = e).AddTo(unRegister);
            property.Subscribe(slider.SetValueWithoutNotify).AddTo(unRegister);
            slider.SetValueWithoutNotify(property.Value);
        }
        
        /// <summary>
        /// Bind <see cref="ReactiveProperty{Int32}"/> to slider
        /// </summary>
        /// <param name="slider"></param>
        /// <param name="property"></param>
        /// <param name="unRegister"></param>
        public static void BindProperty(this Slider slider, ReactiveProperty<int> property, DisposableUnregister unRegister)
        {
            slider.onValueChanged.AsObservable().Subscribe(e => property.Value = (int)e).AddTo(unRegister);
            property.Subscribe(e => slider.SetValueWithoutNotify(e)).AddTo(unRegister);
            slider.SetValueWithoutNotify(property.Value);
        }
        
        /// <summary>
        /// Bind <see cref="ReactiveProperty{Boolean}"/> to toggle
        /// </summary>
        /// <param name="toggle"></param>
        /// <param name="property"></param>
        /// <param name="unRegister"></param>
        public static void BindProperty(this Toggle toggle, ReactiveProperty<bool> property, DisposableUnregister unRegister)
        {
            toggle.onValueChanged.AsObservable().Subscribe(e => property.Value = e).AddTo(unRegister);
            property.Subscribe(toggle.SetIsOnWithoutNotify).AddTo(unRegister);
            toggle.SetIsOnWithoutNotify(property.Value);
        }
    }
}
