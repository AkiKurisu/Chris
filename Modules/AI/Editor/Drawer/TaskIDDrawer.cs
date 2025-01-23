using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace Chris.AI
{
    [CustomPropertyDrawer(typeof(TaskIDAttribute), true)]
    public class TaskIDDrawer : PropertyDrawer
    {
        private class TaskIDRegistry
        {
            public static readonly string[] Values;
            static TaskIDRegistry()
            {
                var hosts = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
                .Where(x => x.GetCustomAttribute<TaskIDHostAttribute>() != null);
                var list = new List<string>();
                foreach (var host in hosts)
                {
                    foreach (var fieldInfo in host.GetFields().Where(x => x.FieldType == typeof(string) && x.IsLiteral))
                    {
                        list.Add(fieldInfo.GetValue(null) as string);
                    }
                }
                Values = list.ToArray();
            }
        }
        private static readonly GUIContent k_IsNotStringLabel = new("The property type is not string.");
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            if (property.propertyType == SerializedPropertyType.String)
            {
                Rect popupPosition = new(position)
                {
                    height = EditorGUIUtility.singleLineHeight
                };
                int index = EditorGUI.Popup(position: popupPosition, label.text, selectedIndex: Array.IndexOf(TaskIDRegistry.Values, property.stringValue), displayedOptions: TaskIDRegistry.Values);
                if (index >= 0)
                {
                    property.stringValue = TaskIDRegistry.Values[index];
                }
            }
            else
            {
                EditorGUI.LabelField(position, label, k_IsNotStringLabel);
            }
            EditorGUI.EndProperty();
        }
    }
}
