using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Asaki.Core.Context;
using Asaki.Core.Pooling;
using Asaki.Core.Pooling.Interfaces;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Debugging
{
    public class AsakiSmartPoolDebuggerWindow : EditorWindow
    {
        [MenuItem("Asaki/Diagnostics/Pool Debugger", false, 54)]
        public static void ShowWindow()
        {
            AsakiSmartPoolDebuggerWindow wnd = GetWindow<AsakiSmartPoolDebuggerWindow>();
            wnd.titleContent = new GUIContent("Smart Pool (V5.1)");
            wnd.minSize = new Vector2(600, 400);
            wnd.Show();
        }

        // --- 数据模型 (DTO) ---
        private class PoolViewData
        {
            public string Key;
            public int InactiveCount;
            public GameObject Prefab;
            public bool HasValidHandle;
        }

        // --- 状态 ---
        private List<PoolViewData> _viewData = new List<PoolViewData>();
        private string _searchFilter = "";
        private string _selectedKey;
        private Vector2 _scrollLeft;
        private Vector2 _scrollRight;
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private float _splitSize = 250f;

        // --- 样式 ---
        private GUIStyle _itemStyle;

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (Application.isPlaying && _autoRefresh)
            {
                if (EditorApplication.timeSinceStartup - _lastRefreshTime > 0.5f) // 降低刷新频率
                {
                    FetchData();
                    _lastRefreshTime = EditorApplication.timeSinceStartup;
                    Repaint();
                }
            }
        }

        private void OnGUI()
        {
            InitStyles();
            DrawToolbar();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("AsakiSmartPool runs in Play Mode.", MessageType.Info);
                return;
            }

            // 检查服务是否就绪
            if (!AsakiContext.TryGet<IAsakiPoolService>(out IAsakiPoolService service))
            {
                EditorGUILayout.HelpBox(
                    "IAsakiPoolService not found in Context.\nMake sure Bootstrapper has started.",
                    MessageType.Warning
                );
                return;
            }

            EditorGUILayout.BeginHorizontal();

            // 1. 左侧列表
            DrawListPanel();

            // 2. 分隔条
            // 注意：如果你没有 GUILayoutExtensions，可以用简单的 Space 代替
            // GUILayoutExtensions.Splitter(ref _splitSize, 150f, position.width - 200f);
            GUILayout.Box("", GUILayout.Width(1), GUILayout.ExpandHeight(true));

            // 3. 右侧详情
            DrawDetailsPanel();

            EditorGUILayout.EndHorizontal();
        }

        private void InitStyles()
        {
            if (_itemStyle == null)
            {
                _itemStyle = new GUIStyle(GUI.skin.button);
                _itemStyle.alignment = TextAnchor.MiddleLeft;
                _itemStyle.padding = new RectOffset(10, 10, 5, 5);
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (
                GUILayout.Button(
                    "Release Selected",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(120)
                )
            )
            {
                if (!string.IsNullOrEmpty(_selectedKey))
                {
                    if (AsakiContext.TryGet<IAsakiPoolService>(out IAsakiPoolService service))
                    {
                        service.DestroyPool(_selectedKey);
                        FetchData();
                    }
                }
            }

            GUILayout.Space(10);
            _autoRefresh = GUILayout.Toggle(
                _autoRefresh,
                "Auto Refresh",
                EditorStyles.toolbarButton,
                GUILayout.Width(90)
            );

            GUILayout.FlexibleSpace();
            _searchFilter = EditorGUILayout.TextField(
                _searchFilter,
                EditorStyles.toolbarSearchField,
                GUILayout.Width(200)
            );

            EditorGUILayout.EndHorizontal();
        }

        private void DrawListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(_splitSize));
            _scrollLeft = EditorGUILayout.BeginScrollView(_scrollLeft);

            if (_viewData != null)
            {
                foreach (PoolViewData data in _viewData)
                {
                    if (
                        !string.IsNullOrEmpty(_searchFilter)
                        && !data
                            .Key.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase)
                            .Equals(-1)
                    )
                        continue;

                    bool isSelected = data.Key == _selectedKey;

                    // 显示逻辑
                    string label = $"{data.Key} ({data.InactiveCount})";
                    if (!data.HasValidHandle)
                        label += " [Invalid]";

                    GUIStyle currentStyle = new GUIStyle(_itemStyle);
                    if (isSelected)
                    {
                        currentStyle.normal.textColor = Color.cyan;
                        currentStyle.fontStyle = FontStyle.Bold;
                    }

                    if (GUILayout.Button(label, currentStyle))
                    {
                        _selectedKey = data.Key;
                        GUI.FocusControl(null);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawDetailsPanel()
        {
            EditorGUILayout.BeginVertical();
            _scrollRight = EditorGUILayout.BeginScrollView(_scrollRight);

            PoolViewData data = _viewData.FirstOrDefault(d => d.Key == _selectedKey);

            if (data != null)
            {
                DrawPoolDetails(data);
            }
            else
            {
                EditorGUILayout.HelpBox("Select a pool to inspect.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawPoolDetails(PoolViewData data)
        {
            EditorGUILayout.LabelField(data.Key, EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 1. 资源状态
            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.LabelField("Resource Status", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Handle Valid:", GUILayout.Width(100));
            EditorGUILayout.LabelField(
                data.HasValidHandle.ToString(),
                data.HasValidHandle
                    ? EditorStyles.label
                    : new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } }
            );
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Prefab Asset:", GUILayout.Width(100));
            EditorGUILayout.ObjectField(data.Prefab, typeof(GameObject), false);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 2. 运行时状态
            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Inactive Count:", GUILayout.Width(100));
            EditorGUILayout.LabelField(data.InactiveCount.ToString(), EditorStyles.largeLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 3. 操作
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Spawn (+1)", GUILayout.Height(30)))
            {
                if (AsakiContext.TryGet<IAsakiPoolService>(out IAsakiPoolService service))
                {
                    // 尝试获取GameObject池
                    var pool = service.GetPool<GameObject>(data.Key);
                    if (pool != null)
                    {
                        _ = pool.GetAsync();
                        FetchData();
                    }
                    else
                    {
                        // 尝试获取其他类型的池
                        var poolBase = service.GetPool<object>(data.Key);
                        if (poolBase != null)
                        {
                            _ = poolBase.GetAsync();
                            FetchData();
                        }
                    }
                }
            }

            if (GUILayout.Button("Prewarm (+5)", GUILayout.Height(30)))
            {
                if (AsakiContext.TryGet<IAsakiPoolService>(out IAsakiPoolService service))
                {
                    // 尝试获取GameObject池
                    var pool = service.GetPool<GameObject>(data.Key);
                    if (pool != null)
                    {
                        _ = pool.PrewarmAsync(5);
                        FetchData();
                    }
                    else
                    {
                        // 尝试获取其他类型的池
                        var poolBase = service.GetPool<object>(data.Key);
                        if (poolBase != null)
                        {
                            _ = poolBase.PrewarmAsync(5);
                            FetchData();
                        }
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        // ==================================================================================
        // Reflection Logic (V5.1 Adapted)
        // ==================================================================================

        private void FetchData()
        {
            _viewData.Clear();

            // 1. 获取 Service 实例
            if (!AsakiContext.TryGet<IAsakiPoolService>(out IAsakiPoolService service))
                return;

            // 2. 反射获取 _pools 字典
            Type serviceType = service.GetType();
            FieldInfo poolsField = serviceType.GetField(
                "_pools",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (poolsField == null)
                return;

            IDictionary poolsDict = poolsField.GetValue(service) as IDictionary;
            if (poolsDict == null)
                return;

            // 3. 遍历对象池
            foreach (DictionaryEntry kvp in poolsDict)
            {
                string key = (string)kvp.Key;
                object poolObj = kvp.Value;

                if (poolObj == null)
                    continue;

                // 获取对象池接口
                IAsakiPoolBase poolBase = poolObj as IAsakiPoolBase;
                if (poolBase == null)
                    continue;

                // 获取统计信息
                int inactiveCount = 0;
                if (poolBase.Statistics != null)
                {
                    // 反射获取统计信息
                    Type statsType = poolBase.Statistics.GetType();
                    FieldInfo inactiveField = statsType.GetField(
                        "InactiveCount",
                        BindingFlags.Public | BindingFlags.Instance
                    );
                    if (inactiveField != null)
                    {
                        inactiveCount = (int)inactiveField.GetValue(poolBase.Statistics);
                    }
                }

                // 获取预制体信息（通过工厂）
                GameObject prefab = null;
                bool hasHandle = false;

                // 反射获取工厂信息
                Type poolType = poolObj.GetType();
                FieldInfo factoryField = poolType.GetField(
                    "_factory",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                if (factoryField != null)
                {
                    object factoryObj = factoryField.GetValue(poolObj);
                    if (factoryObj != null)
                    {
                        // 尝试从工厂中获取预制体
                        Type factoryType = factoryObj.GetType();

                        // 尝试多种可能的字段名
                        FieldInfo prefabField = factoryType.GetField(
                            "_prefab",
                            BindingFlags.NonPublic | BindingFlags.Instance
                        );
                        if (prefabField == null)
                        {
                            prefabField = factoryType.GetField(
                                "prefab",
                                BindingFlags.Public | BindingFlags.Instance
                            );
                        }
                        if (prefabField == null)
                        {
                            prefabField = factoryType.GetField(
                                "Prefab",
                                BindingFlags.Public | BindingFlags.Instance
                            );
                        }

                        if (prefabField != null)
                        {
                            prefab = prefabField.GetValue(factoryObj) as GameObject;
                            hasHandle = prefab != null;
                        }

                        // 尝试从ResourceHandle获取
                        if (!hasHandle)
                        {
                            FieldInfo handleField = factoryType.GetField(
                                "_handle",
                                BindingFlags.NonPublic | BindingFlags.Instance
                            );
                            if (handleField != null)
                            {
                                object handleObj = handleField.GetValue(factoryObj);
                                if (handleObj != null)
                                {
                                    Type handleType = handleObj.GetType();
                                    FieldInfo assetField = handleType.GetField(
                                        "Asset",
                                        BindingFlags.Public | BindingFlags.Instance
                                    );
                                    if (assetField != null)
                                    {
                                        prefab = assetField.GetValue(handleObj) as GameObject;
                                        hasHandle = prefab != null;
                                    }
                                }
                            }
                        }
                    }
                }

                _viewData.Add(
                    new PoolViewData
                    {
                        Key = key,
                        InactiveCount = inactiveCount,
                        Prefab = prefab,
                        HasValidHandle = hasHandle,
                    }
                );
            }
        }
    }
}
