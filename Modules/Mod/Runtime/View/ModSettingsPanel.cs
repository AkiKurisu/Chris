using System.Linq;
using Chris.Events;
using Chris.UI;
using R3;
using R3.Chris;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
namespace Chris.Mod.UI
{
    public class ModSettingsPanel : UIPanel
    {
        protected override void Start()
        {
            if (!ModAPI.IsModInit.Value) return;
            InitializeModPanel().Forget();
        }
        protected async UniTask InitializeModPanel()
        {
            await UniTask.Yield();
            CreateFields();
            ModAPI.OnModRefresh.Subscribe(Refresh).AddTo(this);
        }
        private void Refresh(Unit _)
        {
            ClearFields();
            CreateFields();
        }
    }
    
    public class ModPanelItem : IPanelItem
    {
        public void CreatePanelItem(UIPanel panel, ref PanelItem panelItem)
        {
            var infos = ModAPI.GetAllInfos();
            foreach (var info in infos)
            {
                if (ModAPI.GetModState(info) == ModState.Delate) continue;
                new ModConfigField(info.modName).Bind(info).AddToPanelItem(ref panelItem);
            }
        }
    }
    
    public class ModConfigField : BaseField<int>
    {
        private static readonly IUIFactory DefaultFactory = new UIFactory();
        
        public class UIFactory : UIFactory<ModConfigField>
        {

        }
        
        public ModConfigField(string displayName, string[] optionNames = null, int initialValue = 0) : base(initialValue, DefaultFactory)
        {
            DisplayName = displayName;
            _optionNames = optionNames;
        }
        
        public string DisplayName { get; }
        
        private readonly string[] _optionNames;
        
        private Toggle[] _toggles;
        
        private Button _deleteButton;
        
        protected override GameObject OnCreateView(Transform parent)
        {
            GameObject tr = Instantiate(parent);
            _toggles = tr.GetComponentsInChildren<Toggle>();
            _deleteButton = tr.GetComponentInChildren<Button>();
            (from item in _toggles.Select((val, idx) => new { val, idx })
             where item.val != null
             select item).ToList().ForEach(item =>
            {
                (from isOn in item.val.onValueChanged.AsObservable()
                 where isOn
                 select isOn).Subscribe(_ =>
                 {
                     SetValue(item.idx);
                 }).AddTo(this);
                if (_optionNames != null)
                {
                    Text text = item.val.GetComponentInChildren<Text>();
                    text.text = _optionNames[item.idx];
                }
            });
            OnNotifyViewChanged.Subscribe(b =>
            {
                for (int i = 0; i < _toggles.Length; ++i)
                {
                    _toggles[i].SetIsOnWithoutNotify(i == b);
                }
            });
            if (_deleteButton)
            {
                _deleteButton.OnClickAsObservable().Subscribe(_ =>
                {
                    SetValue(2);
                });
            }
            Text text = tr.transform.Find("title").GetComponent<Text>();
            text.text = DisplayName;
            return tr;
        }
        
        public ModConfigField Bind(ModInfo modInfo)
        {
            this.AsObservable<ChangeEvent<int>>().Skip(1).SubscribeSafe(e =>
            {
                if (e.NewValue == 0)
                {
                    ModAPI.EnabledMod(modInfo, true);
                }
                else if (e.NewValue == 1)
                {
                    ModAPI.EnabledMod(modInfo, false);
                }
                else if (e.NewValue == 2)
                {
                    ModAPI.DeleteMod(modInfo);
                }
            }).AddTo(this);
            SetValueWithoutNotify(ModAPI.GetModState(modInfo) == ModState.Enabled ? 0 : 1);
            return this;
        }
    }
}
