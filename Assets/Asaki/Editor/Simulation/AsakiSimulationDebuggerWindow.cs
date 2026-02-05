// File: Asaki/Editor/Simulation/AsakiSimulationDebuggerWindow.cs

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Asaki.Core.Simulation;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Simulation
{
    /// <summary>
    /// [Asaki Editor] Simulation 服务调试窗口
    /// 用于可视化管理和调试所有已注册的Tick对象
    /// </summary>
    public class AsakiSimulationDebuggerWindow : EditorWindow
    {
        // =========================================================
        // 数据模型
        // =========================================================

        /// <summary>
        /// Tick对象信息包装类
        /// </summary>
        private class TickableInfo
        {
            public string Name;
            public string TypeName;
            public string FullTypeName;
            public int Priority;
            public TickableType Type;
            public object Target;
            public bool IsNull;
            public bool IsActive;
        }

        /// <summary>
        /// Tick对象类型枚举
        /// </summary>
        private enum TickableType
        {
            Tick,
            FixedTick,
            LateTick,
        }

        /// <summary>
        /// 排序模式
        /// </summary>
        private enum SortMode
        {
            Priority,
            Name,
            Type,
        }

        // =========================================================
        // 服务引用
        // =========================================================

        private AsakiSimulationService _simulationService;

        // =========================================================
        // UI状态
        // =========================================================

        private Vector2 _listScrollPos;
        private Vector2 _detailScrollPos;
        private string _searchFilter = "";
        private TickableType? _filterType = null;
        private SortMode _sortMode = SortMode.Priority;
        private bool _sortAscending = true;
        private bool _autoRefresh = true;
        private float _lastRefreshTime;
        private const float RefreshInterval = 0.2f;

        // 选中项
        private TickableInfo _selectedTickable;

        // 列表数据
        private List<TickableInfo> _tickableInfos = new List<TickableInfo>();

        // 布局参数
        private float _listPanelWidth = 350f;
        private bool _isResizing;
        private const float MinListWidth = 200f;
        private const float MaxListWidth = 600f;
        private const float ResizeHandleWidth = 5f;

        // 折叠状态
        private readonly Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();

        // =========================================================
        // 窗口入口
        // =========================================================

        [MenuItem("Asaki/Diagnostics/Simulation Debugger", false, 55)]
        public static void ShowWindow()
        {
            var window = GetWindow<AsakiSimulationDebuggerWindow>("Simulation Debugger");
            window.minSize = new Vector2(700, 500);
            window.Show();
        }

        // =========================================================
        // 生命周期
        // =========================================================

        private void OnEnable()
        {
            _lastRefreshTime = Time.realtimeSinceStartup;
            EditorApplication.update += OnEditorUpdate;
            RefreshTickableData();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (_autoRefresh && Time.realtimeSinceStartup - _lastRefreshTime > RefreshInterval)
            {
                RefreshTickableData();
                _lastRefreshTime = Time.realtimeSinceStartup;
                Repaint();
            }
        }

        // =========================================================
        // 主渲染
        // =========================================================

        private void OnGUI()
        {
            // 上部容器：工具栏和搜索栏
            DrawToolbar();

            EditorGUILayout.Space(2);

            // 下部容器：左右可变宽度布局
            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawResizeHandle();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();

            // 处理拖动调整大小
            HandleResizeEvents();
        }

        // =========================================================
        // 工具栏
        // =========================================================

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 刷新按钮
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshTickableData();
            }

            // 自动刷新开关
            _autoRefresh = GUILayout.Toggle(
                _autoRefresh,
                "Auto",
                EditorStyles.toolbarButton,
                GUILayout.Width(50)
            );

            GUILayout.Space(10);

            // 搜索框
            EditorGUI.BeginChangeCheck();
            _searchFilter = EditorGUILayout.TextField(
                _searchFilter,
                EditorStyles.toolbarSearchField,
                GUILayout.Width(180)
            );
            if (EditorGUI.EndChangeCheck())
            {
                RefreshTickableData();
            }

            GUILayout.Space(10);

            // 类型过滤
            EditorGUILayout.LabelField("Filter:", GUILayout.Width(40));
            var filterIndex = _filterType.HasValue ? (int)_filterType.Value + 1 : 0;
            var filterOptions = new[] { "All", "Tick", "FixedTick", "LateTick" };
            var newFilterIndex = EditorGUILayout.Popup(
                filterIndex,
                filterOptions,
                EditorStyles.toolbarPopup,
                GUILayout.Width(80)
            );
            if (newFilterIndex != filterIndex)
            {
                _filterType =
                    newFilterIndex == 0 ? (TickableType?)null : (TickableType)(newFilterIndex - 1);
                RefreshTickableData();
            }

            GUILayout.Space(10);

            // 排序模式
            EditorGUILayout.LabelField("Sort:", GUILayout.Width(35));
            var sortOptions = new[] { "Priority", "Name", "Type" };
            var newSortMode = (SortMode)
                EditorGUILayout.Popup(
                    (int)_sortMode,
                    sortOptions,
                    EditorStyles.toolbarPopup,
                    GUILayout.Width(70)
                );
            if (newSortMode != _sortMode)
            {
                _sortMode = newSortMode;
                RefreshTickableData();
            }

            // 排序方向
            if (
                GUILayout.Button(
                    _sortAscending ? "▲" : "▼",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(25)
                )
            )
            {
                _sortAscending = !_sortAscending;
                RefreshTickableData();
            }

            GUILayout.FlexibleSpace();

            // 统计信息
            var stats = GetStatsText();
            EditorGUILayout.LabelField(stats, EditorStyles.toolbar, GUILayout.Width(150));

            EditorGUILayout.EndHorizontal();
        }

        private string GetStatsText()
        {
            if (_simulationService == null)
                return "Service: N/A";

            var (tickCount, fixedTickCount, lateTickCount) = _simulationService.GetTickableStats();
            return $"T:{tickCount} F:{fixedTickCount} L:{lateTickCount}";
        }

        // =========================================================
        // 左侧面板：Tick对象列表
        // =========================================================

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(_listPanelWidth));

            // 服务状态指示
            DrawServiceStatus();

            EditorGUILayout.Space(2);

            // 列表标题
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Tick Objects", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"Count: {_tickableInfos.Count}", GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            // 列表内容
            _listScrollPos = EditorGUILayout.BeginScrollView(_listScrollPos, GUI.skin.box);

            if (_simulationService == null)
            {
                EditorGUILayout.HelpBox(
                    "Simulation Service not available.\nMake sure the game is running and Bootstrapper has initialized.",
                    MessageType.Warning
                );
            }
            else if (_tickableInfos.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No tick objects registered.\nObjects will appear here when registered with the Simulation Service.",
                    MessageType.Info
                );
            }
            else
            {
                foreach (var info in _tickableInfos)
                {
                    DrawTickableListItem(info);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawServiceStatus()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Service:", GUILayout.Width(50));

            if (_simulationService == null)
            {
                if (TryGetService())
                {
                    EditorGUILayout.LabelField(
                        "Connected",
                        new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } }
                    );
                }
                else
                {
                    EditorGUILayout.LabelField(
                        "Not Available",
                        new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } }
                    );
                }
            }
            else
            {
                EditorGUILayout.LabelField(
                    "Connected",
                    new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } }
                );
            }

            GUILayout.FlexibleSpace();

            // 快速操作按钮
            if (_simulationService != null)
            {
                GUI.enabled = false; // 这些功能需要更多实现
                if (GUILayout.Button("Pause All", EditorStyles.miniButton, GUILayout.Width(70)))
                {
                    // TODO: 实现暂停功能
                }
                GUI.enabled = true;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTickableListItem(TickableInfo info)
        {
            bool isSelected = _selectedTickable == info;
            bool isNull = info.IsNull;

            // 根据类型设置颜色
            Color typeColor = GetTypeColor(info.Type);

            // 选中状态背景
            GUIStyle itemStyle = new GUIStyle(EditorStyles.helpBox);
            if (isSelected)
            {
                GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f);
            }
            else if (isNull)
            {
                GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
            }

            EditorGUILayout.BeginHorizontal(itemStyle);
            GUI.backgroundColor = Color.white;

            // 类型指示器
            Rect typeRect = GUILayoutUtility.GetRect(8, 16, GUILayout.Width(8));
            EditorGUI.DrawRect(typeRect, typeColor);

            GUILayout.Space(4);

            // 优先级标签
            EditorGUILayout.LabelField(
                info.Priority.ToString(),
                new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight },
                GUILayout.Width(35)
            );

            GUILayout.Space(4);

            // 名称和类型
            EditorGUILayout.BeginVertical();

            // 对象名称
            GUIStyle nameStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 11,
            };
            if (isNull)
            {
                nameStyle.normal.textColor = Color.gray;
            }
            EditorGUILayout.LabelField(info.Name, nameStyle);

            // 类型名称
            EditorGUILayout.LabelField(
                info.TypeName,
                new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } }
            );

            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // 类型标签
            string typeLabel = info.Type.ToString();
            EditorGUILayout.LabelField(
                typeLabel,
                new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = typeColor },
                    alignment = TextAnchor.MiddleRight,
                },
                GUILayout.Width(60)
            );

            EditorGUILayout.EndHorizontal();

            // 点击选择
            Rect itemRect = GUILayoutUtility.GetLastRect();
            if (
                Event.current.type == EventType.MouseDown
                && itemRect.Contains(Event.current.mousePosition)
            )
            {
                _selectedTickable = info;
                Event.current.Use();
                Repaint();
            }
        }

        private Color GetTypeColor(TickableType type)
        {
            return type switch
            {
                TickableType.Tick => new Color(0.2f, 0.8f, 1f), // 青色
                TickableType.FixedTick => new Color(1f, 0.6f, 0.2f), // 橙色
                TickableType.LateTick => new Color(0.6f, 0.8f, 0.2f), // 绿色
                _ => Color.gray,
            };
        }

        // =========================================================
        // 调整大小手柄
        // =========================================================

        private void DrawResizeHandle()
        {
            Rect handleRect = GUILayoutUtility.GetRect(
                ResizeHandleWidth,
                EditorGUIUtility.singleLineHeight,
                GUIStyle.none,
                GUILayout.ExpandHeight(true),
                GUILayout.Width(ResizeHandleWidth)
            );

            // 绘制手柄视觉
            EditorGUI.DrawRect(handleRect, new Color(0.15f, 0.15f, 0.15f, 0.5f));

            // 中心线
            Rect lineRect = new Rect(
                handleRect.x + handleRect.width / 2 - 1,
                handleRect.y,
                2,
                handleRect.height
            );
            EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.3f, 0.3f, 0.8f));

            // 鼠标悬停效果
            if (handleRect.Contains(Event.current.mousePosition))
            {
                EditorGUI.DrawRect(handleRect, new Color(0.3f, 0.5f, 0.8f, 0.3f));
                EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeHorizontal);
            }

            // 存储手柄区域用于事件处理
            _resizeHandleRect = handleRect;
        }

        private Rect _resizeHandleRect;

        private void HandleResizeEvents()
        {
            Event e = Event.current;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (_resizeHandleRect.Contains(e.mousePosition))
                    {
                        _isResizing = true;
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (_isResizing)
                    {
                        _listPanelWidth += e.delta.x;
                        _listPanelWidth = Mathf.Clamp(_listPanelWidth, MinListWidth, MaxListWidth);
                        e.Use();
                        Repaint();
                    }
                    break;

                case EventType.MouseUp:
                    if (_isResizing)
                    {
                        _isResizing = false;
                        e.Use();
                    }
                    break;
            }
        }

        // =========================================================
        // 右侧面板：详细信息
        // =========================================================

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();

            if (_selectedTickable == null)
            {
                DrawEmptyDetailPanel();
            }
            else
            {
                DrawTickableDetail(_selectedTickable);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawEmptyDetailPanel()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.FlexibleSpace();

            GUIStyle style = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 14,
                normal = { textColor = Color.gray },
            };

            EditorGUILayout.LabelField("Select a Tick object to view details", style);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        private void DrawTickableDetail(TickableInfo info)
        {
            _detailScrollPos = EditorGUILayout.BeginScrollView(_detailScrollPos);

            // 头部信息
            DrawDetailHeader(info);

            EditorGUILayout.Space(10);

            // 类型信息
            DrawTypeInfo(info);

            EditorGUILayout.Space(10);

            // 对象详情
            DrawObjectDetails(info);

            EditorGUILayout.Space(10);

            // 操作按钮
            DrawActionButtons(info);

            EditorGUILayout.EndScrollView();
        }

        private void DrawDetailHeader(TickableInfo info)
        {
            EditorGUILayout.BeginVertical("box");

            // 名称和类型标识
            EditorGUILayout.BeginHorizontal();

            // 类型颜色块
            Color typeColor = GetTypeColor(info.Type);
            Rect colorRect = GUILayoutUtility.GetRect(12, 40, GUILayout.Width(12));
            EditorGUI.DrawRect(colorRect, typeColor);

            GUILayout.Space(8);

            EditorGUILayout.BeginVertical();

            // 对象名称
            GUIStyle nameStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 16,
            };
            EditorGUILayout.LabelField(info.Name, nameStyle);

            // 完整类型名
            EditorGUILayout.LabelField(
                info.FullTypeName,
                new GUIStyle(EditorStyles.miniLabel) { wordWrap = true }
            );

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 状态指示
            EditorGUILayout.BeginHorizontal();

            if (info.IsNull)
            {
                EditorGUILayout.LabelField(
                    "⚠ Object is null (may have been destroyed)",
                    new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } }
                );
            }
            else
            {
                EditorGUILayout.LabelField(
                    "✓ Object is active",
                    new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } }
                );
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawTypeInfo(TickableInfo info)
        {
            EditorGUILayout.BeginVertical("box");

            GUILayout.Label("Tick Information", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);

            // 类型
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tick Type:", GUILayout.Width(100));
            Color typeColor = GetTypeColor(info.Type);
            GUIStyle typeStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = typeColor },
                fontStyle = FontStyle.Bold,
            };
            EditorGUILayout.LabelField(info.Type.ToString(), typeStyle);
            EditorGUILayout.EndHorizontal();

            // 优先级
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Priority:", GUILayout.Width(100));
            EditorGUILayout.LabelField(info.Priority.ToString());

            // 优先级标签
            string priorityLabel = GetPriorityLabel(info.Priority);
            if (!string.IsNullOrEmpty(priorityLabel))
            {
                EditorGUILayout.LabelField(
                    $"({priorityLabel})",
                    new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } }
                );
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private string GetPriorityLabel(int priority)
        {
            return priority switch
            {
                (int)TickPriority.High => "High",
                (int)TickPriority.Normal => "Normal",
                (int)TickPriority.Low => "Low",
                _ => null,
            };
        }

        private void DrawObjectDetails(TickableInfo info)
        {
            if (info.IsNull || info.Target == null)
                return;

            EditorGUILayout.BeginVertical("box");

            GUILayout.Label("Object Details", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);

            // 尝试获取Unity对象信息
            if (info.Target is UnityEngine.Object unityObj)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Unity Object:", GUILayout.Width(100));
                EditorGUILayout.ObjectField(unityObj, typeof(UnityEngine.Object), true);
                EditorGUILayout.EndHorizontal();

                // 如果是GameObject或Component，显示更多信息
                if (unityObj is GameObject go)
                {
                    EditorGUILayout.LabelField(
                        "Active in Hierarchy:",
                        go.activeInHierarchy.ToString()
                    );
                    EditorGUILayout.LabelField("Layer:", LayerMask.LayerToName(go.layer));
                    EditorGUILayout.LabelField("Tag:", go.tag);
                }
                else if (unityObj is Component comp)
                {
                    EditorGUILayout.LabelField("GameObject:", comp.gameObject?.name ?? "null");
                    if (comp is Behaviour behaviour)
                    {
                        EditorGUILayout.LabelField("Enabled:", behaviour.enabled.ToString());
                    }
                }
            }
            else
            {
                // 非Unity对象，显示基本类型信息
                EditorGUILayout.LabelField("Object Type:", "Non-Unity Object");
                EditorGUILayout.LabelField("ToString():", info.Target.ToString());
            }

            // 实现的接口
            EditorGUILayout.Space(10);
            GUILayout.Label("Implemented Interfaces", EditorStyles.miniBoldLabel);

            var interfaces = info
                .Target.GetType()
                .GetInterfaces()
                .Where(i =>
                    i == typeof(IAsakiTickable)
                    || i == typeof(IAsakiFixedTickable)
                    || i == typeof(IAsakiLateTickable)
                )
                .Select(i => i.Name)
                .ToList();

            foreach (var interfaceName in interfaces)
            {
                EditorGUILayout.LabelField($"  • {interfaceName}");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActionButtons(TickableInfo info)
        {
            EditorGUILayout.BeginVertical("box");

            GUILayout.Label("Actions", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            // 定位对象按钮
            if (info.Target is UnityEngine.Object unityObj && unityObj != null)
            {
                if (GUILayout.Button("Select in Hierarchy", GUILayout.Height(25)))
                {
                    Selection.activeObject = unityObj;
                    EditorGUIUtility.PingObject(unityObj);
                }
            }

            // 复制类型名按钮
            if (GUILayout.Button("Copy Type Name", GUILayout.Height(25)))
            {
                EditorGUIUtility.systemCopyBuffer = info.FullTypeName;
                ALog.Info($"[SimulationDebugger] Copied type name: {info.FullTypeName}");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 警告信息
            if (!info.IsNull)
            {
                EditorGUILayout.HelpBox(
                    "Note: Direct manipulation of tick objects from editor is not recommended during runtime.",
                    MessageType.Info
                );
            }

            EditorGUILayout.EndVertical();
        }

        // =========================================================
        // 数据刷新
        // =========================================================

        private void RefreshTickableData()
        {
            // 确保服务引用
            if (_simulationService == null)
            {
                if (!TryGetService())
                    return;
            }

            _tickableInfos.Clear();

            // 收集Tick对象
            var tickables = _simulationService.GetTickables();
            foreach (var wrapper in tickables)
            {
                var info = CreateTickableInfo(
                    wrapper.Tickable,
                    wrapper.Priority,
                    TickableType.Tick
                );
                if (ShouldIncludeTickable(info))
                {
                    _tickableInfos.Add(info);
                }
            }

            // 收集FixedTick对象
            var fixedTickables = _simulationService.GetFixedTickables();
            foreach (var tickable in fixedTickables)
            {
                var info = CreateTickableInfo(tickable, 0, TickableType.FixedTick);
                if (ShouldIncludeTickable(info))
                {
                    _tickableInfos.Add(info);
                }
            }

            // 收集LateTick对象
            var lateTickables = _simulationService.GetLateTickables();
            foreach (var wrapper in lateTickables)
            {
                var info = CreateTickableInfo(
                    wrapper.Tickable,
                    wrapper.Priority,
                    TickableType.LateTick
                );
                if (ShouldIncludeTickable(info))
                {
                    _tickableInfos.Add(info);
                }
            }

            // 排序
            SortTickableInfos();

            // 验证选中项是否仍然有效
            if (_selectedTickable != null)
            {
                bool stillExists = _tickableInfos.Any(t =>
                    t.Target == _selectedTickable.Target && t.Type == _selectedTickable.Type
                );

                if (!stillExists)
                {
                    _selectedTickable = null;
                }
            }
        }

        private TickableInfo CreateTickableInfo(object tickable, int priority, TickableType type)
        {
            var info = new TickableInfo
            {
                Target = tickable,
                Priority = priority,
                Type = type,
                IsNull = tickable == null,
            };

            if (tickable != null)
            {
                var typeObj = tickable.GetType();
                info.TypeName = typeObj.Name;
                info.FullTypeName = typeObj.FullName;

                // 获取显示名称
                if (tickable is UnityEngine.Object unityObj)
                {
                    info.Name = unityObj.name;
                    info.IsActive =
                        unityObj is GameObject go ? go.activeInHierarchy
                        : unityObj is Behaviour behaviour ? behaviour.enabled
                        : unityObj is Component ? true
                        : true;
                }
                else
                {
                    info.Name = typeObj.Name;
                    info.IsActive = true;
                }
            }
            else
            {
                info.Name = "<null>";
                info.TypeName = "Unknown";
                info.FullTypeName = "Unknown";
                info.IsActive = false;
            }

            return info;
        }

        private bool ShouldIncludeTickable(TickableInfo info)
        {
            // 类型过滤
            if (_filterType.HasValue && info.Type != _filterType.Value)
                return false;

            // 搜索过滤
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                string search = _searchFilter.ToLower();
                if (
                    !info.Name.ToLower().Contains(search)
                    && !info.TypeName.ToLower().Contains(search)
                    && !info.FullTypeName.ToLower().Contains(search)
                )
                {
                    return false;
                }
            }

            return true;
        }

        private void SortTickableInfos()
        {
            _tickableInfos = _sortMode switch
            {
                SortMode.Priority => _sortAscending
                    ? _tickableInfos.OrderBy(t => t.Priority).ThenBy(t => t.Name).ToList()
                    : _tickableInfos
                        .OrderByDescending(t => t.Priority)
                        .ThenBy(t => t.Name)
                        .ToList(),

                SortMode.Name => _sortAscending
                    ? _tickableInfos.OrderBy(t => t.Name).ToList()
                    : _tickableInfos.OrderByDescending(t => t.Name).ToList(),

                SortMode.Type => _sortAscending
                    ? _tickableInfos.OrderBy(t => t.Type).ThenBy(t => t.Priority).ToList()
                    : _tickableInfos
                        .OrderByDescending(t => t.Type)
                        .ThenBy(t => t.Priority)
                        .ToList(),

                _ => _tickableInfos,
            };
        }

        private bool TryGetService()
        {
            if (AsakiContext.TryGet(out IAsakiSimulationService service))
            {
                _simulationService = service as AsakiSimulationService;
                return _simulationService != null;
            }
            return false;
        }
    }
}

#endif
