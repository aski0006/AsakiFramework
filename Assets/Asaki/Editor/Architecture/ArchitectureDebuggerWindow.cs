using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Asaki.Core.Architecture;
using Asaki.Core.Context;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Architecture
{
    /// <summary>
    /// Architecture调试窗口，用于在PlayMode下查看已注册的Architecture及其System和Model
    /// </summary>
    public class ArchitectureDebuggerWindow : EditorWindow
    {
        private const double RefreshInterval = 0.5d;
        private static readonly int[] RetryDelays = { 100, 300, 500 };

        private ArchitectureRegister _architectureRegister;
        private bool _isRetrying;
        private int _retryCount;
        private string _retryMessage;

        private List<ArchitectureInfo> _architectureInfos;
        private int _selectedIndex = -1;
        private Vector2 _leftScrollPos;
        private Vector2 _rightScrollPos;

        private double _lastUpdateTime;
        private bool _needsRefresh;

        private GUIStyle _headerStyle;
        private GUIStyle _itemStyle;
        private GUIStyle _selectedStyle;
        private GUIStyle _countBadgeStyle;
        private bool _stylesInitialized;

        [MenuItem("Asaki/Diagnostics/Architecture Debugger", false, 51)]
        public static void OpenWindow()
        {
            ArchitectureDebuggerWindow window = GetWindow<ArchitectureDebuggerWindow>("Architecture Debugger");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            _architectureInfos = new List<ArchitectureInfo>();
            _needsRefresh = true;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _architectureRegister = null;
                _architectureInfos.Clear();
                _selectedIndex = -1;
                _needsRefresh = true;
                _isRetrying = false;
                _retryCount = 0;
                _retryMessage = null;
                StartRetryProcess();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                _architectureRegister = null;
                _architectureInfos.Clear();
                _selectedIndex = -1;
            }
        }

        private async void StartRetryProcess()
        {
            if (!Application.isPlaying)
                return;

            _isRetrying = true;
            _retryMessage = "正在获取ArchitectureRegister...";

            for (int i = 0; i < RetryDelays.Length; i++)
            {
                if (!Application.isPlaying)
                {
                    _isRetrying = false;
                    return;
                }

                if (AsakiContext.TryGet(out ArchitectureRegister register))
                {
                    _architectureRegister = register;
                    _isRetrying = false;
                    _retryMessage = null;
                    _retryCount = i + 1;
                    RefreshArchitectureInfos();
                    Repaint();
                    return;
                }

                _retryCount = i + 1;
                _retryMessage = $"尝试 {_retryCount}/3 获取ArchitectureRegister...";
                Repaint();

                await Task.Delay(RetryDelays[i]);
            }

            _isRetrying = false;
            _retryMessage = "无法获取ArchitectureRegister，请确保框架已正确初始化";
            Repaint();
        }

        private void Update()
        {
            if (!Application.isPlaying || _architectureRegister == null)
                return;

            if (EditorApplication.timeSinceStartup - _lastUpdateTime > RefreshInterval)
            {
                _lastUpdateTime = EditorApplication.timeSinceStartup;
                _needsRefresh = true;
                Repaint();
            }
        }

        private void RefreshArchitectureInfos()
        {
            _architectureInfos.Clear();

            if (_architectureRegister == null)
                return;

            var architectures = _architectureRegister.GetArchitecturesForEditor();
            if (architectures == null)
                return;

            foreach (var kvp in architectures)
            {
                var arch = kvp.Value as AsakiArchitecture;
                if (arch == null)
                    continue;

                var info = new ArchitectureInfo
                {
                    Type = kvp.Key,
                    Instance = arch,
                    Models = arch.GetModelsForEditor(),
                    Systems = arch.GetSystemsForEditor()
                };
                _architectureInfos.Add(info);
            }
        }

        private void OnGUI()
        {
            InitializeStyles();

            if (!Application.isPlaying)
            {
                DrawNonPlayModeUI();
                return;
            }

            if (_isRetrying)
            {
                DrawRetryUI();
                return;
            }

            if (_architectureRegister == null)
            {
                DrawErrorUI();
                return;
            }

            if (_needsRefresh)
            {
                RefreshArchitectureInfos();
                _needsRefresh = false;
            }

            DrawMainUI();
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized)
                return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                margin = new RectOffset(5, 5, 5, 5)
            };

            _itemStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(2, 2, 2, 2)
            };

            _selectedStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(2, 2, 2, 2)
            };
            _selectedStyle.normal.background = MakeTex(2, 2, new Color(0.24f, 0.48f, 0.72f, 0.3f));

            _countBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                normal = { textColor = Color.white }
            };

            _stylesInitialized = true;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void DrawNonPlayModeUI()
        {
            EditorGUILayout.HelpBox(
                "Architecture调试器仅在PlayMode下可用。\n请进入播放模式查看已注册的Architecture。",
                MessageType.Info
            );

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(
                "等待进入PlayMode...",
                EditorStyles.centeredGreyMiniLabel
            );
        }

        private void DrawRetryUI()
        {
            EditorGUILayout.HelpBox(
                _retryMessage ?? "正在初始化...",
                MessageType.Info
            );

            Rect rect = GUILayoutUtility.GetRect(200, 20);
            float progress = _retryCount / 3f;
            EditorGUI.ProgressBar(rect, progress, $"{_retryCount}/3");
        }

        private void DrawErrorUI()
        {
            EditorGUILayout.HelpBox(
                _retryMessage ?? "无法获取ArchitectureRegister",
                MessageType.Error
            );

            if (GUILayout.Button("重试", GUILayout.Height(30)))
            {
                StartRetryProcess();
            }
        }

        private void DrawMainUI()
        {
            EditorGUILayout.BeginHorizontal();

            DrawLeftPanel();
            DrawRightPanel();

            EditorGUILayout.EndHorizontal();

            DrawStatusBar();
        }

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Architectures", _headerStyle, GUILayout.Height(24));
            GUILayout.FlexibleSpace();
            DrawCountBadge(_architectureInfos.Count, new Color(0.2f, 0.6f, 0.3f));
            EditorGUILayout.EndHorizontal();

            _leftScrollPos = EditorGUILayout.BeginScrollView(_leftScrollPos, EditorStyles.helpBox);

            for (int i = 0; i < _architectureInfos.Count; i++)
            {
                DrawArchitectureItem(i);
            }

            if (_architectureInfos.Count == 0)
            {
                EditorGUILayout.LabelField(
                    "暂无已注册的Architecture",
                    EditorStyles.centeredGreyMiniLabel,
                    GUILayout.Height(100)
                );
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawArchitectureItem(int index)
        {
            ArchitectureInfo info = _architectureInfos[index];
            bool isSelected = _selectedIndex == index;

            GUIStyle style = isSelected ? _selectedStyle : _itemStyle;

            EditorGUILayout.BeginHorizontal(style);

            if (GUILayout.Button(info.Type.Name, EditorStyles.label, GUILayout.ExpandWidth(true)))
            {
                _selectedIndex = index;
            }

            if (isSelected)
            {
                GUILayout.Label("▶", EditorStyles.miniLabel, GUILayout.Width(15));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Details", _headerStyle, GUILayout.Height(24));
            EditorGUILayout.EndHorizontal();

            _rightScrollPos = EditorGUILayout.BeginScrollView(_rightScrollPos, EditorStyles.helpBox);

            if (_selectedIndex >= 0 && _selectedIndex < _architectureInfos.Count)
            {
                DrawArchitectureDetails(_architectureInfos[_selectedIndex]);
            }
            else
            {
                EditorGUILayout.LabelField(
                    "请从左侧选择一个Architecture",
                    EditorStyles.centeredGreyMiniLabel,
                    GUILayout.Height(200)
                );
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawArchitectureDetails(ArchitectureInfo info)
        {
            DrawSectionHeader("Architecture Info", new Color(0.3f, 0.5f, 0.7f));
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Type", info.Type.Name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Full Name", info.Type.FullName);
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            DrawSectionHeader("Models", new Color(0.2f, 0.6f, 0.3f));
            DrawDictionarySection(info.Models, "Model");

            GUILayout.Space(10);

            DrawSectionHeader("Systems", new Color(0.6f, 0.4f, 0.2f));
            DrawDictionarySection(info.Systems, "System");
        }

        private void DrawSectionHeader(string title, Color color)
        {
            EditorGUILayout.BeginHorizontal();

            Rect rect = GUILayoutUtility.GetRect(4, 20);
            EditorGUI.DrawRect(rect, color);

            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDictionarySection<TKey, TValue>(
            IReadOnlyDictionary<TKey, TValue> dictionary,
            string itemType
        )
            where TKey : Type
            where TValue : class
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (dictionary == null || dictionary.Count == 0)
            {
                EditorGUILayout.LabelField($"暂无已注册的{itemType}", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var kvp in dictionary)
                {
                    EditorGUILayout.BeginHorizontal();

                    GUILayout.Label("•", GUILayout.Width(15));
                    GUILayout.Label(kvp.Key.Name, EditorStyles.boldLabel);

                    GUILayout.FlexibleSpace();

                    if (kvp.Value != null)
                    {
                        Type implType = kvp.Value.GetType();
                        if (implType != kvp.Key)
                        {
                            GUILayout.Label($"({implType.Name})", EditorStyles.miniLabel);
                        }
                        else
                        {
                            GUILayout.Label("✓", EditorStyles.miniLabel);
                        }
                    }
                    else
                    {
                        GUILayout.Label("null", EditorStyles.miniLabel);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCountBadge(int count, Color color)
        {
            Rect rect = GUILayoutUtility.GetRect(30, 18, GUILayout.Width(30));
            EditorGUI.DrawRect(rect, color);
            GUI.Label(rect, count.ToString(), _countBadgeStyle);
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label(
                $"Architectures: {_architectureInfos.Count}",
                EditorStyles.miniLabel
            );

            GUILayout.FlexibleSpace();

            GUILayout.Label(
                $"自动刷新间隔: {RefreshInterval}s",
                EditorStyles.miniLabel
            );

            EditorGUILayout.EndHorizontal();
        }

        private class ArchitectureInfo
        {
            public Type Type;
            public AsakiArchitecture Instance;
            public IReadOnlyDictionary<Type, IAsakiModel> Models;
            public IReadOnlyDictionary<Type, IAsakiSystem> Systems;
        }
    }
}
