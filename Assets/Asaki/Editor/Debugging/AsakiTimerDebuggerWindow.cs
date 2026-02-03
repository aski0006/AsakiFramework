// File: Asaki/Editor/Debugging/AsakiTimerDebuggerWindow.cs

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using Asaki.Core.Context;
using Asaki.Core.Time;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Debugging
{
    /// <summary>
    /// [Asaki Editor] Timer 服务调试窗口
    /// 用于可视化管理和调试所有定时器实例
    /// </summary>
    public class AsakiTimerDebuggerWindow : EditorWindow
    {
        private IAsakiTimerService _timerService;
        private Vector2 _scrollPos;
        private List<AsakiTimerDebugInfo> _timerInfos = new List<AsakiTimerDebugInfo>();
        private Dictionary<string, bool> _tagFoldouts = new Dictionary<string, bool>();
        private float _lastRefreshTime;
        private const float RefreshInterval = 0.1f;

        // 编辑器配置
        private bool _autoRefresh = true;
        private bool _showCompletedTimers = false;
        private bool _showPausedTimers = true;
        private string _searchFilter = "";

        [MenuItem("Asaki/Diagnostics/Timer Debugger", false, 58)]
        public static void ShowWindow()
        {
            var window = GetWindow<AsakiTimerDebuggerWindow>("Timer Debugger");
            window.minSize = new Vector2(800, 500);
            window.Show();
        }

        private void OnEnable()
        {
            _lastRefreshTime = Time.realtimeSinceStartup;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (_autoRefresh && Time.realtimeSinceStartup - _lastRefreshTime > RefreshInterval)
            {
                RefreshTimerData();
                _lastRefreshTime = Time.realtimeSinceStartup;
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawGlobalControls();
            DrawTimerList();
        }

        #region 工具栏

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshTimerData();
            }

            _autoRefresh = GUILayout.Toggle(
                _autoRefresh,
                "Auto Refresh",
                EditorStyles.toolbarButton,
                GUILayout.Width(80)
            );

            GUILayout.Space(10);

            EditorGUI.BeginChangeCheck();
            _searchFilter = EditorGUILayout.TextField(
                _searchFilter,
                EditorStyles.toolbarSearchField,
                GUILayout.Width(150)
            );
            if (EditorGUI.EndChangeCheck())
            {
                RefreshTimerData();
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField(
                $"Active: {_timerInfos.Count}",
                EditorStyles.toolbar,
                GUILayout.Width(80)
            );

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 全局控制

        private void DrawGlobalControls()
        {
            EditorGUILayout.BeginVertical("box");

            // 服务状态
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                "Timer Service Status:",
                EditorStyles.boldLabel,
                GUILayout.Width(130)
            );

            if (_timerService == null)
            {
                if (AsakiContext.TryGet(out _timerService))
                {
                    EditorGUILayout.LabelField(
                        "✅ Connected",
                        new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } }
                    );
                }
                else
                {
                    EditorGUILayout.LabelField(
                        "❌ Not Available",
                        new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } }
                    );
                }
            }
            else
            {
                EditorGUILayout.LabelField(
                    "✅ Connected",
                    new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } }
                );
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 全局控制按钮
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
            if (GUILayout.Button("Pause All", GUILayout.Width(80)))
            {
                _timerService?.PauseAll();
            }
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("Resume All", GUILayout.Width(80)))
            {
                _timerService?.ResumeAll();
            }
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = new Color(0.9f, 0.5f, 0.2f);
            if (GUILayout.Button("Cancel All", GUILayout.Width(80)))
            {
                if (
                    EditorUtility.DisplayDialog("Confirm", "Cancel all active timers?", "Yes", "No")
                )
                {
                    _timerService?.CancelAll();
                }
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(20);

            // 时间缩放
            EditorGUILayout.LabelField("Time Scale:", GUILayout.Width(70));
            float currentScale = _timerService?.GetGlobalTimeScale() ?? 1f;
            float newScale = EditorGUILayout.Slider(currentScale, 0f, 5f, GUILayout.Width(150));
            if (Math.Abs(newScale - currentScale) > 0.001f)
            {
                _timerService?.SetGlobalTimeScale(newScale);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 过滤器
            EditorGUILayout.BeginHorizontal();
            _showPausedTimers = GUILayout.Toggle(
                _showPausedTimers,
                "Show Paused",
                GUILayout.Width(90)
            );
            _showCompletedTimers = GUILayout.Toggle(
                _showCompletedTimers,
                "Show Completed",
                GUILayout.Width(110)
            );
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region 定时器列表

        private void DrawTimerList()
        {
            if (_timerService == null)
            {
                EditorGUILayout.HelpBox(
                    "Timer Service not available. Make sure the game is running and Bootstrapper has initialized.",
                    MessageType.Warning
                );
                return;
            }

            // 按标签分组
            var groupedTimers = _timerInfos
                .Where(ShouldShowTimer)
                .GroupBy(t => string.IsNullOrEmpty(t.Tag) ? "[No Tag]" : t.Tag)
                .OrderBy(g => g.Key);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            foreach (var group in groupedTimers)
            {
                DrawTagGroup(group.Key, group.ToList());
            }

            if (!groupedTimers.Any())
            {
                EditorGUILayout.HelpBox(
                    "No active timers matching the current filters.",
                    MessageType.Info
                );
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTagGroup(string tag, List<AsakiTimerDebugInfo> timers)
        {
            if (!_tagFoldouts.ContainsKey(tag))
            {
                _tagFoldouts[tag] = true;
            }

            EditorGUILayout.BeginVertical("box");

            // 标签头部
            EditorGUILayout.BeginHorizontal();

            _tagFoldouts[tag] = EditorGUILayout.Foldout(
                _tagFoldouts[tag],
                $"{tag} ({timers.Count})",
                true,
                EditorStyles.boldLabel
            );

            GUILayout.FlexibleSpace();

            // 标签操作按钮
            if (GUILayout.Button("Pause Tag", EditorStyles.miniButton, GUILayout.Width(70)))
            {
                _timerService.PauseAllByTag(tag == "[No Tag]" ? "" : tag, true);
            }

            if (GUILayout.Button("Resume Tag", EditorStyles.miniButton, GUILayout.Width(70)))
            {
                _timerService.PauseAllByTag(tag == "[No Tag]" ? "" : tag, false);
            }

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Cancel Tag", EditorStyles.miniButton, GUILayout.Width(70)))
            {
                _timerService.CancelAllByTag(tag == "[No Tag]" ? "" : tag);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // 定时器列表
            if (_tagFoldouts[tag])
            {
                foreach (var timer in timers)
                {
                    DrawTimerItem(timer);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTimerItem(AsakiTimerDebugInfo timer)
        {
            EditorGUILayout.BeginHorizontal("box");

            // 状态指示器
            Rect statusRect = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12));
            Color statusColor = timer.IsPaused
                ? Color.yellow
                : (timer.IsCompleted ? Color.green : Color.cyan);
            EditorGUI.DrawRect(statusRect, statusColor);

            GUILayout.Space(5);

            // ID 和基本信息
            EditorGUILayout.LabelField($"ID:{timer.Id}", GUILayout.Width(60));
            EditorGUILayout.LabelField($"v{timer.Version}", GUILayout.Width(50));

            // 进度条
            Rect progressRect = GUILayoutUtility.GetRect(100, 18, GUILayout.Width(100));
            EditorGUI.ProgressBar(progressRect, timer.Progress, $"{timer.Progress * 100:F0}%");

            // 时间信息
            EditorGUILayout.LabelField(
                $"{timer.Elapsed:F2}s / {timer.Duration:F2}s",
                GUILayout.Width(100)
            );

            // 属性标签
            if (timer.IsLooped)
            {
                EditorGUILayout.LabelField(
                    "[Loop]",
                    new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.green } },
                    GUILayout.Width(40)
                );
            }
            if (timer.UseUnscaledTime)
            {
                EditorGUILayout.LabelField(
                    "[Unscaled]",
                    new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.cyan } },
                    GUILayout.Width(55)
                );
            }

            GUILayout.FlexibleSpace();

            // 回调信息
            string callbackInfo = "";
            if (timer.HasCompleteCallback)
                callbackInfo += "C";
            if (timer.HasUpdateCallback)
                callbackInfo += "U";
            if (!string.IsNullOrEmpty(callbackInfo))
            {
                EditorGUILayout.LabelField($"[{callbackInfo}]", GUILayout.Width(30));
            }

            // 操作按钮
            if (timer.IsPaused)
            {
                if (GUILayout.Button("▶", GUILayout.Width(25)))
                {
                    _timerService.Pause(new AsakiTimerHandle(timer.Id, timer.Version), false);
                }
            }
            else
            {
                if (GUILayout.Button("⏸", GUILayout.Width(25)))
                {
                    _timerService.Pause(new AsakiTimerHandle(timer.Id, timer.Version), true);
                }
            }

            if (GUILayout.Button("⏩", GUILayout.Width(25)))
            {
                _timerService.ForceComplete(new AsakiTimerHandle(timer.Id, timer.Version));
            }

            GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
            if (GUILayout.Button("✕", GUILayout.Width(25)))
            {
                _timerService.Cancel(new AsakiTimerHandle(timer.Id, timer.Version));
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 辅助方法

        private void RefreshTimerData()
        {
            if (_timerService == null)
            {
                AsakiContext.TryGet(out _timerService);
            }

            _timerService?.GetAllTimerDebugInfos()?.Clear();
            _timerInfos = _timerService?.GetAllTimerDebugInfos() ?? new List<AsakiTimerDebugInfo>();
        }

        private bool ShouldShowTimer(AsakiTimerDebugInfo timer)
        {
            // 搜索过滤
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                string search = _searchFilter.ToLower();
                if (
                    !timer.Tag.ToLower().Contains(search)
                    && !timer.Id.ToString().Contains(search)
                    && !timer.CallbackTargetType.ToLower().Contains(search)
                )
                {
                    return false;
                }
            }

            // 暂停过滤
            if (!_showPausedTimers && timer.IsPaused)
                return false;

            // 完成过滤
            if (!_showCompletedTimers && timer.IsCompleted)
                return false;

            return true;
        }

        #endregion
    }
}

#endif
