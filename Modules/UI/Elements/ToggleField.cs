using R3;
using R3.Chris;
using UnityEngine;
using UnityEngine.UI;
namespace Chris.UI
{
    /// <summary>
    /// Field that draws a toggle
    /// </summary>
    public class ToggleField : BaseField<bool>
    {
        public class UIFactory : UIFactory<ToggleField>
        {

        }
        public ToggleField(string displayName) : this(displayName, false)
        {
        }

        public ToggleField(string displayName, bool initialValue) : base(initialValue, DefaultFactory)
        {
            DisplayName = displayName;
        }
        public ToggleField(IUIFactory factory, string displayName, bool initialValue) : base(initialValue, factory)
        {
            DisplayName = displayName;
        }

        /// <summary>
        /// Text shown next to the checkbox
        /// </summary>
        public string DisplayName { get; }
        
        private Toggle _toggle;
        
        private static readonly UIFactory DefaultFactory = new();
        protected override GameObject OnCreateView(Transform parent)
        {
            GameObject tr = Instantiate(parent);
            _toggle = tr.GetComponentInChildren<Toggle>();
            _toggle.onValueChanged.AsObservable().Subscribe(SetValue).AddTo(this);
            OnNotifyViewChanged.Subscribe(b =>
            {
                _toggle.SetIsOnWithoutNotify(b);
            });
            Text text = tr.GetComponentInChildren<Text>();
            text.text = DisplayName;
            text.color = GetUIStyle().TextColor;
            text.AutoResize();
            return tr;
        }
    }
}