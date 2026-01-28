using Asaki.Core.MVVM;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(AsakiProperty<>))]
    public class AsakiPropertyDrawer : PropertyDrawer
    {
        // 对应 Core 层中变量的名字
        private const string FieldName = "_value";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 查找内部的 _value 字段
            SerializedProperty valueProp = property.FindPropertyRelative(FieldName);

            if (valueProp != null)
            {
                // 使用 includeChildren=true 以支持 Vector3/Struct 等复杂类型
                EditorGUI.PropertyField(position, valueProp, label, true);
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Error: serialization mismatch");
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProp = property.FindPropertyRelative(FieldName);
            return valueProp != null
                ? EditorGUI.GetPropertyHeight(valueProp, label, true)
                : EditorGUIUtility.singleLineHeight;
        }
    }
}
