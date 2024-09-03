using UnityEngine;
using UnityEngine.UI;
namespace Kurisu.Framework.UI
{
    public static class UIExtensions
    {
        /// <summary>
        /// Add a field to the panel directly
        /// </summary>
        /// <param name="field"></param>
        /// <param name="panel"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T AddToPanel<T>(this T field, UIPanel panel) where T : BaseField
        {
            return panel.Add(field);
        }
        /// <summary>
        /// Add a field to a temporary panel slot item
        /// </summary>
        /// <param name="field"></param>
        /// <param name="panelItem"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T AddToPanelItem<T>(this T field, ref PanelItem panelItem) where T : BaseField
        {
            panelItem.Fields ??= new();
            panelItem.Fields.Add(field);
            return field;
        }
        public static void SetAutosize(this Text text)
        {
            text.resizeTextMaxSize = text.fontSize;
            text.resizeTextForBestFit = true;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
        }
    }
}