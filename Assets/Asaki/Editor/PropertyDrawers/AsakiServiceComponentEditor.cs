// File: Assets/Asaki/Editor/Component/AsakiServiceComponentEditor.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Asaki.Core.Context.Resolvers;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.PropertyDrawers
{
    /// <summary>
    /// 为所有 Asaki 服务 MonoBehaviour 自定义 Inspector 显示
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    [CanEditMultipleObjects]
    public class AsakiServiceComponentEditor : UnityEditor.Editor
    {
        // ========================================================================
        // 缓存数据
        // ========================================================================

        private bool _isService;
        private bool _isGlobalService;
        private bool _isSceneService;
        private Type[] _dependencies;
        private bool _showDependencies;

        // 静态缓存：避免每帧重复反射
        private static readonly Dictionary<Type, Type[]> _dependencyCache =
            new Dictionary<Type, Type[]>();

        // ========================================================================
        // 生命周期
        // ========================================================================

        private void OnEnable()
        {
            AnalyzeTarget();
        }

        public override void OnInspectorGUI()
        {
            // 如果是服务，绘制自定义 Header
            if (_isService)
            {
                DrawServiceHeader();
                EditorGUILayout.Space(5);
            }

            // 绘制默认 Inspector
            DrawDefaultInspector();

            // 如果有依赖信息，绘制依赖列表
            if (_isService && _dependencies != null && _dependencies.Length > 0)
            {
                EditorGUILayout.Space(10);
                DrawDependencyFoldout();
            }
        }

        // ========================================================================
        // 分析逻辑
        // ========================================================================

        /// <summary>
        /// 分析目标组件
        /// </summary>
        private void AnalyzeTarget()
        {
            if (target == null)
                return;

            Type targetType = target.GetType();

            // 检查是否为服务
            _isGlobalService = typeof(IAsakiGlobalService).IsAssignableFrom(targetType);
            _isSceneService = typeof(IAsakiSceneService).IsAssignableFrom(targetType);
            _isService = _isGlobalService || _isSceneService;

            if (!_isService)
                return;

            // 获取依赖信息（使用缓存）
            _dependencies = GetOrCreateDependencyCache(targetType);
        }

        /// <summary>
        /// 获取或创建依赖缓存
        /// </summary>
        private Type[] GetOrCreateDependencyCache(Type targetType)
        {
            if (_dependencyCache.TryGetValue(targetType, out var cached))
                return cached;

            var dependencies = CollectDependencies(targetType);
            _dependencyCache[targetType] = dependencies;
            return dependencies;
        }

        /// <summary>
        /// 收集依赖：扫描 [AsakiInject] 方法的参数
        /// </summary>
        private Type[] CollectDependencies(Type targetType)
        {
            var dependencies = new List<Type>();

            // 查找所有标记了 [AsakiInject] 的方法
            var injectMethods = targetType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttribute<AsakiInjectAttribute>() != null);

            foreach (MethodInfo method in injectMethods)
            {
                // 获取方法参数类型
                var parameters = method.GetParameters();
                dependencies.AddRange(parameters.Select(p => p.ParameterType));
            }

            // 去重并排序
            return dependencies.Distinct().OrderBy(t => t.Name).ToArray();
        }

        // ========================================================================
        // UI 绘制
        // ========================================================================

        /// <summary>
        /// 绘制服务 Header
        /// </summary>
        private void DrawServiceHeader()
        {
            // 确定颜色方案
            Color backgroundColor;
            Color textColor;
            string icon;
            string label;

            if (_isGlobalService)
            {
                backgroundColor = new Color(0.2f, 0.4f, 0.8f, 0.15f); // 蓝色
                textColor = new Color(0.3f, 0.6f, 1f);
                icon = "🌐";
                label = "Global Service";
            }
            else
            {
                backgroundColor = new Color(0.8f, 0.5f, 0.2f, 0.15f); // 橙色
                textColor = new Color(1f, 0.7f, 0.3f);
                icon = "📍";
                label = "Scene Service";
            }

            // 绘制背景框
            Rect headerRect = EditorGUILayout.GetControlRect(false, 28);
            EditorGUI.DrawRect(headerRect, backgroundColor);

            // 绘制边框
            Handles.BeginGUI();
            Handles.color = textColor * 0.5f;
            Handles.DrawLine(
                new Vector3(headerRect.x, headerRect.y, 0),
                new Vector3(headerRect.xMax, headerRect.y, 0)
            );
            Handles.DrawLine(
                new Vector3(headerRect.x, headerRect.yMax, 0),
                new Vector3(headerRect.xMax, headerRect.yMax, 0)
            );
            Handles.EndGUI();

            // 绘制内容
            Rect contentRect = new Rect(
                headerRect.x + 8,
                headerRect.y + 4,
                headerRect.width - 16,
                20
            );

            // 左侧：图标 + 标签
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = textColor },
                alignment = TextAnchor.MiddleLeft,
            };

            Rect leftRect = new Rect(
                contentRect.x,
                contentRect.y,
                contentRect.width * 0.7f,
                contentRect.height
            );
            GUI.Label(leftRect, $"{icon} {label}", labelStyle);

            // 右侧：依赖图标（如果有）
            if (_dependencies != null && _dependencies.Length > 0)
            {
                Rect rightRect = new Rect(
                    contentRect.xMax - 120,
                    contentRect.y,
                    120,
                    contentRect.height
                );

                GUIStyle dependencyStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 10,
                    normal = { textColor = textColor * 0.8f },
                    alignment = TextAnchor.MiddleRight,
                };

                string dependencyText = $"▼ {_dependencies.Length} Dependencies";
                if (GUI.Button(rightRect, dependencyText, dependencyStyle))
                {
                    _showDependencies = !_showDependencies;
                }
            }
        }

        /// <summary>
        /// 绘制依赖列表折叠面板
        /// </summary>
        private void DrawDependencyFoldout()
        {
            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.3f, 0.6f, 1f) },
            };

            _showDependencies = EditorGUILayout.Foldout(
                _showDependencies,
                $"Service Dependencies ({_dependencies.Length})",
                true,
                foldoutStyle
            );

            if (!_showDependencies)
                return;

            EditorGUI.indentLevel++;

            // 显示依赖来源信息
            Type targetType = target.GetType();
            MethodInfo injectMethod = targetType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.GetCustomAttribute<AsakiInjectAttribute>() != null);

            if (injectMethod != null)
            {
                EditorGUILayout.LabelField(
                    $"From Method: {injectMethod.Name}()",
                    EditorStyles.miniLabel
                );
                EditorGUILayout.Space(3);
            }

            // 绘制依赖列表
            foreach (Type depType in _dependencies)
            {
                DrawDependencyItem(depType);
            }

            EditorGUI.indentLevel--;

            // 全部验证按钮
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                if (GUILayout.Button("Validate All Dependencies"))
                {
                    ValidateAllDependencies();
                }
            }
        }

        /// <summary>
        /// 绘制单个依赖项
        /// </summary>
        private void DrawDependencyItem(Type depType)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // 判断是否为 Asaki 服务
            bool isService = typeof(IAsakiService).IsAssignableFrom(depType);

            // 图标
            string icon = isService ? "🔗" : "⚙️";
            GUILayout.Label(icon, GUILayout.Width(20));

            // 依赖类型名称
            GUIStyle typeStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = isService ? Color.white : new Color(1f, 0.8f, 0.4f) },
            };

            EditorGUILayout.LabelField(depType.Name, typeStyle);

            // 非服务类型警告
            if (!isService)
            {
                GUILayout.Label("⚠️", GUILayout.Width(20));
            }

            // 运行时验证按钮
            if (Application.isPlaying && GUILayout.Button("Check", GUILayout.Width(50)))
            {
                ValidateDependency(depType);
            }

            EditorGUILayout.EndHorizontal();
        }

        // ========================================================================
        // 运行时验证
        // ========================================================================

        /// <summary>
        /// 验证单个依赖
        /// </summary>
        private void ValidateDependency(Type depType)
        {
            // 只验证服务类型
            if (!typeof(IAsakiService).IsAssignableFrom(depType))
            {
                Debug.LogWarning(
                    $"⚠️ [{depType.Name}] is not an IAsakiService, skipping validation."
                );
                return;
            }

            try
            {
                // 检查全局 Context
                bool resolvedGlobal = TryResolveFromGlobalContext(depType);

                // 检查场景 Context
                bool resolvedScene = false;
                if (_isSceneService)
                {
#if UNITY_2022_3_OR_NEWER
                    AsakiSceneContext sceneContext = FindFirstObjectByType<AsakiSceneContext>();
#else
                    var sceneContext = FindObjectOfType<AsakiSceneContext>();
#endif
                    if (sceneContext)
                    {
                        resolvedScene = TryResolveFromSceneContext(depType, sceneContext);
                    }
                }

                if (resolvedGlobal || resolvedScene)
                {
                    string source = resolvedGlobal ? "Global" : "Scene";
                    Debug.Log(
                        $"✅ <color=green>[Dependency Check]</color> {depType.Name} found in {source} Context."
                    );
                }
                else
                {
                    Debug.LogWarning(
                        $"❌ <color=yellow>[Dependency Check]</color> {depType.Name} NOT found in any Context!"
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"❌ [Dependency Check] Failed to validate {depType.Name}:  {ex.Message}"
                );
            }
        }

        /// <summary>
        /// 尝试从全局 Context 解析
        /// </summary>
        private bool TryResolveFromGlobalContext(Type depType)
        {
            MethodInfo tryGetMethod = typeof(AsakiContext).GetMethod(
                "TryGet",
                BindingFlags.Public | BindingFlags.Static
            );
            if (tryGetMethod == null)
                return false;

            MethodInfo genericMethod = tryGetMethod.MakeGenericMethod(depType);
            object[] parameters = new object[] { null };

            return (bool)genericMethod.Invoke(null, parameters);
        }

        /// <summary>
        /// 尝试从场景 Context 解析
        /// </summary>
        private bool TryResolveFromSceneContext(Type depType, AsakiSceneContext context)
        {
            MethodInfo tryGetMethod = typeof(AsakiSceneContext).GetMethod(
                "TryGet",
                BindingFlags.Public | BindingFlags.Instance
            );
            if (tryGetMethod == null)
                return false;

            MethodInfo genericMethod = tryGetMethod.MakeGenericMethod(depType);
            object[] parameters = new object[] { null };

            return (bool)genericMethod.Invoke(context, parameters);
        }

        /// <summary>
        /// 验证所有依赖
        /// </summary>
        private void ValidateAllDependencies()
        {
            if (_dependencies == null || _dependencies.Length == 0)
                return;

            int resolved = 0;
            int missing = 0;
            int skipped = 0;

            foreach (Type depType in _dependencies)
            {
                // 只验证服务类型
                if (!typeof(IAsakiService).IsAssignableFrom(depType))
                {
                    skipped++;
                    continue;
                }

                try
                {
                    bool isResolved = TryResolveFromGlobalContext(depType);

                    if (_isSceneService && !isResolved)
                    {
#if UNITY_2022_3_OR_NEWER
                        AsakiSceneContext sceneContext = FindFirstObjectByType<AsakiSceneContext>();
#else
                        var sceneContext = FindObjectOfType<AsakiSceneContext>();
#endif
                        if (sceneContext)
                        {
                            isResolved = TryResolveFromSceneContext(depType, sceneContext);
                        }
                    }

                    if (isResolved)
                        resolved++;
                    else
                        missing++;
                }
                catch
                {
                    missing++;
                }
            }

            string report = $"[{target.GetType().Name}] Validation Result:\n";
            report += $"✅ Resolved: {resolved}\n";

            if (missing > 0)
                report += $"❌ Missing: {missing}\n";

            if (skipped > 0)
                report += $"⚠️ Skipped (non-service): {skipped}";

            if (missing == 0)
            {
                Debug.Log($"✅ <color=green>{report}</color>");
            }
            else
            {
                Debug.LogWarning($"⚠️ <color=yellow>{report}</color>");
            }
        }

        // ========================================================================
        // 编辑器工具
        // ========================================================================

        /// <summary>
        /// 清除依赖缓存（用于调试）
        /// </summary>
        [MenuItem("Asaki/Tools/Clear Dependency Cache")]
        private static void ClearDependencyCache()
        {
            _dependencyCache.Clear();
            Debug.Log("[Asaki] Dependency cache cleared.");
        }
    }
}
