using Asaki.Core.Context;
using Asaki.Core.FrameworkSettings;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.FrameworkSettings
{
    [CustomEditor(typeof(GlobalServiceRegistry))]
    [CanEditMultipleObjects]
    public class GlobalServiceRegistryEditor : UnityEditor.Editor
    {
        private SerializedProperty _serviceEntriesProp;
        private SerializedProperty _validateOnStartProp;
        private SerializedProperty _versionProp;
        private SerializedProperty _lastModifiedProp;

        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _validStyle;
        private GUIStyle _invalidStyle;

        private void OnEnable()
        {
            _serviceEntriesProp = serializedObject.FindProperty("_serviceEntries");
            _validateOnStartProp = serializedObject.FindProperty("_validateOnStart");
            _versionProp = serializedObject.FindProperty("_version");
            _lastModifiedProp = serializedObject.FindProperty("_lastModified");
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            serializedObject.Update();

            EditorGUILayout.Space(5);

            DrawHeader();

            EditorGUILayout.Space(10);

            EditorGUILayout.PropertyField(_validateOnStartProp);

            EditorGUILayout.Space(10);

            DrawServiceEntriesList();

            EditorGUILayout.Space(10);

            DrawStatistics();

            EditorGUILayout.Space(10);

            DrawVersionInfo();

            serializedObject.ApplyModifiedProperties();
        }

        private void InitStyles()
        {
            if (_headerStyle != null)
                return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 5, 5),
            };

            _boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
            };

            _validStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.2f, 0.8f, 0.2f) },
            };

            _invalidStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.9f, 0.3f, 0.3f) },
            };
        }

        private new void DrawHeader()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("🌐 Global Service Registry", _headerStyle);
            GUILayout.Label("管理全局服务预制体的注册配置", EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawServiceEntriesList()
        {
            EditorGUILayout.LabelField("服务列表", EditorStyles.boldLabel);

            int validCount = 0;
            int invalidCount = 0;

            for (int i = 0; i < _serviceEntriesProp.arraySize; i++)
            {
                var entryProp = _serviceEntriesProp.GetArrayElementAtIndex(i);
                DrawServiceEntry(entryProp, i, ref validCount, ref invalidCount);
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("+ 添加服务", GUILayout.Height(28)))
            {
                _serviceEntriesProp.arraySize++;
                var newEntry = _serviceEntriesProp.GetArrayElementAtIndex(
                    _serviceEntriesProp.arraySize - 1
                );
                var priorityProp = newEntry.FindPropertyRelative("Priority");
                var enabledProp = newEntry.FindPropertyRelative("Enabled");
                priorityProp.intValue = 50;
                enabledProp.boolValue = true;
            }

            if (GUILayout.Button("按优先级排序", GUILayout.Height(28)))
            {
                var registry = target as GlobalServiceRegistry;
                if (registry != null)
                {
                    registry.SortByPriority();
                    EditorUtility.SetDirty(registry);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawServiceEntry(
            SerializedProperty entryProp,
            int index,
            ref int validCount,
            ref int invalidCount
        )
        {
            var prefabProp = entryProp.FindPropertyRelative("Prefab");
            var enabledProp = entryProp.FindPropertyRelative("Enabled");
            var descriptionProp = entryProp.FindPropertyRelative("Description");
            var priorityProp = entryProp.FindPropertyRelative("Priority");

            GameObject prefab = prefabProp.objectReferenceValue as GameObject;
            bool isValid = prefab != null && HasGlobalServiceComponent(prefab);

            if (isValid)
                validCount++;
            else if (prefab != null)
                invalidCount++;

            Color bgColor = GUI.backgroundColor;
            if (!enabledProp.boolValue)
            {
                GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            }
            else if (prefab != null && !isValid)
            {
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f, 0.3f);
            }

            EditorGUILayout.BeginVertical(_boxStyle);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(30));
            GUILayout.Label($"#{index + 1}", EditorStyles.boldLabel, GUILayout.Width(25));
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(prefabProp, GUIContent.none, GUILayout.MinWidth(150));

            GUIContent statusIcon = isValid
                ? new GUIContent("✅")
                : (prefab != null ? new GUIContent("❌") : new GUIContent("⚪"));
            GUILayout.Label(statusIcon, GUILayout.Width(20));

            enabledProp.boolValue = EditorGUILayout.Toggle(
                enabledProp.boolValue,
                GUILayout.Width(20)
            );
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("优先级:", GUILayout.Width(50));
            EditorGUILayout.PropertyField(priorityProp, GUIContent.none, GUILayout.Width(60));
            GUILayout.Label("描述:", GUILayout.Width(40));
            EditorGUILayout.PropertyField(
                descriptionProp,
                GUIContent.none,
                GUILayout.MinWidth(100)
            );
            EditorGUILayout.EndHorizontal();

            if (prefab != null && !isValid)
            {
                GUILayout.Label("⚠️ 预制体未包含 IAsakiGlobalService 组件", _invalidStyle);
            }

            EditorGUILayout.EndVertical();

            if (GUILayout.Button("↑", GUILayout.Width(25), GUILayout.Height(22)) && index > 0)
            {
                _serviceEntriesProp.MoveArrayElement(index, index - 1);
            }

            if (
                GUILayout.Button("↓", GUILayout.Width(25), GUILayout.Height(22))
                && index < _serviceEntriesProp.arraySize - 1
            )
            {
                _serviceEntriesProp.MoveArrayElement(index, index + 1);
            }

            if (GUILayout.Button("×", GUILayout.Width(25), GUILayout.Height(22)))
            {
                _serviceEntriesProp.DeleteArrayElementAtIndex(index);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            GUI.backgroundColor = bgColor;
        }

        private void DrawStatistics()
        {
            int total = _serviceEntriesProp.arraySize;
            int enabled = 0;
            int valid = 0;

            for (int i = 0; i < _serviceEntriesProp.arraySize; i++)
            {
                var entryProp = _serviceEntriesProp.GetArrayElementAtIndex(i);
                var prefabProp = entryProp.FindPropertyRelative("Prefab");
                var enabledProp = entryProp.FindPropertyRelative("Enabled");

                if (enabledProp.boolValue)
                    enabled++;

                GameObject prefab = prefabProp.objectReferenceValue as GameObject;
                if (prefab != null && HasGlobalServiceComponent(prefab))
                    valid++;
            }

            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("📊 统计信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"总服务数: {total}");
            EditorGUILayout.LabelField($"已启用: {enabled}");
            EditorGUILayout.LabelField($"有效配置: {valid}");
            EditorGUILayout.EndVertical();
        }

        private void DrawVersionInfo()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("📝 版本信息", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_versionProp, new GUIContent("版本号"));
            EditorGUILayout.LabelField($"最后修改: {_lastModifiedProp.stringValue}");
            EditorGUILayout.EndVertical();
        }

        private bool HasGlobalServiceComponent(GameObject prefab)
        {
            if (prefab == null)
                return false;

            var components = prefab.GetComponentsInChildren<IAsakiGlobalService>(true);
            return components != null && components.Length > 0;
        }
    }
}
