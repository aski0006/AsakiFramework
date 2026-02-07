using System.Collections.Generic;
using Asaki.Core.Configs;
using Asaki.Core.Context.Resolvers;
using Asaki.Editor.Utilities.Tools;
using Asaki.Unity.Bootstrapper;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Asaki.Editor.Bootstrapper
{
    /// <summary>
    /// Asaki Bootstrapper 配置窗口
    /// 提供可视化的框架配置管理界面
    /// </summary>
    public class AsakiBootstrapperWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private AsakiBootstrapper _bootstrapper;
        private AsakiConfig _config;
        private AsakiSceneContext _sceneContext;

        private bool _showBootstrapperSection = true;
        private bool _showConfigSection = true;
        private bool _showSceneContextSection = true;
        private bool _showValidationSection = true;

        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _successStyle;

        [MenuItem("Asaki/Bootstrapper/Bootstrapper Window", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<AsakiBootstrapperWindow>("Asaki Bootstrapper");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshState();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                RefreshState();
                Repaint();
            }
        }

        private void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            RefreshState();
            Repaint();
        }

        private void RefreshState()
        {
            _bootstrapper = FindFirstObjectByType<AsakiBootstrapper>();
            _config = AsakiBootstrapperEditor.GetConfigAsset();
            _sceneContext = FindFirstObjectByType<AsakiSceneContext>();
        }

        private void InitStyles()
        {
            if (_headerStyle != null) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 10, 5)
            };

            _boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(0, 0, 5, 10)
            };

            _warningStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(1f, 0.8f, 0.2f) },
                wordWrap = true
            };

            _successStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.2f, 0.8f, 0.2f) },
                wordWrap = true
            };
        }

        private void OnGUI()
        {
            InitStyles();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            GUILayout.Space(10);
            GUILayout.Label("Asaki Framework Bootstrapper", _headerStyle);
            GUILayout.Space(10);

            // 快速操作按钮
            DrawQuickActions();

            GUILayout.Space(10);

            // 验证状态
            _showValidationSection = EditorGUILayout.Foldout(_showValidationSection, "📋 场景验证状态", true);
            if (_showValidationSection)
            {
                DrawValidationStatus();
            }

            GUILayout.Space(10);

            // Bootstrapper 部分
            _showBootstrapperSection = EditorGUILayout.Foldout(_showBootstrapperSection, "🚀 Bootstrapper", true);
            if (_showBootstrapperSection)
            {
                DrawBootstrapperSection();
            }

            GUILayout.Space(10);

            // Config 部分
            _showConfigSection = EditorGUILayout.Foldout(_showConfigSection, "⚙️ 配置资源 (AsakiConfig)", true);
            if (_showConfigSection)
            {
                DrawConfigSection();
            }

            GUILayout.Space(10);

            // Scene Context 部分
            _showSceneContextSection = EditorGUILayout.Foldout(_showSceneContextSection, "🎯 场景上下文 (SceneContext)", true);
            if (_showSceneContextSection)
            {
                DrawSceneContextSection();
            }

            GUILayout.Space(20);

            // 底部信息
            DrawFooter();

            EditorGUILayout.EndScrollView();
        }

        private void DrawQuickActions()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("快速操作", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("验证场景", GUILayout.Height(30)))
            {
                AsakiBootstrapperEditor.ValidateCurrentScene();
                RefreshState();
            }

            if (GUILayout.Button("自动修复", GUILayout.Height(30)))
            {
                var result = AsakiBootstrapperEditor.ValidateScene(SceneManager.GetActiveScene());
                if (!result.IsValid)
                {
                    AsakiBootstrapperEditor.FixSceneIssues(result);
                    RefreshState();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawValidationStatus()
        {
            EditorGUILayout.BeginVertical(_boxStyle);

            var result = AsakiBootstrapperEditor.ValidateScene(SceneManager.GetActiveScene());

            if (result.IsValid)
            {
                GUILayout.Label("✅ 场景配置正确", _successStyle);
            }
            else
            {
                GUILayout.Label("⚠️ 检测到配置问题：", _warningStyle);
                GUILayout.Label(result.GetErrorMessage(), _warningStyle);
            }

            EditorGUILayout.Space(5);

            // 详细状态
            DrawStatusItem("AsakiConfig 资源", result.HasConfig);
            DrawStatusItem("Bootstrapper", result.HasBootstrapper, !result.HasBootstrapper);
            DrawStatusItem("SceneContext", result.HasSceneContext, false);

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusItem(string label, bool exists, bool isOptional = false)
        {
            EditorGUILayout.BeginHorizontal();

            string icon = exists ? "✅" : (isOptional ? "⚪" : "❌");
            GUILayout.Label($"{icon} {label}", GUILayout.Width(150));

            if (exists)
            {
                GUILayout.Label("已存在", _successStyle);
            }
            else
            {
                GUILayout.Label(isOptional ? "可选（运行时自动创建）" : "缺失", isOptional ? EditorStyles.label : _warningStyle);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBootstrapperSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);

            if (_bootstrapper != null)
            {
                GUILayout.Label("状态：已存在", _successStyle);
                EditorGUILayout.ObjectField("Bootstrapper", _bootstrapper, typeof(AsakiBootstrapper), true);

                GUILayout.Space(10);

                if (GUILayout.Button("选中 Bootstrapper"))
                {
                    Selection.activeGameObject = _bootstrapper.gameObject;
                }

                if (GUILayout.Button("移除 Bootstrapper"))
                {
                    AsakiBootstrapperEditor.RemoveBootstrapperFromScene();
                    RefreshState();
                }
            }
            else
            {
                GUILayout.Label("状态：未添加", _warningStyle);
                GUILayout.Label("Bootstrapper 可以在运行时自动创建，也可以手动添加到场景。", EditorStyles.wordWrappedLabel);

                GUILayout.Space(10);

                if (GUILayout.Button("添加到当前场景", GUILayout.Height(30)))
                {
                    AsakiBootstrapperEditor.AddBootstrapperToScene();
                    RefreshState();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawConfigSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);

            if (_config != null)
            {
                GUILayout.Label("状态：已存在", _successStyle);
                EditorGUILayout.ObjectField("Config Asset", _config, typeof(AsakiConfig), false);

                GUILayout.Space(10);

                if (GUILayout.Button("选中 Config"))
                {
                    Selection.activeObject = _config;
                }

                if (GUILayout.Button("创建新的 Config"))
                {
                    AsakiBootstrapperEditor.CreateConfigAsset();
                    RefreshState();
                }
            }
            else
            {
                GUILayout.Label("状态：未创建", _warningStyle);
                GUILayout.Label("AsakiConfig 是必需的配置资源。", EditorStyles.wordWrappedLabel);

                GUILayout.Space(10);

                if (GUILayout.Button("创建 Config Asset", GUILayout.Height(30)))
                {
                    AsakiBootstrapperEditor.CreateConfigAsset();
                    RefreshState();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSceneContextSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);

            if (_sceneContext != null)
            {
                GUILayout.Label("状态：已存在", _successStyle);
                EditorGUILayout.ObjectField("Scene Context", _sceneContext, typeof(AsakiSceneContext), true);

                GUILayout.Space(10);

                if (GUILayout.Button("选中 Scene Context"))
                {
                    Selection.activeGameObject = _sceneContext.gameObject;
                }

                if (GUILayout.Button("移除 Scene Context"))
                {
                    Undo.DestroyObjectImmediate(_sceneContext.gameObject);
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                    RefreshState();
                }
            }
            else
            {
                GUILayout.Label("状态：未添加（可选）", EditorStyles.label);
                GUILayout.Label("SceneContext 用于管理场景级别的服务。如果不需要场景特定服务，可以省略。", EditorStyles.wordWrappedLabel);

                GUILayout.Space(10);

                if (GUILayout.Button("创建 Scene Context", GUILayout.Height(30)))
                {
                    AsakiSceneContextCreator.CreateSceneContext();
                    RefreshState();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginVertical(_boxStyle);

            GUILayout.Label("关于场景切换", EditorStyles.boldLabel);
            GUILayout.Label(
                "Bootstrapper 在首次创建时会调用 DontDestroyOnLoad()，因此它会一直存在于整个游戏生命周期中。\n\n" +
                "当你在 A 场景添加了 Bootstrapper，切换到 B 场景开发时：\n" +
                "• 如果 A 场景是首场景：Bootstrapper 会在场景切换后仍然存在\n" +
                "• 如果直接从 B 场景开始 PlayMode：Bootstrapper 会自动创建\n\n" +
                "建议：在首场景（如 Boot、Splash、MainMenu）添加 Bootstrapper，其他子场景不需要添加。",
                EditorStyles.wordWrappedLabel
            );

            EditorGUILayout.EndVertical();
        }
    }
}
