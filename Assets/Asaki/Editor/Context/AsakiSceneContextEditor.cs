using System;
using System.Collections.Generic;
using System.Linq;
using Asaki.Core.Context;
using Asaki.Core.Context.Resolvers;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Context
{
    [CustomEditor(typeof(AsakiSceneContext))]
    public class AsakiSceneContextEditor : UnityEditor.Editor
    {
        #region Constants & Colors

        private static readonly Color BrandColor = new Color(0.95f, 0.55f, 0.15f);
        private static readonly Color HeaderBgColor = new Color(0.22f, 0.22f, 0.22f, 0.9f);
        private static readonly Color SectionHeaderColor = new Color(0.4f, 0.7f, 0.9f);
        private static readonly Color PrefabHeaderColor = new Color(0.3f, 0.65f, 0.35f);
        private static readonly Color RuntimeColor = new Color(0.9f, 0.7f, 0.2f);
        private static readonly Color WarningColor = new Color(0.9f, 0.5f, 0.2f);
        private static readonly Color SuccessColor = new Color(0.3f, 0.8f, 0.4f);

        private const float HeaderHeight = 38f;
        private const float SectionSpacing = 8f;

        #endregion

        #region Serialized Properties

        private SerializedProperty _pureCSharpServicesProp;
        private SerializedProperty _servicePrefabsProp;
        private SerializedProperty _instanceParentProp;

        #endregion

        #region State

        private bool _foldoutPrefabs = true;
        private bool _foldoutPureServices = true;
        private bool _foldoutRuntime = true;
        private bool _foldoutServiceList = true;

        private Texture2D _headerBgTexture;
        private GUIStyle _headerLabelStyle;
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _foldoutStyle;
        private GUIStyle _miniLabelStyle;
        private GUIStyle _statusLabelStyle;

        private List<int> _prefabServiceCounts = new List<int>();

        #endregion

        #region Lifecycle

        private bool _stylesCreated = false;

        private void OnEnable()
        {
            _pureCSharpServicesProp = serializedObject.FindProperty("_pureCSharpServices");
            _servicePrefabsProp = serializedObject.FindProperty("_servicePrefabs");
            _instanceParentProp = serializedObject.FindProperty("_instanceParent");

            _stylesCreated = false;
        }

        private void EnsureStylesCreated()
        {
            if (_stylesCreated)
                return;

            _headerBgTexture = new Texture2D(1, 1);
            _headerBgTexture.SetPixel(0, 0, HeaderBgColor);
            _headerBgTexture.Apply();

            _headerLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 0, 0),
                normal = { textColor = BrandColor }
            };

            _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(6, 6, 2, 2)
            };

            _boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(4, 4, 2, 2)
            };

            _foldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 11
            };

            _miniLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft
            };

            _statusLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(4, 4, 2, 2)
            };

            _stylesCreated = true;
        }

        #endregion

        #region Main GUI

        public override void OnInspectorGUI()
        {
            EnsureStylesCreated();

            serializedObject.Update();

            DrawBrandHeader();

            EditorGUILayout.Space(SectionSpacing);

            DrawPrefabServicesSection();

            EditorGUILayout.Space(SectionSpacing);

            DrawPureCSharpServicesSection();

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(SectionSpacing);
                DrawRuntimeSection();
            }

            EditorGUILayout.Space(4);
        }

        #endregion

        #region Brand Header

        private void DrawBrandHeader()
        {
            Rect headerRect = GUILayoutUtility.GetRect(0, HeaderHeight, GUILayout.ExpandWidth(true));

            GUI.DrawTexture(headerRect, _headerBgTexture, ScaleMode.StretchToFill);

            EditorGUI.DrawRect(
                new Rect(headerRect.x, headerRect.yMax - 2, headerRect.width, 2),
                BrandColor
            );

            GUI.Label(
                new Rect(headerRect.x + 10, headerRect.y + 8, headerRect.width - 20, 24),
                "ASAKI SCENE CONTEXT",
                _headerLabelStyle
            );

            string scopeText = "Scene Scope | v3.0 Prefab Mode";
            GUI.Label(
                new Rect(headerRect.x + 10, headerRect.y + 28, headerRect.width - 20, 16),
                scopeText,
                _miniLabelStyle
            );
        }

        #endregion

        #region Prefab Services Section

        private void DrawPrefabServicesSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);

            DrawSectionHeader(
                "Scene Service Prefabs",
                PrefabHeaderColor,
                ref _foldoutPrefabs,
                GetPrefabStatusText()
            );

            if (_foldoutPrefabs)
            {
                EditorGUILayout.Space(4);

                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(_servicePrefabsProp, new GUIContent("Prefabs"), true);

                EditorGUILayout.Space(4);

                EditorGUILayout.PropertyField(_instanceParentProp, new GUIContent("Parent Transform"));

                EditorGUI.indentLevel--;

                EditorGUILayout.Space(4);

                DrawPrefabPreview();
            }

            EditorGUILayout.EndVertical();
        }

        private string GetPrefabStatusText()
        {
            int prefabCount = _servicePrefabsProp.arraySize;
            int serviceCount = CountAllServicesInPrefabs();

            if (prefabCount == 0)
                return "No prefabs configured";
            if (serviceCount == 0)
                return $"{prefabCount} prefab(s), no services found";

            return $"{prefabCount} prefab(s) | {serviceCount} service(s)";
        }

        private int CountAllServicesInPrefabs()
        {
            int count = 0;
            _prefabServiceCounts.Clear();

            for (int i = 0; i < _servicePrefabsProp.arraySize; i++)
            {
                SerializedProperty element = _servicePrefabsProp.GetArrayElementAtIndex(i);
                GameObject prefab = element.objectReferenceValue as GameObject;

                int services = prefab != null
                    ? prefab.GetComponentsInChildren<IAsakiSceneService>(true).Length
                    : 0;

                _prefabServiceCounts.Add(services);
                count += services;
            }

            return count;
        }

        private void DrawPrefabPreview()
        {
            if (_servicePrefabsProp.arraySize == 0)
            {
                DrawInfoBox(
                    "Drag prefabs containing IAsakiSceneService components here.\n"
                        + "Prefabs will be instantiated at runtime and services auto-registered.",
                    MessageType.Info
                );
                return;
            }

            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel, GUILayout.Height(18));

            EditorGUILayout.Space(2);

            for (int i = 0; i < _servicePrefabsProp.arraySize; i++)
            {
                SerializedProperty element = _servicePrefabsProp.GetArrayElementAtIndex(i);
                GameObject prefab = element.objectReferenceValue as GameObject;

                if (prefab == null)
                {
                    DrawWarningRow(i, "Missing Prefab", "Prefab reference is null");
                    continue;
                }

                int serviceCount = i < _prefabServiceCounts.Count ? _prefabServiceCounts[i] : 0;

                DrawPrefabRow(prefab, serviceCount, i);
            }
        }

        private void DrawPrefabRow(GameObject prefab, int serviceCount, int index)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            Color statusColor = serviceCount > 0 ? SuccessColor : WarningColor;
            string statusIcon = serviceCount > 0 ? "✓" : "⚠";

            GUI.color = statusColor;
            GUILayout.Label(statusIcon, GUILayout.Width(16));
            GUI.color = Color.white;

            GUI.enabled = false;
            EditorGUILayout.ObjectField(prefab, typeof(GameObject), false, GUILayout.ExpandWidth(true));
            GUI.enabled = true;

            string countText = serviceCount == 1 ? "1 svc" : $"{serviceCount} svcs";
            GUILayout.Label(countText, _miniLabelStyle, GUILayout.Width(60));

            EditorGUILayout.EndHorizontal();

            if (serviceCount > 0)
            {
                IAsakiSceneService[] services = prefab.GetComponentsInChildren<IAsakiSceneService>(true);
                EditorGUI.indentLevel++;
                foreach (IAsakiSceneService service in services)
                {
                    if (service is MonoBehaviour behaviour)
                    {
                        Type type = behaviour.GetType();
                        string interfaceInfo = GetServiceInterfaceInfo(type);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(4);
                        GUI.color = new Color(0.5f, 0.7f, 0.5f);
                        GUILayout.Label("├", GUILayout.Width(14));
                        GUI.color = Color.white;
                        EditorGUILayout.LabelField(type.Name, _miniLabelStyle);
                        if (!string.IsNullOrEmpty(interfaceInfo))
                        {
                            GUI.color = new Color(0.5f, 0.5f, 0.6f);
                            GUILayout.Label("→", GUILayout.Width(14));
                            GUILayout.Label(interfaceInfo, _miniLabelStyle);
                            GUI.color = Color.white;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawWarningRow(int index, string title, string message)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            GUI.color = WarningColor;
            GUILayout.Label("✗", GUILayout.Width(16));
            GUI.color = Color.white;

            GUILayout.Label($"{title}: {message}", _miniLabelStyle);

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Pure C# Services Section

        private void DrawPureCSharpServicesSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);

            DrawSectionHeader(
                "Pure C# Services",
                SectionHeaderColor,
                ref _foldoutPureServices,
                GetPureServicesStatusText()
            );

            if (_foldoutPureServices)
            {
                EditorGUILayout.Space(4);

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_pureCSharpServicesProp, true);
                EditorGUI.indentLevel--;

                if (_pureCSharpServicesProp.arraySize == 0)
                {
                    EditorGUILayout.Space(4);
                    DrawInfoBox(
                        "Serializable C# classes implementing IAsakiSceneService.\n"
                            + "MonoBehaviour types should be in prefab services.",
                        MessageType.Info
                    );
                }
                else
                {
                    ValidatePureCSharpServices();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private string GetPureServicesStatusText()
        {
            int count = _pureCSharpServicesProp.arraySize;
            return count == 0 ? "No services" : $"{count} service(s)";
        }

        private void ValidatePureCSharpServices()
        {
            for (int i = 0; i < _pureCSharpServicesProp.arraySize; i++)
            {
                SerializedProperty element = _pureCSharpServicesProp.GetArrayElementAtIndex(i);

                if (element.managedReferenceValue == null)
                    continue;

                Type elementType = element.managedReferenceValue.GetType();

                if (typeof(MonoBehaviour).IsAssignableFrom(elementType))
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.HelpBox(
                        $"❌ {elementType.Name} is a MonoBehaviour!\n"
                            + "Move it to prefab services instead.",
                        MessageType.Error
                    );

                    if (GUILayout.Button("Remove This Entry", GUILayout.Height(20)))
                    {
                        _pureCSharpServicesProp.DeleteArrayElementAtIndex(i);
                        serializedObject.ApplyModifiedProperties();
                        break;
                    }
                }
            }
        }

        #endregion

        #region Runtime Section

        private void DrawRuntimeSection()
        {
            AsakiSceneContext context = (AsakiSceneContext)target;

            EditorGUILayout.BeginVertical(_boxStyle);

            DrawSectionHeader(
                "Runtime Debugger",
                RuntimeColor,
                ref _foldoutRuntime,
                context.IsBuilt ? "Built ✓" : "Pending Build"
            );

            if (_foldoutRuntime)
            {
                EditorGUILayout.Space(4);

                Dictionary<Type, IAsakiService> services = context.GetRuntimeServices();
                List<GameObject> prefabs = context.GetInstantiatedPrefabs();

                DrawRuntimeStats(services.Count, prefabs.Count, context.IsBuilt);

                EditorGUILayout.Space(4);

                DrawRuntimeServiceList(services);
            }

            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Layout)
            {
                Repaint();
            }
        }

        private void DrawRuntimeStats(int serviceCount, int prefabCount, bool isBuilt)
        {
            EditorGUILayout.BeginHorizontal();

            DrawStatBox("Services", serviceCount.ToString(), SuccessColor);
            DrawStatBox("Prefabs", prefabCount.ToString(), PrefabHeaderColor);
            DrawStatBox("Status", isBuilt ? "Built" : "Pending", isBuilt ? SuccessColor : WarningColor);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatBox(string label, string value, Color valueColor)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(40));

            GUI.color = valueColor;
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel, GUILayout.Height(20));
            GUI.color = Color.white;

            EditorGUILayout.LabelField(label, _miniLabelStyle, GUILayout.Height(14));

            EditorGUILayout.EndVertical();
        }

        private void DrawRuntimeServiceList(Dictionary<Type, IAsakiService> services)
        {
            _foldoutServiceList = EditorGUILayout.Foldout(_foldoutServiceList, "Service List", true);

            if (!_foldoutServiceList)
                return;

            if (services.Count == 0)
            {
                EditorGUILayout.LabelField("  No services registered", _miniLabelStyle);
                return;
            }

            foreach (KeyValuePair<Type, IAsakiService> kvp in services)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                string icon = kvp.Value is MonoBehaviour ? "🎮" : "🔹";
                EditorGUILayout.LabelField($"{icon} {kvp.Key.Name}", EditorStyles.miniLabel);

                if (kvp.Value is MonoBehaviour behaviour)
                {
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField(behaviour, typeof(MonoBehaviour), true, GUILayout.Width(120));
                    GUI.enabled = true;
                }
                else
                {
                    GUI.color = new Color(0.5f, 0.5f, 0.6f);
                    EditorGUILayout.LabelField($"({kvp.Value.GetType().Name})", _miniLabelStyle, GUILayout.Width(120));
                    GUI.color = Color.white;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        #endregion

        #region Utility Methods

        private void DrawSectionHeader(string title, Color color, ref bool foldout, string statusText)
        {
            EditorGUILayout.BeginHorizontal();

            Color foldoutColor = _foldoutStyle.normal.textColor;
            _foldoutStyle.normal.textColor = color;

            foldout = EditorGUILayout.Foldout(foldout, title, true, _foldoutStyle);

            _foldoutStyle.normal.textColor = foldoutColor;

            GUILayout.FlexibleSpace();

            GUI.color = new Color(color.r, color.g, color.b, 0.7f);
            GUILayout.Label(statusText, _miniLabelStyle);
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();

            Rect lineRect = GUILayoutUtility.GetLastRect();
            EditorGUI.DrawRect(
                new Rect(lineRect.x, lineRect.yMax - 1, lineRect.width, 1),
                new Color(color.r, color.g, color.b, 0.3f)
            );
        }

        private void DrawInfoBox(string message, MessageType type)
        {
            EditorGUILayout.HelpBox(message, type);
        }

        private string GetServiceInterfaceInfo(Type type)
        {
            Type[] interfaces = type
                .GetInterfaces()
                .Where(t =>
                    typeof(IAsakiService).IsAssignableFrom(t)
                    && t != typeof(IAsakiService)
                    && t != typeof(IAsakiSceneService)
                    && t != typeof(IAsakiGlobalService)
                )
                .ToArray();

            return interfaces.Length > 0 ? string.Join(", ", interfaces.Select(t => t.Name)) : "";
        }

        #endregion
    }
}
