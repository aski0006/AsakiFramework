// File: Assets/Asaki/Editor/Context/AsakiSceneContextEditor.cs
using Asaki. Core.Context;
using Asaki.Core.Context.Resolvers;
using System;
using System. Linq;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Context
{
    [CustomEditor(typeof(AsakiSceneContext))]
    public class AsakiSceneContextEditor : UnityEditor.Editor
    {
        private SerializedProperty _pureCSharpServicesProp;
        private SerializedProperty _behaviourServicesProp;
        private bool _foldoutRuntime = true;
        private bool _foldoutPureServices = true;
        private bool _foldoutBehaviourServices = true;

        private void OnEnable()
        {
            _pureCSharpServicesProp = serializedObject.FindProperty("_pureCSharpServices");
            _behaviourServicesProp = serializedObject.FindProperty("_behaviourServices");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            AsakiSceneContext context = (AsakiSceneContext)target;

            // Header
            DrawHeader();

            EditorGUILayout.Space(10);

            // Pure C# Services
            DrawPureCSharpServices();

            EditorGUILayout.Space(5);

            // MonoBehaviour Services
            DrawBehaviourServices();

            serializedObject.ApplyModifiedProperties();

            // Runtime Debugger
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(10);
                DrawRuntimeDebugger(context);
            }
        }

        private new void DrawHeader()
        {
            var headerStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            EditorGUILayout.BeginVertical(headerStyle);
            
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.9f, 0.5f, 0.2f) }
            };
            
            EditorGUILayout.LabelField("📍 Asaki Scene Context", titleStyle);
            EditorGUILayout.LabelField("Scope:  Current Scene Only", EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
        }

        private void DrawPureCSharpServices()
        {
            EditorGUILayout.BeginVertical(EditorStyles. helpBox);
            
            var foldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                normal = { textColor = new Color(0.3f, 0.7f, 0.3f) },
                fontStyle = FontStyle.Bold
            };
            
            var foldoutRect = EditorGUILayout.GetControlRect();
            _foldoutPureServices = EditorGUI.Foldout(foldoutRect, _foldoutPureServices, 
                $"Pure C# Services ({_pureCSharpServicesProp.arraySize})", true, foldoutStyle);

            if (_foldoutPureServices)
            {
                EditorGUI.indentLevel++;
                
                // 使用自定义绘制，确保 AsakiInterfaceDrawer 被调用
                EditorGUILayout.PropertyField(_pureCSharpServicesProp, true);
                
                EditorGUI.indentLevel--;

                if (_pureCSharpServicesProp. arraySize == 0)
                {
                    EditorGUILayout.HelpBox(
                        "No pure C# services configured.\n" +
                        "These must be [Serializable] classes implementing IAsakiSceneService.\n" +
                        "MonoBehaviour types are NOT allowed here.", 
                        MessageType.Info);
                }
                else
                {
                    // ✅ 新增：验证纯 C# 服务列表
                    ValidatePureCSharpServices();
                }
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawBehaviourServices()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var behaviourFoldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                normal = { textColor = new Color(0.2f, 0.5f, 0.7f) },
                fontStyle = FontStyle.Bold
            };
            
            var foldoutRect = EditorGUILayout.GetControlRect();
            _foldoutBehaviourServices = EditorGUI. Foldout(foldoutRect, _foldoutBehaviourServices, 
                $"MonoBehaviour Services ({_behaviourServicesProp.arraySize})", true, behaviourFoldoutStyle);

            if (_foldoutBehaviourServices)
            {
                EditorGUI.indentLevel++;
                
                // ✅ 使用自定义列表绘制，带类型验证
                DrawBehaviourServicesList();
                
                EditorGUI.indentLevel--;

                if (_behaviourServicesProp.arraySize == 0)
                {
                    EditorGUILayout.HelpBox(
                        "No MonoBehaviour services configured.\n" +
                        "Drag & drop scene objects that implement IAsakiSceneService.\n" +
                        "Only components implementing IAsakiSceneService are allowed.", 
                        MessageType.Info);
                }
            }
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// ✅ 新增：验证纯 C# 服务列表（检测是否包含 MonoBehaviour）
        /// </summary>
        private void ValidatePureCSharpServices()
        {
            for (int i = 0; i < _pureCSharpServicesProp.arraySize; i++)
            {
                var element = _pureCSharpServicesProp.GetArrayElementAtIndex(i);
                
                if (element.managedReferenceValue == null) continue;

                Type elementType = element.managedReferenceValue.GetType();

                // 检查是否为 MonoBehaviour
                if (typeof(MonoBehaviour).IsAssignableFrom(elementType))
                {
                    EditorGUILayout.HelpBox(
                        $"❌ ERROR: {elementType.Name} is a MonoBehaviour!\n" +
                        "MonoBehaviour types should be added to 'MonoBehaviour Services' list instead.", 
                        MessageType.Error);
                    
                    if (GUILayout.Button("Remove This Entry"))
                    {
                        _pureCSharpServicesProp. DeleteArrayElementAtIndex(i);
                        serializedObject.ApplyModifiedProperties();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// ✅ 绘制 MonoBehaviour 服务列表，带类型验证
        /// </summary>
        private void DrawBehaviourServicesList()
        {
            // 绘制列表大小控制
            int newSize = EditorGUILayout.IntField("Size", _behaviourServicesProp.arraySize);
            if (newSize != _behaviourServicesProp.arraySize)
            {
                _behaviourServicesProp.arraySize = newSize;
            }

            // 绘制每个元素
            for (int i = 0; i < _behaviourServicesProp.arraySize; i++)
            {
                var element = _behaviourServicesProp.GetArrayElementAtIndex(i);
                
                EditorGUILayout.BeginHorizontal();

                // 绘制对象字段
                var newValue = EditorGUILayout. ObjectField(
                    $"Element {i}", 
                    element. objectReferenceValue, 
                    typeof(MonoBehaviour), 
                    true);

                // 验证新值
                if (newValue != element.objectReferenceValue)
                {
                    if (newValue == null || newValue is IAsakiSceneService)
                    {
                        element.objectReferenceValue = newValue;
                    }
                    else
                    {
                        EditorUtility.DisplayDialog(
                            "Invalid Service Type",
                            $"{newValue.GetType().Name} does not implement IAsakiSceneService!\n\n" +
                            "Only MonoBehaviour components implementing IAsakiSceneService can be added.",
                            "OK");
                    }
                }

                EditorGUILayout.EndHorizontal();

                // 显示当前对象的验证信息
                if (element.objectReferenceValue != null)
                {
                    var obj = element.objectReferenceValue as MonoBehaviour;
                    
                    if (obj is IAsakiSceneService)
                    {
                        // 显示实现的接口
                        var interfaces = obj.GetType().GetInterfaces()
                            .Where(t => typeof(IAsakiService).IsAssignableFrom(t) && 
                                       t != typeof(IAsakiService) &&
                                       t != typeof(IAsakiSceneService))
                            .Select(t => t.Name)
                            .ToArray();

                        if (interfaces.Length > 0)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.LabelField($"✅ Provides: {string.Join(", ", interfaces)}", 
                                EditorStyles.miniLabel);
                            EditorGUI.indentLevel--;
                        }
                    }
                    else
                    {
                        EditorGUI.indentLevel++;
                        if (obj)
                            EditorGUILayout.HelpBox(
                                $"❌ {obj.GetType().Name} does not implement IAsakiSceneService!",
                                MessageType.Error);
                        else
                            EditorGUILayout.HelpBox(
                                "❌ This object does not exist!",
                                MessageType.Error);
                        if (GUILayout. Button("Remove This Entry"))
                        {
                            _behaviourServicesProp.DeleteArrayElementAtIndex(i);
                            break;
                        }
                        
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUILayout.Space(3);
            }
        }

        private void DrawRuntimeDebugger(AsakiSceneContext context)
        {
            var debuggerStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            EditorGUILayout.BeginVertical(debuggerStyle);

            var services = context.GetRuntimeServices();

            // Header
            EditorGUILayout. BeginHorizontal();
            EditorGUILayout.LabelField("🔍 Runtime Debugger", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Active Services: {services.Count}", EditorStyles.miniLabel);

            _foldoutRuntime = EditorGUILayout.Foldout(_foldoutRuntime, "Service List", true);
            if (_foldoutRuntime)
            {
                if (services. Count > 0)
                {
                    foreach (var kvp in services)
                    {
                        DrawServiceEntry(kvp.Key, kvp.Value);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Empty Context", EditorStyles.centeredGreyMiniLabel);
                }
            }

            EditorGUILayout. EndVertical();

            // 自动刷新
            if (Event.current.type == EventType. Layout)
            {
                Repaint();
            }
        }

        private void DrawServiceEntry(Type type, IAsakiService service)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 类型名称
            string icon = service is MonoBehaviour ? "🎮" : "🔹";
            EditorGUILayout.LabelField($"{icon} {type.Name}", EditorStyles.boldLabel);

            // 如果是 MonoBehaviour，显示引用
            if (service is MonoBehaviour behaviour)
            {
                GUI.enabled = false;
                EditorGUILayout.ObjectField("Instance", behaviour, typeof(MonoBehaviour), true);
                GUI.enabled = true;
            }
            else
            {
                EditorGUILayout.LabelField($"Type: Pure C# ({service.GetType().Name})", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }
    }
}