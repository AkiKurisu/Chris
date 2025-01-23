using Chris.Editor;
using Chris.Tasks;
using Kurisu.AkiBT.Editor;
using UnityEditor;
using UnityEngine;
namespace Chris.AI.Editor
{
    [CustomPropertyDrawer(typeof(BehaviorTask))]
    public class BehaviorTaskDrawer : PropertyDrawer
    {
        private static Color ColorGreen = new(170 / 255f, 255 / 255f, 97 / 255f);
        private static Color ColorBlue = new(140 / 255f, 160 / 255f, 250 / 255f);
        private static Color ColorYellow = new(255 / 255f, 244 / 255f, 94 / 255f);
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect rect = new(position)
            {
                height = EditorGUIUtility.singleLineHeight
            };
            var startOnEnabled = property.FindPropertyRelative("startOnEnabled");
            EditorGUI.PropertyField(rect, startOnEnabled);
            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
            if (!startOnEnabled.boolValue)
            {
                EditorGUI.PropertyField(rect, property.FindPropertyRelative("taskID"));
                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
            }
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("behaviorTreeAsset"));
            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
            GUI.enabled = Application.isPlaying;
            var task = ReflectionEditorUtility.GetTargetObjectWithProperty(property) as BehaviorTask;
            var color = GUI.backgroundColor;
            GUI.backgroundColor = GetStatusColor(task.GetStatus());
            if (GUI.Button(rect, "Debug Task Behavior"))
            {
                GraphEditorWindow.Show(task);
            }
            GUI.backgroundColor = color;
            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
            GUI.enabled = true;
            EditorGUI.EndProperty();
        }
        private Color GetStatusColor(TaskStatus taskStatus)
        {
            if (taskStatus == TaskStatus.Running) return ColorGreen;
            if (taskStatus == TaskStatus.Completed) return ColorBlue;
            else return ColorYellow;
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var startOnEnabled = property.FindPropertyRelative("startOnEnabled").boolValue;
            return EditorGUIUtility.singleLineHeight * (startOnEnabled ? 3 : 4) + EditorGUIUtility.standardVerticalSpacing * (startOnEnabled ? 2 : 3);
        }
    }
}
