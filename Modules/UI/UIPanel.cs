using System.Collections.Generic;
using System.Linq;
using Chris.Serialization;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;
namespace Chris.UI
{
    /// <summary>
    /// Virtual panel item data structure
    /// </summary>
    public struct PanelItem
    {
        /// <summary>
        /// Overwrite to set item order
        /// </summary>
        public int Order;
        /// <summary>
        /// Panel item included fields
        /// </summary>
        public List<BaseField> Fields;
    }
    /// <summary>
    /// Interface to create virtual panel item
    /// </summary>
    public interface IPanelItem
    {
        /// <summary>
        /// Create items attached to target <see cref="UIPanel"/>
        /// </summary>
        /// <param name="panel">Parent panel</param>
        /// <param name="panelItem">Panel item slot that will be ordered</param>
        void CreatePanelItem(UIPanel panel, ref PanelItem panelItem);
    }
    public class UIPanel : MonoBehaviour
    {
        /// <summary>
        /// Initial panel items
        /// </summary>
        [SerializeField]
        private SerializedType<IPanelItem>[] initialItems;
        
        /// <summary>
        /// Add space between each <see cref="initialItems"/> 
        /// </summary>
        [FormerlySerializedAs("AddSpaceForInitialItems")]
        public bool addSpaceForInitialItems = true;
        
        /// <summary>
        /// Space amount between each <see cref="initialItems"/> 
        /// </summary>
        [FormerlySerializedAs("ItemSpace")] 
        public int itemSpace = SpaceField.DefaultSpace;
        
        [SerializeField, Tooltip("Set to override content container, default use self transform")]
        private Transform contentContainer;
        
        private PanelField _panelField;
        
        public Transform ContentContainer
        {
            get
            {
                if (contentContainer)
                {
                    return contentContainer;
                }
                return transform;
            }
        }

        public RectTransform PanelRect { get; private set; }
        

        protected virtual void Awake()
        {
            PanelRect = GetComponent<RectTransform>();
            _panelField ??= new PanelField(this);
        }
        
        protected virtual void Start()
        {
            InitializePanel().Forget();
        }
        
        protected async UniTask InitializePanel()
        {
            await UniTask.Yield();
            CreateFields();
        }
        
        private void OnDestroy()
        {
            _panelField.Dispose();
        }
        
        public PanelField GetRootPanel()
        {
            _panelField ??= new PanelField(this);
            return _panelField;
        }
        
        protected void CreateFields()
        {
            // Inject virtual panel items
            var items = new PanelItem[initialItems.Length];
            for (var i = 0; i < items.Length; ++i)
            {
                items[i].Fields = ListPool<BaseField>.Get();
                // use default array order
                items[i].Order = i;
                initialItems[i].GetObject().CreatePanelItem(this, ref items[i]);
            }
            items = items.OrderBy(x => x.Order).ToArray();

            // Layout
            bool isFirst = true;
            foreach (var item in items)
            {
                if (!isFirst && addSpaceForInitialItems)
                {
                    _panelField.Add(new SpaceField(itemSpace));
                }
                _panelField.AddRange(item.Fields);
                ListPool<BaseField>.Release(item.Fields);
                isFirst = false;
            }

            // Create view
            if (!_panelField.IsInitialized)
            {
                _panelField.CreateView(transform, null);
            }
        }
        
        public void ClearFields()
        {
            _panelField?.Clear();
        }
        
        public TPanel Cast<TPanel>() where TPanel : UIPanel
        {
            return this as TPanel;
        }
    }
}