using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.Pool;
using UObject = UnityEngine.Object;
using Chris.Events;
using Chris.Resource;
using R3;
using R3.Chris;
namespace Chris.UI
{
    public static class UIEntry
    {
        private static Transform _root;
        public static Transform VisualRoot
        {
            get
            {
                if (!_root)
                {
                    _root = new GameObject(nameof(UIEntry)).transform;
                    Disposable.Create(static () => _root = null).AddTo(_root);
                }
                return _root;
            }
        }
    }
    
    public interface IUIFactory
    {
        GameObject Instantiate(Transform parent);
        ref UIStyle GetUIStyle();
    }
    
    public struct UIStyle
    {
        private static readonly Color DefaultTextColor = new(0.922f, 0.886f, 0.843f);
        
        public Color TextColor;
        
        public static UIStyle DefaultStyle = new()
        {
            TextColor = DefaultTextColor
        };
    }
    
    /// <summary>
    /// UI factory loading prefab by type name
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class UIFactory<T> : IUIFactory
    {
        public static GameObject Prefab
        {
            get
            {
                if (_prefab == null)
                {
                    LoadPrefab();
                }
                return _prefab;
            }
        }
        private static GameObject _prefab;
        
        private static string _address;
        
        private static ResourceHandle<GameObject> _resourceHandle;
        
        private UIStyle _uiStyle;
        
        static UIFactory()
        {
            SetAddress(typeof(T).Name);
        }
        
        public UIFactory(UIStyle style)
        {
            _uiStyle = style;
        }
        
        public UIFactory()
        {
            _uiStyle = UIStyle.DefaultStyle;
        }
        
        public ref UIStyle GetUIStyle()
        {
            return ref _uiStyle;
        }
        
        protected static void SetAddress(string inAddress)
        {
            _address = inAddress;
        }
        
        private static void LoadPrefab()
        {
            _resourceHandle = ResourceSystem.InstantiateAsync(_address, UIEntry.VisualRoot);
            _prefab = _resourceHandle.WaitForCompletion();
            Disposable.Create(ReleasePrefab).AddTo(_prefab);
        }
        
        private static void ReleasePrefab()
        {
            _prefab = null;
            _resourceHandle.Dispose();
        }
        
        public GameObject Instantiate(Transform parent)
        {
            return UObject.Instantiate(Prefab, parent);
        }
    }
    
    /// <summary>
    /// UI base element
    /// </summary>
    public abstract class BaseField : CallbackEventHandler, IDisposable, IDisposableUnregister
    {
        public abstract class UIFactory<T> : UI.UIFactory<T> where T : BaseField
        {
            public UIFactory()
            {

            }
            public UIFactory(UIStyle style) : base(style)
            {

            }
        }
        
        public BaseField(IUIFactory factory)
        {
            _factory = factory;
            Visible.Subscribe(b =>
            {
                foreach (GameObject viewItem in ViewItems)
                {
                    viewItem.SetActive(b);
                }
            });
        }
        
        public readonly List<GameObject> ViewItems = new(1);
        
        public ReactiveProperty<bool> Visible { get; } = new(true);
        
        private readonly IUIFactory _factory;
        
        private List<IDisposable> _disposables;
        
        internal virtual void CreateView(Transform parent, BaseField parentField)
        {
            GameObject view = OnCreateView(parent);
            if (parentField != null)
            {
                Parent = parentField;
            }
            view.SetActive(Visible.Value);
            ViewItems.Add(view);
        }
        
        protected abstract GameObject OnCreateView(Transform parent);
        
        public virtual void DestroyView()
        {
            foreach (var view in ViewItems)
            {
                if (view)
                {
                    UObject.Destroy(view);
                }
            }
        }
        
        public virtual void Dispose()
        {
            Visible.Dispose();
            if (_disposables != null)
            {
                foreach (var disposable in _disposables)
                {
                    disposable.Dispose();
                }
                ListPool<IDisposable>.Release(_disposables);
                _disposables = null;
            }
        }
        
        public TField Cast<TField>() where TField : BaseField
        {
            return this as TField;
        }
        
        public GameObject Instantiate(Transform parent)
        {
            return _factory.Instantiate(parent);
        }
        
        public ref UIStyle GetUIStyle()
        {
            return ref _factory.GetUIStyle();
        }
        
        #region Events Implementation
        public override void SendEvent(EventBase e, DispatchMode dispatchMode = DispatchMode.Default)
        {
            e.Target = this;
            EventSystem.Instance.Dispatch(e, dispatchMode, MonoDispatchType.Update);
        }

        void IDisposableUnregister.Register(IDisposable disposable)
        {
            _disposables ??= ListPool<IDisposable>.Get();
            _disposables.Add(disposable);
        }

        public override IEventCoordinator Coordinator => EventSystem.Instance;
        #endregion
    }
    /// <summary>
    /// UI generic base element
    /// </summary>
    public abstract class BaseField<TValue> : BaseField, INotifyValueChanged<TValue>
    {
        protected BaseField(TValue initialValue, IUIFactory factory) : base(factory)
        {
            _value = new ReactiveProperty<TValue>(initialValue);
        }

        public TValue Value
        {
            get => _value.Value;
            set => SetValue(value);
        }

        // ReSharper disable once InconsistentNaming
        protected Observable<TValue> OnNotifyViewChanged => _value;

        public void SetValue(TValue newValue)
        {
            TValue lastValue = _value.Value;
            if (Equals(newValue, lastValue))
            {
                return;
            }
            _value.OnNext(newValue);
            if (_canFireEvents)
            {
                using var evt = ChangeEvent<TValue>.GetPooled(lastValue, newValue);
                SendEvent(evt);
            }
        }

        public void SetValueWithoutNotify(TValue newValue)
        {
            if (Equals(newValue, _value.Value))
            {
                return;
            }
            _value.OnNext(newValue);
        }

        internal override void CreateView(Transform parent, BaseField parentField)
        {
            bool wasCreated = ViewItems.Any(x => x != null);
            _canFireEvents = false;
            base.CreateView(parent, parentField);
            _canFireEvents = true;
            if (wasCreated) return;
            _value.OnNext(_value.Value);
            using var evt = ChangeEvent<TValue>.GetPooled(default, _value.Value);
            SendEvent(evt);
        }
        public BaseField<TValue> BindProperty<T>(ReactiveProperty<TValue> property, T unRegister) where T : IDisposableUnregister
        {
            this.AsObservable<ChangeEvent<TValue>>().SubscribeSafe(e => property.Value = e.NewValue).AddTo(unRegister);
            property.Subscribe(e => _value.OnNext(e)).AddTo(unRegister);
            _value.OnNext(property.Value);
            return this;
        }
        
        public BaseField<TValue> Bind<T>(Func<TValue> getter, Action<TValue> setter, T unRegister) where T : IDisposableUnregister
        {
            this.AsObservable<ChangeEvent<TValue>>().SubscribeSafe(e => setter(e.NewValue)).AddTo(unRegister);
            if(getter!=null)
                _value.OnNext(getter());
            return this;
        }
        
        public override void Dispose()
        {
            _value.Dispose();
            base.Dispose();
        }

        // ReSharper disable once InconsistentNaming
        protected readonly ReactiveProperty<TValue> _value;
        
        private bool _canFireEvents;
    }
}
