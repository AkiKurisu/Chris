using System;
using R3.Chris;
using R3;
using UnityEngine;
using UnityEngine.UI;
namespace Chris.UI
{
    /// <summary>
    /// Field that draws a button
    /// </summary>
    public class ButtonField : BaseField
    {
        public class UIFactory : UIFactory<ButtonField>
        {

        }
        public ButtonField(string displayName, Action onClicked) : base(DefaultFactory)
        {
            DisplayName = displayName;
            OnClicked = onClicked;
        }
        public ButtonField(IUIFactory factory, string displayName, Action onClicked) : base(factory)
        {
            DisplayName = displayName;
            OnClicked = onClicked;
        }

        /// <summary>
        /// Text shown next to the checkbox
        /// </summary>
        public string DisplayName { get; }

        public Action OnClicked;
        
        private Button _button;
        
        private static readonly UIFactory DefaultFactory = new();
        
        protected override GameObject OnCreateView(Transform parent)
        {
            GameObject tr = Instantiate(parent);
            _button = tr.GetComponentInChildren<Button>();
            _button.OnClickAsObservable().Subscribe(OnButtonClicked).AddTo(this);
            Text text = tr.GetComponentInChildren<Text>();
            text.text = DisplayName;
            text.color = GetUIStyle().TextColor;
            text.AutoResize();
            return tr;
        }
        private void OnButtonClicked(Unit _)
        {
            OnClicked?.Invoke();
        }
    }
}