using System.Linq;
using System.Reflection;
using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Unity;
using Asaki.Unity.Services.UI;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.UI
{
    [CustomEditor(typeof(AsakiUIWindow), true)]
    public class AsakiUIWindowEditor : UnityEditor.Editor
    {
        private bool _showBindings = true;

        public override void OnInspectorGUI()
        {
            // 1. 绘制默认的脚本引用框
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Asaki UI Dev Tools", EditorStyles.boldLabel);

            // 2. 核心功能：直接在 Inspector 触发 Scaffolder
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("♻️ Sync & Re-Scaffold UI", GUILayout.Height(30)))
            {
                AsakiUIScaffolder.ScaffoldFromTarget((AsakiUIWindow)target);
            }
            GUI.backgroundColor = Color.white;

            // 3. 可视化绑定状态 (Dashboard)
            _showBindings = EditorGUILayout.Foldout(_showBindings, "Bindings Status");
            if (_showBindings)
            {
                DrawBindingStatus();
            }
        }

        private void DrawBindingStatus()
        {
            MonoBehaviour targetScript = (MonoBehaviour)target;
            var fields = targetScript
                .GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => f.GetCustomAttribute<AsakiUIBuilderAttribute>() != null);

            EditorGUI.indentLevel++;
            foreach (FieldInfo field in fields)
            {
                AsakiUIBuilderAttribute attr = field.GetCustomAttribute<AsakiUIBuilderAttribute>();
                Object value = field.GetValue(targetScript) as Object;

                EditorGUILayout.BeginHorizontal();

                // 状态图标
                if (value != null)
                    GUILayout.Label("✅", GUILayout.Width(20));
                else
                    GUILayout.Label("❌", GUILayout.Width(20));

                // 字段名 + 预期类型
                EditorGUILayout.LabelField($"{field.Name} ({attr.Type})", EditorStyles.miniLabel);

                // 当前引用对象 (只读显示)
                EditorGUILayout.ObjectField(value, typeof(Object), true);

                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }
    }
}
