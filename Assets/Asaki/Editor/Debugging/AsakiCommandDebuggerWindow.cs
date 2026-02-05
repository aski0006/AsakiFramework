using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Asaki.Core.Architecture;
using Asaki.Core.Architecture.Command;
using Asaki.Core.Context;
using Asaki.Core.Context.Resolvers;
using Asaki.Editor.Utilities.Extensions;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Debugging
{
    /// <summary>
    /// 命令调试器窗口 - 调试 CQRS 架构中的 Command 执行流程
    /// </summary>
    public class AsakiCommandDebuggerWindow : EditorWindow, IAsakiCommandDebugHook
    {
        [MenuItem("Asaki/Diagnostics/Command Debugger &F7", false, 50)]
        public static void ShowWindow()
        {
            AsakiCommandDebuggerWindow wnd = GetWindow<AsakiCommandDebuggerWindow>();
            wnd.titleContent = new GUIContent(
                "Asaki Commands",
                EditorGUIUtility.IconContent("d_Animation.Record").image
            );
            wnd.minSize = new Vector2(800, 500);
            wnd.Show();
        }

        [MenuItem("Asaki/Diagnostics/Toggle Command Debug", false, 51)]
        public static void ToggleDebug()
        {
            AsakiCommandDebugger.IsEnabled = !AsakiCommandDebugger.IsEnabled;
            Debug.Log(
                $"[Command Debugger] Debug {(AsakiCommandDebugger.IsEnabled ? "Enabled" : "Disabled")}"
            );
        }

        // --- 窗口状态 ---
        private Vector2 _historyScrollPos;
        private Vector2 _timelineScrollPos;
        private Vector2 _undoStackScrollPos;
        private Vector2 _detailScrollPos;

        private float _leftPanelWidth = 300f;

#pragma warning disable CS0414 // 字段已被赋值，但它的值从未被使用
        private float _bottomPanelHeight = 200f;
#pragma warning restore CS0414 // 字段已被赋值，但它的值从未被使用

        private int _selectedTab = 0; // 0: History, 1: Timeline, 2: Undo/Redo Stack
        private int _selectedCommandIndex = -1;

        private string _searchFilter = "";
        private bool _autoScroll = true;
        private bool _pauseRecording = false;
        private int _maxHistorySize = 500;

        // --- 调试数据 ---
        private readonly List<CommandExecutionRecord> _commandHistory =
            new List<CommandExecutionRecord>();
        private readonly Dictionary<string, CommandStatistics> _commandStats =
            new Dictionary<string, CommandStatistics>();

        // --- 断点 ---
        private readonly HashSet<string> _breakpoints = new HashSet<string>();
        private bool _breakOnError = true;
        private bool _breakOnUndoCommand = false;

        // --- 运行时引用 ---
        private AsakiArchitecture _activeArchitecture;
        private DateTime _sessionStartTime = DateTime.Now;
        private double _sessionTime => (DateTime.Now - _sessionStartTime).TotalSeconds;

        // ==================================================================================
        // 生命周期
        // ==================================================================================

        private void OnEnable()
        {
            AsakiCommandDebugger.SetHook(this);
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            Debug.Log(
                $"[Command Debugger] Window enabled. Debug enabled: {AsakiCommandDebugger.IsEnabled}"
            );

            FindActiveArchitecture();

            if (_activeArchitecture != null)
            {
                Debug.Log(
                    $"[Command Debugger] Architecture found: {_activeArchitecture.GetType().Name}"
                );
            }
            else
            {
                Debug.LogWarning("[Command Debugger] No architecture found!");
            }
        }

        private void OnDisable()
        {
            AsakiCommandDebugger.ClearHook();
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                ClearHistory();
                _sessionStartTime = DateTime.Now;

                // 进入 Play Mode 时自动启用调试
                if (!AsakiCommandDebugger.IsEnabled)
                {
                    AsakiCommandDebugger.IsEnabled = true;
                    Debug.Log("[Command Debugger] Auto-enabled debug in Play Mode");
                }

                FindActiveArchitecture();

                if (_activeArchitecture != null)
                {
                    Debug.Log(
                        $"[Command Debugger] Architecture connected: {_activeArchitecture.GetType().Name}"
                    );
                }
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                _activeArchitecture = null;
            }
            Repaint();
        }

        private void FindActiveArchitecture()
        {
            // 从所有 SceneContext 中查找 Architecture 实例
            // AsakiArchitecture 存储在 SceneContext 的 _pureCSharpServices 中（SerializeReference）
            var sceneContexts = Resources.FindObjectsOfTypeAll<AsakiSceneContext>();
            Debug.Log("FindActiveArchitecture: " + sceneContexts.Length);
            foreach (var context in sceneContexts)
            {
                // 使用反射获取 _pureCSharpServices 字段
                var servicesField = typeof(AsakiSceneContext).GetField(
                    "_pureCSharpServices",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (servicesField?.GetValue(context) is System.Collections.IList services)
                {
                    foreach (var service in services)
                    {
                        if (service is AsakiArchitecture architecture)
                        {
                            Debug.Log(
                                $"[Command Debugger] Found active architecture: {architecture.GetType().Name}"
                            );
                            _activeArchitecture = architecture;
                            return;
                        }
                    }
                }
            }

            // 备选：尝试从全局上下文获取
            if (
                AsakiContext.TryGet<IAsakiArchitecture>(out var archInterface)
                && archInterface is AsakiArchitecture arch
            )
            {
                _activeArchitecture = arch;
            }
        }

        // ==================================================================================
        // IAsakiCommandDebugHook 实现
        // ==================================================================================

        public void OnCommandExecuting(string commandType, bool isAsync, bool isUndoCommand)
        {
            if (_pauseRecording)
                return;

            // 检查断点
            if (_breakpoints.Contains(commandType) || (isUndoCommand && _breakOnUndoCommand))
            {
                Debug.Break();
                Debug.Log($"[Command Debugger] Breakpoint hit: {commandType}");
            }
        }

        public void OnCommandExecuted(CommandExecutionInfo info)
        {
            if (_pauseRecording)
                return;

            // 诊断日志 - 首次捕获时输出
            if (_commandHistory.Count == 0)
            {
                Debug.Log($"[Command Debugger] First command captured: {info.CommandType}");
            }

            var record = new CommandExecutionRecord
            {
                Info = info,
                SessionTime = _sessionTime,
                Index = _commandHistory.Count,
            };

            _commandHistory.Add(record);

            // 更新统计
            if (!_commandStats.TryGetValue(info.CommandType, out var stats))
            {
                stats = new CommandStatistics { CommandType = info.CommandType };
                _commandStats[info.CommandType] = stats;
            }
            stats.TotalExecutions++;
            stats.TotalTimeMs += info.ExecutionTimeMs;
            stats.LastExecutionTime = _sessionTime;

            if (info.HasError)
            {
                stats.ErrorCount++;
                if (_breakOnError)
                {
                    Debug.Break();
                    Debug.LogError(
                        $"[Command Debugger] Error in command: {info.CommandType} - {info.ErrorMessage}"
                    );
                }
            }

            // 限制历史大小
            if (_commandHistory.Count > _maxHistorySize)
            {
                _commandHistory.RemoveAt(0);
            }

            // 自动滚动
            if (_autoScroll)
            {
                _selectedCommandIndex = _commandHistory.Count - 1;
            }

            Repaint();
        }

        public void OnCommandUndo(string commandType)
        {
            if (_pauseRecording)
                return;

            var record = new CommandExecutionRecord
            {
                Info = new CommandExecutionInfo(
                    commandType + " (Undo)",
                    DateTime.Now.Ticks,
                    0,
                    false,
                    null,
                    null,
                    false,
                    true,
                    false,
                    null
                ),
                SessionTime = _sessionTime,
                Index = _commandHistory.Count,
                IsUndoOperation = true,
            };

            _commandHistory.Add(record);
            Repaint();
        }

        public void OnCommandRedo(string commandType)
        {
            if (_pauseRecording)
                return;

            var record = new CommandExecutionRecord
            {
                Info = new CommandExecutionInfo(
                    commandType + " (Redo)",
                    DateTime.Now.Ticks,
                    0,
                    false,
                    null,
                    null,
                    false,
                    true,
                    false,
                    null
                ),
                SessionTime = _sessionTime,
                Index = _commandHistory.Count,
                IsRedoOperation = true,
            };

            _commandHistory.Add(record);
            Repaint();
        }

        // ==================================================================================
        // UI 渲染
        // ==================================================================================

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.BeginHorizontal();

            // 左侧面板
            DrawLeftPanel();

            // 分隔条
            GUILayoutExtensions.Splitter(
                ref _leftPanelWidth,
                200f,
                position.width - 400f,
                false,
                () => Repaint()
            );

            // 右侧主面板
            DrawMainPanel();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 清空按钮
            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                ClearHistory();
            }

            // 暂停/继续
            _pauseRecording = GUILayout.Toggle(
                _pauseRecording,
                _pauseRecording ? "▶ Continue" : "⏸ Pause",
                EditorStyles.toolbarButton,
                GUILayout.Width(70)
            );

            // 自动滚动
            _autoScroll = GUILayout.Toggle(
                _autoScroll,
                "Auto Scroll",
                EditorStyles.toolbarButton,
                GUILayout.Width(80)
            );

            GUILayout.Space(10);

            // 搜索框
            _searchFilter = EditorGUILayout.TextField(
                _searchFilter,
                EditorStyles.toolbarSearchField,
                GUILayout.Width(200)
            );

            GUILayout.FlexibleSpace();

            // 状态指示
            if (Application.isPlaying)
            {
                GUI.color = AsakiCommandDebugger.IsEnabled ? Color.green : Color.gray;
                GUILayout.Label(
                    AsakiCommandDebugger.IsEnabled ? "● DEBUG ENABLED" : "○ DEBUG DISABLED",
                    EditorStyles.boldLabel
                );
                GUI.color = Color.white;
            }
            else
            {
                GUILayout.Label("Not in Play Mode", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(_leftPanelWidth));

            // Tab 选择
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            string[] tabs = { "History", "Timeline", "Undo/Redo" };
            _selectedTab = GUILayout.Toolbar(_selectedTab, tabs, EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();

            // 根据 Tab 绘制内容
            switch (_selectedTab)
            {
                case 0:
                    DrawHistoryTab();
                    break;
                case 1:
                    DrawTimelineTab();
                    break;
                case 2:
                    DrawUndoRedoTab();
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawHistoryTab()
        {
            _historyScrollPos = EditorGUILayout.BeginScrollView(_historyScrollPos);

            var filteredHistory = GetFilteredHistory();

            for (int i = filteredHistory.Count - 1; i >= 0; i--)
            {
                var record = filteredHistory[i];
                DrawHistoryItem(record);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHistoryItem(CommandExecutionRecord record)
        {
            bool isSelected = record.Index == _selectedCommandIndex;

            // 根据状态设置颜色
            Color bgColor = GetRecordBackgroundColor(record);
            GUI.backgroundColor = isSelected ? new Color(0.3f, 0.5f, 0.8f, 0.5f) : bgColor;

            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();

            // 序号
            GUILayout.Label($"#{record.Index}", GUILayout.Width(45));

            // 时间戳
            GUILayout.Label($"{record.SessionTime:F2}s", GUILayout.Width(55));

            // 命令类型
            GUIStyle nameStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
            };

            if (GUILayout.Button(record.Info.CommandType, nameStyle, GUILayout.ExpandWidth(true)))
            {
                _selectedCommandIndex = record.Index;
            }

            // 执行时间
            if (record.Info.ExecutionTimeMs > 0)
            {
                GUI.color = GetTimeColor(record.Info.ExecutionTimeMs);
                GUILayout.Label($"{record.Info.ExecutionTimeMs:F1}ms", GUILayout.Width(60));
                GUI.color = Color.white;
            }

            // 类型标签
            DrawTypeTag(record);

            EditorGUILayout.EndHorizontal();

            // 错误信息
            if (record.Info.HasError)
            {
                GUI.color = Color.red;
                EditorGUILayout.LabelField(
                    $"  ⚠ {record.Info.ErrorMessage}",
                    EditorStyles.miniLabel
                );
                GUI.color = Color.white;
            }

            EditorGUILayout.EndVertical();

            GUI.backgroundColor = Color.white;
        }

        private void DrawTypeTag(CommandExecutionRecord record)
        {
            string tag = null;
            Color color = Color.gray;

            if (record.Info.HasError)
            {
                tag = "ERROR";
                color = Color.red;
            }
            else if (record.IsUndoOperation)
            {
                tag = "UNDO";
                color = new Color(0.8f, 0.4f, 0.2f);
            }
            else if (record.IsRedoOperation)
            {
                tag = "REDO";
                color = new Color(0.4f, 0.8f, 0.2f);
            }
            else if (record.Info.IsAsync)
            {
                tag = "ASYNC";
                color = new Color(0.2f, 0.6f, 1f);
            }
            else if (record.Info.IsUndoCommand)
            {
                tag = "UNDOABLE";
                color = new Color(0.8f, 0.6f, 0.2f);
            }

            if (tag != null)
            {
                GUI.color = color;
                GUILayout.Label(tag, EditorStyles.miniLabel, GUILayout.Width(50));
                GUI.color = Color.white;
            }
        }

        private void DrawTimelineTab()
        {
            _timelineScrollPos = EditorGUILayout.BeginScrollView(_timelineScrollPos);

            if (_commandHistory.Count == 0)
            {
                EditorGUILayout.HelpBox("No commands recorded yet.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            // 时间轴图
            float timelineWidth = _leftPanelWidth - 30;
            float maxTime = (float)_sessionTime + 1f;
            float pixelsPerSecond = timelineWidth / Mathf.Max(maxTime, 10f);

            EditorGUILayout.LabelField("Command Execution Timeline", EditorStyles.boldLabel);

            // 绘制时间刻度
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i <= 10; i++)
            {
                float time = maxTime * i / 10f;
                GUILayout.Label($"{time:F1}s", GUILayout.Width(timelineWidth / 10f));
            }
            EditorGUILayout.EndHorizontal();

            // 按命令类型分组绘制
            var groupedCommands = GetFilteredHistory()
                .GroupBy(r => r.Info.CommandType)
                .OrderBy(g => g.Min(r => r.SessionTime));

            foreach (var group in groupedCommands)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(group.Key, GUILayout.Width(120));

                EditorGUILayout.BeginHorizontal(GUILayout.Width(timelineWidth));

                foreach (var record in group)
                {
                    float x = (float)record.SessionTime * pixelsPerSecond;
                    Color color = GetRecordBackgroundColor(record);

                    GUI.color = color;
                    GUILayout.Space(x - GUILayoutUtility.GetLastRect().xMax);
                    GUILayout.Box("", GUILayout.Width(4), GUILayout.Height(16));
                    GUI.color = Color.white;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawUndoRedoTab()
        {
            _undoStackScrollPos = EditorGUILayout.BeginScrollView(_undoStackScrollPos);

            if (!Application.isPlaying || _activeArchitecture == null)
            {
                EditorGUILayout.HelpBox(
                    "Undo/Redo stack is only available in Play Mode with an active Architecture.",
                    MessageType.Info
                );
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.LabelField("Undo Stack (Top to Bottom)", EditorStyles.boldLabel);

            // 通过反射获取 Undo/Redo 栈
            var undoStack = GetUndoStack();
            var redoStack = GetRedoStack();

            // 获取 CanUndo/CanRedo 属性值
            bool canUndo = GetCanUndo();
            bool canRedo = GetCanRedo();

            // 绘制 Undo 栈
            GUI.color = new Color(0.8f, 0.4f, 0.2f);
            EditorGUILayout.LabelField($"Can Undo: {canUndo} ({undoStack.Count} items)");
            GUI.color = Color.white;

            foreach (var cmd in undoStack)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label(
                    EditorGUIUtility.IconContent("d_Animation.PrevKey"),
                    GUILayout.Width(20)
                );
                EditorGUILayout.LabelField(cmd.GetType().Name);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);

            // 绘制 Redo 栈
            GUI.color = new Color(0.4f, 0.8f, 0.2f);
            EditorGUILayout.LabelField($"Can Redo: {canRedo} ({redoStack.Count} items)");
            GUI.color = Color.white;

            foreach (var cmd in redoStack)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label(
                    EditorGUIUtility.IconContent("d_Animation.NextKey"),
                    GUILayout.Width(20)
                );
                EditorGUILayout.LabelField(cmd.GetType().Name);
                EditorGUILayout.EndHorizontal();
            }

            // 快捷操作按钮
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(!canUndo);
            if (GUILayout.Button("Undo"))
            {
                InvokeUndo();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!canRedo);
            if (GUILayout.Button("Redo"))
            {
                InvokeRedo();
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Clear History"))
            {
                InvokeClearHistory();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        private void DrawMainPanel()
        {
            EditorGUILayout.BeginVertical();

            // 断点设置面板
            DrawBreakpointPanel();

            EditorGUILayout.Space(5);

            // 命令详情
            DrawCommandDetail();

            // 统计面板
            DrawStatisticsPanel();

            EditorGUILayout.EndVertical();
        }

        private void DrawBreakpointPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Breakpoint Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            _breakOnError = EditorGUILayout.Toggle("Break on Error", _breakOnError);
            _breakOnUndoCommand = EditorGUILayout.Toggle("Break on Undo Cmd", _breakOnUndoCommand);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // 调试开关
            EditorGUI.BeginChangeCheck();
            bool debugEnabled = GUILayout.Toggle(
                AsakiCommandDebugger.IsEnabled,
                AsakiCommandDebugger.IsEnabled ? "Disable Debug" : "Enable Debug",
                GUILayout.Width(120),
                GUILayout.Height(30)
            );
            if (EditorGUI.EndChangeCheck())
            {
                AsakiCommandDebugger.IsEnabled = debugEnabled;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawCommandDetail()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("Command Details", EditorStyles.boldLabel);

            _detailScrollPos = EditorGUILayout.BeginScrollView(_detailScrollPos);

            if (_selectedCommandIndex >= 0 && _selectedCommandIndex < _commandHistory.Count)
            {
                var record = _commandHistory[_selectedCommandIndex];
                var info = record.Info;

                EditorGUILayout.LabelField("Command Type:", info.CommandType);
                EditorGUILayout.LabelField("Session Time:", $"{record.SessionTime:F3}s");
                EditorGUILayout.LabelField(
                    "Timestamp:",
                    new DateTime(info.Timestamp).ToString("HH:mm:ss.fff")
                );

                EditorGUILayout.Space(5);

                EditorGUILayout.LabelField("Execution Time:", $"{info.ExecutionTimeMs:F3}ms");
                EditorGUILayout.LabelField("Is Async:", info.IsAsync.ToString());
                EditorGUILayout.LabelField("Is Undo Command:", info.IsUndoCommand.ToString());

                if (info.HasResult)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Result Type:", info.ResultType);
                    EditorGUILayout.LabelField("Result Value:");
                    EditorGUILayout.SelectableLabel(
                        info.ResultValue,
                        EditorStyles.textArea,
                        GUILayout.Height(60)
                    );
                }

                if (info.HasError)
                {
                    EditorGUILayout.Space(5);
                    GUI.color = Color.red;
                    EditorGUILayout.LabelField("Error:", info.ErrorMessage);
                    GUI.color = Color.white;
                }

                EditorGUILayout.Space(10);

                // 断点切换
                EditorGUILayout.BeginHorizontal();
                bool hasBreakpoint = _breakpoints.Contains(info.CommandType);
                bool newBreakpoint = EditorGUILayout.Toggle("Breakpoint", hasBreakpoint);
                if (newBreakpoint != hasBreakpoint)
                {
                    if (newBreakpoint)
                        _breakpoints.Add(info.CommandType);
                    else
                        _breakpoints.Remove(info.CommandType);
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Select a command from the history to view details.",
                    MessageType.Info
                );
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawStatisticsPanel()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(150));
            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);

            var statsScrollPos = EditorGUILayout.BeginScrollView(Vector2.zero);

            var sortedStats = _commandStats
                .Values.OrderByDescending(s => s.TotalExecutions)
                .Take(10);

            foreach (var stat in sortedStats)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(stat.CommandType, GUILayout.Width(200));
                EditorGUILayout.LabelField($"Count: {stat.TotalExecutions}", GUILayout.Width(80));

                if (stat.TotalExecutions > 0)
                {
                    float avgTime = (float)(stat.TotalTimeMs / stat.TotalExecutions);
                    GUI.color = GetTimeColor(avgTime);
                    EditorGUILayout.LabelField($"Avg: {avgTime:F2}ms", GUILayout.Width(80));
                    GUI.color = Color.white;
                }

                if (stat.ErrorCount > 0)
                {
                    GUI.color = Color.red;
                    EditorGUILayout.LabelField($"Errors: {stat.ErrorCount}");
                    GUI.color = Color.white;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ==================================================================================
        // 辅助方法
        // ==================================================================================

        private List<CommandExecutionRecord> GetFilteredHistory()
        {
            if (string.IsNullOrEmpty(_searchFilter))
                return _commandHistory;

            return _commandHistory
                .Where(r =>
                    r.Info.CommandType.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase)
                    >= 0
                )
                .ToList();
        }

        private Color GetRecordBackgroundColor(CommandExecutionRecord record)
        {
            if (record.Info.HasError)
                return new Color(1f, 0.3f, 0.3f, 0.2f);
            if (record.IsUndoOperation)
                return new Color(0.8f, 0.4f, 0.2f, 0.2f);
            if (record.IsRedoOperation)
                return new Color(0.4f, 0.8f, 0.2f, 0.2f);
            if (record.Info.IsUndoCommand)
                return new Color(0.8f, 0.6f, 0.2f, 0.1f);
            return Color.clear;
        }

        private Color GetTimeColor(double ms)
        {
            if (ms < 1)
                return Color.green;
            if (ms < 10)
                return Color.yellow;
            return Color.red;
        }

        private void ClearHistory()
        {
            _commandHistory.Clear();
            _commandStats.Clear();
            _selectedCommandIndex = -1;
            Repaint();
        }

        private List<IAsakiUndoCommand> GetUndoStack()
        {
            var list = new List<IAsakiUndoCommand>();
            if (_activeArchitecture == null)
                return list;

            // 通过反射获取 Undo 栈
            var field = _activeArchitecture
                .GetType()
                .GetField("_asakiUndoRedoStack", BindingFlags.NonPublic | BindingFlags.Instance);

            if (field?.GetValue(_activeArchitecture) is AsakiUndoRedoStack stack)
            {
                var undoField = stack
                    .GetType()
                    .GetField("_undoStack", BindingFlags.NonPublic | BindingFlags.Instance);

                if (undoField?.GetValue(stack) is Stack<IAsakiUndoCommand> undoStack)
                {
                    list.AddRange(undoStack);
                }
            }

            return list;
        }

        private List<IAsakiUndoCommand> GetRedoStack()
        {
            var list = new List<IAsakiUndoCommand>();
            if (_activeArchitecture == null)
                return list;

            // 通过反射获取 Redo 栈
            var field = _activeArchitecture
                .GetType()
                .GetField("_asakiUndoRedoStack", BindingFlags.NonPublic | BindingFlags.Instance);

            if (field?.GetValue(_activeArchitecture) is AsakiUndoRedoStack stack)
            {
                var redoField = stack
                    .GetType()
                    .GetField("_redoStack", BindingFlags.NonPublic | BindingFlags.Instance);

                if (redoField?.GetValue(stack) is Stack<IAsakiUndoCommand> redoStack)
                {
                    list.AddRange(redoStack);
                }
            }

            return list;
        }

        private bool GetCanUndo()
        {
            if (_activeArchitecture == null)
                return false;

            var property = _activeArchitecture
                .GetType()
                .GetProperty("CanUndo", BindingFlags.Public | BindingFlags.Instance);

            return property?.GetValue(_activeArchitecture) is bool value && value;
        }

        private bool GetCanRedo()
        {
            if (_activeArchitecture == null)
                return false;

            var property = _activeArchitecture
                .GetType()
                .GetProperty("CanRedo", BindingFlags.Public | BindingFlags.Instance);

            return property?.GetValue(_activeArchitecture) is bool value && value;
        }

        private void InvokeUndo()
        {
            if (_activeArchitecture == null)
                return;

            var method = _activeArchitecture
                .GetType()
                .GetMethod("Undo", BindingFlags.Public | BindingFlags.Instance);

            method?.Invoke(_activeArchitecture, null);
        }

        private void InvokeRedo()
        {
            if (_activeArchitecture == null)
                return;

            var method = _activeArchitecture
                .GetType()
                .GetMethod("Redo", BindingFlags.Public | BindingFlags.Instance);

            method?.Invoke(_activeArchitecture, null);
        }

        private void InvokeClearHistory()
        {
            if (_activeArchitecture == null)
                return;

            var method = _activeArchitecture
                .GetType()
                .GetMethod("ClearUndoHistory", BindingFlags.Public | BindingFlags.Instance);

            method?.Invoke(_activeArchitecture, null);
        }

        // ==================================================================================
        // 数据结构
        // ==================================================================================

        private class CommandExecutionRecord
        {
            public CommandExecutionInfo Info;
            public double SessionTime;
            public int Index;
            public bool IsUndoOperation;
            public bool IsRedoOperation;
        }

        private class CommandStatistics
        {
            public string CommandType;
            public int TotalExecutions;
            public double TotalTimeMs;
            public int ErrorCount;
            public double LastExecutionTime;
        }
    }
}
