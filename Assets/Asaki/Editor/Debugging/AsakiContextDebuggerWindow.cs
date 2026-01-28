using System;
using System.Collections.Generic;
using System.Reflection;
using Asaki.Core.Context;
using Asaki.Core.Context.Resolvers; // 引用 AsakiSceneContext 所在的命名空间
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Debugging
{
    public class AsakiContextDebuggerWindow : EditorWindow
    {
        // ========================================================================
        // 缓存数据 (Cache)
        // ========================================================================

        // 反射元数据缓存
        private FieldInfo _globalServicesField;
        private FieldInfo _localServicesField;
        private bool _reflectionInitialized;

        // 运行时数据快照缓存
        private Dictionary<Type, IAsakiService> _globalSnapshot;
        private Dictionary<Type, IAsakiService> _sceneSnapshot;
        private AsakiSceneContext _sceneContextInstance;

        // UI 状态
        private Vector2 _scrollPos;
        private string _searchFilter = "";
        private bool _showGlobal = true;
        private bool _showScene = true;

        // 刷新计时器 (限制反射频率)
        private double _lastUpdateTime;
        private const double RefreshInterval = 0.5f; // 每0.5秒刷新一次数据

        [MenuItem("Asaki/Debugger/Context Debugger", false, 50)]
        public static void OpenWindow()
        {
            AsakiContextDebuggerWindow window = GetWindow<AsakiContextDebuggerWindow>(
                "Asaki Context"
            );
            window.minSize = new Vector2(300, 400);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeReflection();
        }

        /// <summary>
        /// Level 1 缓存：初始化反射元数据 (FieldInfo)
        /// </summary>
        private void InitializeReflection()
        {
            try
            {
                // 1. 获取 AsakiContext 的静态私有字段 _services
                // 注意：根据你的代码，_services 是 private static volatile Dictionary...
                _globalServicesField = typeof(AsakiContext).GetField(
                    "_services",
                    BindingFlags.NonPublic | BindingFlags.Static
                );

                // 2. 获取 AsakiSceneContext 的实例私有字段 _localServices
                _localServicesField = typeof(AsakiSceneContext).GetField(
                    "_localServices",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                _reflectionInitialized =
                    _globalServicesField != null && _localServicesField != null;

                if (!_reflectionInitialized)
                {
                    Debug.LogError(
                        "[AsakiDebugger] Failed to capture internal fields via reflection. Did the framework code change?"
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AsakiDebugger] Reflection Init Error: {ex}");
            }
        }

        /// <summary>
        /// 编辑器每帧调用，用于定期刷新数据
        /// </summary>
        private void Update()
        {
            if (!Application.isPlaying)
                return;

            // 限制刷新频率，避免每帧反射
            if (EditorApplication.timeSinceStartup - _lastUpdateTime > RefreshInterval)
            {
                RefreshData();
                _lastUpdateTime = EditorApplication.timeSinceStartup;
                Repaint(); // 强制重绘 UI
            }
        }

        /// <summary>
        /// Level 2 缓存：获取运行时数据快照
        /// </summary>
        private void RefreshData()
        {
            if (!_reflectionInitialized)
                InitializeReflection();

            // 1. 获取全局服务快照
            if (_globalServicesField != null)
            {
                // 静态字段，传 null
                _globalSnapshot =
                    _globalServicesField.GetValue(null) as Dictionary<Type, IAsakiService>;
            }

            // 2. 获取场景服务快照
            // 先尝试找场景里的 Context
            if (_sceneContextInstance == null)
            {
                // Unity 2023+ 使用 FindAnyObjectByType, 旧版使用 FindObjectOfType
#if UNITY_2023_1_OR_NEWER
                _sceneContextInstance = FindAnyObjectByType<AsakiSceneContext>();
#else
                _sceneContextInstance = FindObjectOfType<AsakiSceneContext>();
#endif
            }

            if (_sceneContextInstance != null && _localServicesField != null)
            {
                _sceneSnapshot =
                    _localServicesField.GetValue(_sceneContextInstance)
                    as Dictionary<Type, IAsakiService>;
            }
            else
            {
                _sceneSnapshot = null;
            }
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Asaki Context lives in Runtime. Enter Play Mode to see services.",
                    MessageType.Info
                );
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // 全局服务部分
            DrawSection(
                "Global Services (AsakiContext)",
                ref _showGlobal,
                _globalSnapshot,
                new Color(0.2f, 0.4f, 0.6f, 1f)
            );

            GUILayout.Space(10);

            // 场景服务部分
            string sceneHeader =
                _sceneContextInstance != null
                    ? $"Scene Services ({_sceneContextInstance.gameObject.name})"
                    : "Scene Services (Not Found)";

            DrawSection(
                sceneHeader,
                ref _showScene,
                _sceneSnapshot,
                new Color(0.2f, 0.6f, 0.3f, 1f)
            );

            EditorGUILayout.EndScrollView();

            // 底部状态栏
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(
                "Auto-refreshing every 0.5s",
                EditorStyles.centeredGreyMiniLabel
            );
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Filter:", GUILayout.Width(40));
            _searchFilter = EditorGUILayout.TextField(
                _searchFilter,
                EditorStyles.toolbarSearchField
            );
            if (GUILayout.Button("Force Refresh", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                RefreshData();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSection(
            string title,
            ref bool isExpanded,
            Dictionary<Type, IAsakiService> services,
            Color headerColor
        )
        {
            // 自定义样式的折叠头
            GUIStyle style = new GUIStyle(EditorStyles.foldoutHeader);
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = headerColor;

            int count = services != null ? services.Count : 0;
            isExpanded = EditorGUILayout.Foldout(isExpanded, $"{title} [{count}]", true, style);

            if (isExpanded)
            {
                EditorGUI.indentLevel++;
                if (services == null || services.Count == 0)
                {
                    EditorGUILayout.LabelField("Empty or Null", EditorStyles.miniLabel);
                }
                else
                {
                    foreach (var kvp in services)
                    {
                        DrawServiceItem(kvp.Key, kvp.Value);
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawServiceItem(Type serviceType, IAsakiService instance)
        {
            string typeName = serviceType.Name;

            // 搜索过滤逻辑
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                if (typeName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 第一行：接口类型
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(
                new GUIContent(typeName, "Service Interface Type"),
                EditorStyles.boldLabel
            );
            GUILayout.FlexibleSpace();
            // 显示对象是否存活
            if (instance == null || instance is UnityEngine.Object obj && obj == null)
            {
                GUILayout.Label("Missing/Null", EditorStyles.miniLabel);
            }
            else
            {
                GUILayout.Label("Active", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();

            // 第二行：具体实现 + 辅助信息
            if (instance != null)
            {
                Type implType = instance.GetType();
                if (implType != serviceType)
                {
                    EditorGUILayout.LabelField($"Impl: {implType.Name}", EditorStyles.miniLabel);
                }

                // 如果是 MonoBehaviour，提供点击跳转功能
                if (instance is MonoBehaviour mb)
                {
                    if (GUILayout.Button("Select GameObject", EditorStyles.miniButton))
                    {
                        EditorGUIUtility.PingObject(mb.gameObject);
                        Selection.activeGameObject = mb.gameObject;
                    }
                }
                // 如果是纯 C# 类，尝试显示 ToString()
                else
                {
                    // 只有当 ToString 被重写时才显示，避免显示默认的类名
                    string str = instance.ToString();
                    if (str != implType.FullName)
                    {
                        EditorGUILayout.LabelField(str, EditorStyles.wordWrappedMiniLabel);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}
