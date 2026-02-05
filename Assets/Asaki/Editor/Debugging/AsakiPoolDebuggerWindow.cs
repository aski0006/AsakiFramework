using System;
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
    /// <summary>
    /// Asaki 对象池调试窗口
    /// 提供池的实时监控、统计信息可视化管理功能
    /// </summary>
    public class AsakiPoolDebuggerWindow : EditorWindow
    {
        [MenuItem("Asaki/Diagnostics/Pool Debugger", false, 54)]
        public static void ShowWindow()
        {
            AsakiPoolDebuggerWindow window = GetWindow<AsakiPoolDebuggerWindow>();
            window.titleContent = new GUIContent(
                "Pool Debugger",
                EditorGUIUtility.IconContent("Profiler.Memory").image
            );
            window.minSize = new Vector2(700, 500);
            window.Show();
        }

        #region 数据模型

        /// <summary>
        /// 池视图数据模型
        /// </summary>
        private class PoolViewData
        {
            public string Key;
            public Type ObjectType;
            public AsakiPoolConfig Config;
            public IAsakiPoolStatistics Statistics;
            public IAsakiPoolBase PoolBase;

            // 快捷访问属性
            public int TotalCreated => Statistics?.TotalCreated ?? 0;
            public int ActiveCount => Statistics?.ActiveCount ?? 0;
            public int InactiveCount => Statistics?.InactiveCount ?? 0;
            public int TotalDestroyed => Statistics?.TotalDestroyed ?? 0;
            public long GetCallCount => Statistics?.GetCallCount ?? 0;
            public long ReturnCallCount => Statistics?.ReturnCallCount ?? 0;

            /// <summary>
            /// 获取使用率（0-1）
            /// </summary>
            public float GetUtilizationRate()
            {
                if (Config?.MaxSize <= 0)
                    return 0f;
                return (float)ActiveCount / Config.MaxSize;
            }

            /// <summary>
            /// 获取总对象数
            /// </summary>
            public int GetTotalObjectCount()
            {
                return ActiveCount + InactiveCount;
            }
        }

        #endregion

        #region 状态字段

        private readonly List<PoolViewData> _viewData = new List<PoolViewData>();
        private readonly Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();

        private string _searchFilter = "";
        private string _selectedKey;
        private Vector2 _listScrollPosition;
        private Vector2 _detailsScrollPosition;

        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private float _refreshInterval = 0.5f;
        private float _splitterPosition = 280f;

        private bool _showInactivePools = true;
        private SortMode _currentSortMode = SortMode.Name;
        private bool _sortAscending = true;

        private enum SortMode
        {
            Name,
            ActiveCount,
            InactiveCount,
            TotalCreated,
            UtilizationRate,
        }

        #endregion

        #region 样式缓存

        private class StyleCache
        {
            public GUIStyle SelectedItemStyle;
            public GUIStyle NormalItemStyle;
            public GUIStyle HeaderStyle;
            public GUIStyle BoxStyle;
            public GUIStyle StatsLabelStyle;
            public GUIStyle CenteredLabelStyle;
            public GUIStyle ToolbarSearchFieldStyle;

            public Texture2D GreenBarTexture;
            public Texture2D YellowBarTexture;
            public Texture2D RedBarTexture;
            public Texture2D GrayBarTexture;

            private bool _initialized;

            public void Initialize()
            {
                if (_initialized)
                    return;

                // 列表项样式
                NormalItemStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(10, 10, 6, 6),
                    fontSize = 11,
                    margin = new RectOffset(0, 0, 1, 1),
                };

                SelectedItemStyle = new GUIStyle(NormalItemStyle)
                {
                    normal =
                    {
                        textColor = Color.white,
                        background = CreateTexture(2, 2, new Color(0.2f, 0.4f, 0.8f)),
                    },
                    fontStyle = FontStyle.Bold,
                    margin = new RectOffset(0, 0, 1, 1),
                };

                // 其他样式
                HeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    margin = new RectOffset(0, 0, 8, 8),
                };

                BoxStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(12, 12, 12, 12) };

                StatsLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    richText = true,
                };

                CenteredLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                };

                ToolbarSearchFieldStyle = new GUIStyle(EditorStyles.toolbarSearchField);

                // 创建进度条纹理
                GreenBarTexture = CreateTexture(4, 4, new Color(0.2f, 0.8f, 0.2f));
                YellowBarTexture = CreateTexture(4, 4, new Color(1f, 0.8f, 0.2f));
                RedBarTexture = CreateTexture(4, 4, new Color(0.9f, 0.3f, 0.3f));
                GrayBarTexture = CreateTexture(4, 4, new Color(0.5f, 0.5f, 0.5f));

                _initialized = true;
            }

            private static Texture2D CreateTexture(int width, int height, Color color)
            {
                Texture2D texture = new Texture2D(width, height);
                Color[] pixels = new Color[width * height];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = color;
                texture.SetPixels(pixels);
                texture.Apply();
                return texture;
            }
        }

        private StyleCache _styles;

        #endregion

        #region 生命周期

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            _styles = new StyleCache();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying || !_autoRefresh)
                return;

            if (EditorApplication.timeSinceStartup - _lastRefreshTime > _refreshInterval)
            {
                RefreshData();
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void OnGUI()
        {
            _styles?.Initialize();

            DrawToolbar();

            if (!Application.isPlaying)
            {
                DrawPlayModeMessage();
                return;
            }

            if (!AsakiContext.TryGet<IAsakiPoolService>(out _))
            {
                DrawServiceNotReadyMessage();
                return;
            }

            DrawMainContent();
        }

        #endregion

        #region 绘制方法

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 刷新按钮
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshData();
            }

            // 自动刷新开关
            GUI.changed = false;
            bool newAutoRefresh = GUILayout.Toggle(
                _autoRefresh,
                "Auto",
                EditorStyles.toolbarButton,
                GUILayout.Width(50)
            );
            if (GUI.changed)
            {
                _autoRefresh = newAutoRefresh;
            }

            GUILayout.Space(10);

            // 显示非活动池开关
            GUI.changed = false;
            bool newShowInactive = GUILayout.Toggle(
                _showInactivePools,
                "Show Empty",
                EditorStyles.toolbarButton,
                GUILayout.Width(80)
            );
            if (GUI.changed)
            {
                _showInactivePools = newShowInactive;
            }

            GUILayout.Space(10);

            // 排序下拉
            EditorGUILayout.LabelField("Sort:", GUILayout.Width(32));
            _currentSortMode = (SortMode)
                EditorGUILayout.EnumPopup(
                    _currentSortMode,
                    EditorStyles.toolbarPopup,
                    GUILayout.Width(100)
                );

            // 排序方向按钮
            if (
                GUILayout.Button(
                    _sortAscending ? "▲" : "▼",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(24)
                )
            )
            {
                _sortAscending = !_sortAscending;
            }

            GUILayout.FlexibleSpace();

            // 搜索框
            _searchFilter = EditorGUILayout.TextField(
                _searchFilter,
                _styles.ToolbarSearchFieldStyle,
                GUILayout.Width(200)
            );

            if (GUILayout.Button("", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                _searchFilter = "";
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPlayModeMessage()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUIContent icon = EditorGUIUtility.IconContent("Console.InfoIcon");
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(icon, GUILayout.Width(32), GUILayout.Height(32));
            GUILayout.Space(8);
            GUILayout.Label("Enter Play Mode to view pool data", _styles.CenteredLabelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        private void DrawServiceNotReadyMessage()
        {
            EditorGUILayout.HelpBox(
                "IAsakiPoolService not found in Context.\n"
                    + "Make sure the Bootstrapper has started and PoolModule is initialized.",
                MessageType.Warning
            );
        }

        private void DrawMainContent()
        {
            EditorGUILayout.BeginHorizontal();

            // 左侧列表
            DrawPoolListPanel();

            // 分隔条
            DrawSplitter();

            // 右侧详情
            DrawDetailsPanel();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPoolListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(_splitterPosition));

            // 绘制总览信息
            DrawOverviewInfo();

            GUILayout.Space(4);

            // 绘制池列表
            _listScrollPosition = EditorGUILayout.BeginScrollView(_listScrollPosition);

            IEnumerable<PoolViewData> filteredData = _viewData;

            // 搜索过滤
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                string filter = _searchFilter.ToLowerInvariant();
                filteredData = filteredData.Where(d =>
                    d.Key.ToLowerInvariant().Contains(filter)
                    || d.ObjectType?.Name.ToLowerInvariant().Contains(filter) == true
                );
            }

            // 隐藏空池
            if (!_showInactivePools)
            {
                filteredData = filteredData.Where(d => d.GetTotalObjectCount() > 0);
            }

            // 排序
            filteredData = SortData(filteredData);

            int index = 0;
            foreach (PoolViewData data in filteredData)
            {
                DrawPoolListItem(data, index++);
            }

            if (index == 0)
            {
                EditorGUILayout.HelpBox("No pools match the current filter.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawOverviewInfo()
        {
            int totalPools = _viewData.Count;
            int totalActive = _viewData.Sum(d => d.ActiveCount);
            int totalInactive = _viewData.Sum(d => d.InactiveCount);
            int totalObjects = totalActive + totalInactive;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                $"Pools: {totalPools}",
                EditorStyles.boldLabel,
                GUILayout.Width(80)
            );
            EditorGUILayout.LabelField($"Active: {totalActive}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"Inactive: {totalInactive}", GUILayout.Width(90));
            EditorGUILayout.LabelField($"Total: {totalObjects}", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private IEnumerable<PoolViewData> SortData(IEnumerable<PoolViewData> data)
        {
            IOrderedEnumerable<PoolViewData> ordered = _currentSortMode switch
            {
                SortMode.Name => _sortAscending
                    ? data.OrderBy(d => d.Key)
                    : data.OrderByDescending(d => d.Key),
                SortMode.ActiveCount => _sortAscending
                    ? data.OrderBy(d => d.ActiveCount)
                    : data.OrderByDescending(d => d.ActiveCount),
                SortMode.InactiveCount => _sortAscending
                    ? data.OrderBy(d => d.InactiveCount)
                    : data.OrderByDescending(d => d.InactiveCount),
                SortMode.TotalCreated => _sortAscending
                    ? data.OrderBy(d => d.TotalCreated)
                    : data.OrderByDescending(d => d.TotalCreated),
                SortMode.UtilizationRate => _sortAscending
                    ? data.OrderBy(d => d.GetUtilizationRate())
                    : data.OrderByDescending(d => d.GetUtilizationRate()),
                _ => data.OrderBy(d => d.Key),
            };
            return ordered;
        }

        private void DrawPoolListItem(PoolViewData data, int index)
        {
            bool isSelected = data.Key == _selectedKey;
            int totalCount = data.GetTotalObjectCount();

            // 构建标签
            string label =
                $"{data.Key}\n<color=#888888><size=10>{data.ObjectType?.Name ?? "Unknown"} | A:{data.ActiveCount} I:{data.InactiveCount}</size></color>";

            GUIStyle style = isSelected ? _styles.SelectedItemStyle : _styles.NormalItemStyle;

            if (GUILayout.Button(label, style, GUILayout.Height(36)))
            {
                _selectedKey = data.Key;
                GUI.FocusControl(null);
            }
        }

        private void DrawSplitter()
        {
            Rect splitterRect = GUILayoutUtility.GetRect(4f, position.height, GUILayout.Width(4f));

            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

            if (Event.current.type == EventType.Repaint)
            {
                Color oldColor = GUI.color;
                GUI.color = new Color(0.15f, 0.15f, 0.15f, 1f);
                GUI.DrawTexture(splitterRect, EditorGUIUtility.whiteTexture);
                GUI.color = oldColor;
            }

            if (
                Event.current.type == EventType.MouseDown
                && splitterRect.Contains(Event.current.mousePosition)
            )
            {
                GUI.FocusControl(null);
            }

            // 简单的拖拽处理
            if (
                Event.current.type == EventType.MouseDrag
                && splitterRect.Contains(Event.current.mousePosition)
            )
            {
                _splitterPosition += Event.current.delta.x;
                _splitterPosition = Mathf.Clamp(_splitterPosition, 200f, position.width - 300f);
                Event.current.Use();
            }
        }

        private void DrawDetailsPanel()
        {
            EditorGUILayout.BeginVertical();
            _detailsScrollPosition = EditorGUILayout.BeginScrollView(_detailsScrollPosition);

            PoolViewData data = _viewData.FirstOrDefault(d => d.Key == _selectedKey);

            if (data != null)
            {
                DrawPoolDetails(data);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Select a pool from the list to view details.",
                    MessageType.Info
                );
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawPoolDetails(PoolViewData data)
        {
            // 标题
            EditorGUILayout.LabelField(data.Key, _styles.HeaderStyle);

            // 类型信息
            EditorGUILayout.LabelField(
                $"Type: {data.ObjectType?.FullName ?? "Unknown"}",
                _styles.StatsLabelStyle
            );
            GUILayout.Space(12);

            // 统计信息卡片
            DrawStatisticsSection(data);

            GUILayout.Space(12);

            // 配置信息
            DrawConfigSection(data);

            GUILayout.Space(12);

            // 操作按钮
            DrawActionsSection(data);
        }

        private void DrawStatisticsSection(PoolViewData data)
        {
            EditorGUILayout.BeginVertical(_styles.BoxStyle);
            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
            GUILayout.Space(8);

            // 使用率进度条
            float utilizationRate = data.GetUtilizationRate();
            DrawUtilizationBar(utilizationRate, data.ActiveCount, data.Config?.MaxSize ?? 0);

            GUILayout.Space(8);

            // 统计网格
            EditorGUILayout.BeginHorizontal();
            DrawStatItem("Total Created", data.TotalCreated.ToString(), Color.cyan);
            DrawStatItem("Active", data.ActiveCount.ToString(), Color.yellow);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawStatItem("Inactive", data.InactiveCount.ToString(), Color.green);
            DrawStatItem("Destroyed", data.TotalDestroyed.ToString(), Color.red);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawStatItem("Get Calls", data.GetCallCount.ToString(), Color.white);
            DrawStatItem("Return Calls", data.ReturnCallCount.ToString(), Color.white);
            EditorGUILayout.EndHorizontal();

            // 命中率
            if (data.GetCallCount > 0)
            {
                float hitRate =
                    (float)(
                        data.GetCallCount
                        - (data.TotalCreated - data.InactiveCount - data.ActiveCount)
                    )
                    / data.GetCallCount
                    * 100;
                hitRate = Mathf.Clamp(hitRate, 0f, 100f);
                GUILayout.Space(8);
                EditorGUILayout.LabelField(
                    $"Pool Hit Rate: {hitRate:F1}%",
                    _styles.StatsLabelStyle
                );
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawUtilizationBar(float rate, int activeCount, int maxSize)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Utilization:", GUILayout.Width(70));

            Rect barRect = GUILayoutUtility.GetRect(100, 18, GUILayout.ExpandWidth(true));

            // 背景
            GUI.DrawTexture(barRect, _styles.GrayBarTexture);

            // 填充
            if (rate > 0 && maxSize > 0)
            {
                Rect fillRect = new Rect(
                    barRect.x,
                    barRect.y,
                    barRect.width * rate,
                    barRect.height
                );
                Texture2D fillTexture =
                    rate < 0.5f ? _styles.GreenBarTexture
                    : rate < 0.8f ? _styles.YellowBarTexture
                    : _styles.RedBarTexture;
                GUI.DrawTexture(fillRect, fillTexture);
            }

            // 边框
            GUI.Box(barRect, "", GUI.skin.box);

            // 文本
            string label =
                maxSize > 0
                    ? $"{activeCount} / {maxSize} ({rate * 100:F0}%)"
                    : $"{activeCount} (No Limit)";
            GUI.Label(barRect, label, _styles.CenteredLabelStyle);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatItem(string label, string value, Color valueColor)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(120));
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            GUIStyle valueStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                normal = { textColor = valueColor },
                fontSize = 14,
            };
            EditorGUILayout.LabelField(value, valueStyle);
            EditorGUILayout.EndVertical();
        }

        private void DrawConfigSection(PoolViewData data)
        {
            if (data.Config == null)
            {
                EditorGUILayout.HelpBox("Configuration not available.", MessageType.Info);
                return;
            }

            // 确保折叠状态存在
            if (!_foldoutStates.TryGetValue(data.Key + "_config", out bool isExpanded))
            {
                isExpanded = false;
                _foldoutStates[data.Key + "_config"] = isExpanded;
            }

            EditorGUILayout.BeginVertical(_styles.BoxStyle);

            _foldoutStates[data.Key + "_config"] = EditorGUILayout.Foldout(
                _foldoutStates[data.Key + "_config"],
                "Configuration",
                true
            );

            if (_foldoutStates[data.Key + "_config"])
            {
                EditorGUI.indentLevel++;

                DrawConfigRow("Initial Size", data.Config.InitialSize.ToString());
                DrawConfigRow("Max Size", data.Config.MaxSize.ToString());
                DrawConfigRow("Allow Sync Creation", data.Config.AllowSyncCreation.ToString());
                DrawConfigRow("Enable Validation", data.Config.EnableValidation.ToString());
                DrawConfigRow(
                    "Enable Collection Check",
                    data.Config.EnableCollectionCheck.ToString()
                );

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Governance", EditorStyles.miniBoldLabel);

                DrawConfigRow("Auto Shrink", data.Config.EnableAutoShrink.ToString());
                DrawConfigRow("Check Interval", $"{data.Config.CheckInterval:F1}s");
                DrawConfigRow("Idle Timeout", $"{data.Config.IdleTimeout:F1}s");
                DrawConfigRow("Keep Min Size", data.Config.KeepMinSize.ToString());
                DrawConfigRow("Shrink Ratio", $"{data.Config.ShrinkRatio:P0}");

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawConfigRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(160));
            EditorGUILayout.LabelField(value);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActionsSection(PoolViewData data)
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            // 获取对象
            if (GUILayout.Button("Get Object", GUILayout.Height(28)))
            {
                PerformGetObject(data);
            }

            // 预热
            if (GUILayout.Button("Prewarm (+5)", GUILayout.Height(28)))
            {
                PerformPrewarm(data, 5);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            // 收缩
            if (GUILayout.Button("Shrink", GUILayout.Height(28)))
            {
                PerformShrink(data);
            }

            // 清空
            if (GUILayout.Button("Clear", GUILayout.Height(28)))
            {
                if (
                    EditorUtility.DisplayDialog(
                        "Clear Pool",
                        $"Are you sure you want to clear all inactive objects from pool '{data.Key}'?",
                        "Clear",
                        "Cancel"
                    )
                )
                {
                    PerformClear(data);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            // 重置统计
            if (GUILayout.Button("Reset Stats", GUILayout.Height(28)))
            {
                PerformResetStats(data);
            }

            // 销毁池
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("Destroy Pool", GUILayout.Height(28)))
            {
                if (
                    EditorUtility.DisplayDialog(
                        "Destroy Pool",
                        $"Are you sure you want to destroy pool '{data.Key}'?\n\n"
                            + "Active objects will be orphaned.",
                        "Destroy",
                        "Cancel"
                    )
                )
                {
                    PerformDestroyPool(data);
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 操作方法

        private void PerformGetObject(PoolViewData data)
        {
            if (!AsakiContext.TryGet<IAsakiPoolService>(out IAsakiPoolService service))
                return;

            // 使用反射调用泛型方法获取对象
            MethodInfo getPoolMethod = service.GetType().GetMethod("GetPool");
            if (getPoolMethod == null)
                return;

            MethodInfo genericMethod = getPoolMethod.MakeGenericMethod(
                data.ObjectType ?? typeof(object)
            );
            object pool = genericMethod.Invoke(service, new object[] { data.Key });

            if (pool != null)
            {
                MethodInfo getAsyncMethod = pool.GetType().GetMethod("GetAsync");
                getAsyncMethod?.Invoke(
                    pool,
                    new object[] { default(System.Threading.CancellationToken) }
                );
            }

            RefreshData();
        }

        private void PerformPrewarm(PoolViewData data, int count)
        {
            if (!AsakiContext.TryGet<IAsakiPoolService>(out IAsakiPoolService service))
                return;

            MethodInfo getPoolMethod = service.GetType().GetMethod("GetPool");
            if (getPoolMethod == null)
                return;

            MethodInfo genericMethod = getPoolMethod.MakeGenericMethod(
                data.ObjectType ?? typeof(object)
            );
            object pool = genericMethod.Invoke(service, new object[] { data.Key });

            if (pool != null)
            {
                MethodInfo prewarmMethod = pool.GetType().GetMethod("PrewarmAsync");
                prewarmMethod?.Invoke(
                    pool,
                    new object[] { count, 5, default(System.Threading.CancellationToken) }
                );
            }

            RefreshData();
        }

        private void PerformShrink(PoolViewData data)
        {
            // 收缩到 KeepMinSize 或当前的一半
            int targetSize = data.Config?.KeepMinSize ?? Mathf.Max(0, data.InactiveCount / 2);
            data.PoolBase?.Shrink(targetSize);
            RefreshData();
        }

        private void PerformClear(PoolViewData data)
        {
            data.PoolBase?.Clear();
            RefreshData();
        }

        private void PerformResetStats(PoolViewData data)
        {
            data.Statistics?.Reset();
            RefreshData();
        }

        private void PerformDestroyPool(PoolViewData data)
        {
            if (!AsakiContext.TryGet<IAsakiPoolService>(out IAsakiPoolService service))
                return;

            service.DestroyPool(data.Key);
            _selectedKey = null;
            RefreshData();
        }

        #endregion

        #region 数据刷新

        private void RefreshData()
        {
            _viewData.Clear();

            if (!AsakiContext.TryGet<IAsakiPoolService>(out IAsakiPoolService service))
                return;

            // 通过反射获取 _pools 字典
            FieldInfo poolsField = service
                .GetType()
                .GetField("_pools", BindingFlags.NonPublic | BindingFlags.Instance);
            if (poolsField?.GetValue(service) is not System.Collections.IDictionary poolsDict)
                return;

            foreach (System.Collections.DictionaryEntry kvp in poolsDict)
            {
                if (kvp.Value is not IAsakiPoolBase poolBase)
                    continue;

                _viewData.Add(
                    new PoolViewData
                    {
                        Key = (string)kvp.Key,
                        ObjectType = poolBase.ObjectType,
                        Config = poolBase.Config,
                        Statistics = poolBase.Statistics,
                        PoolBase = poolBase,
                    }
                );
            }
        }

        #endregion
    }
}
