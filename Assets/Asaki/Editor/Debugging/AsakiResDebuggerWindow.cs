using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Asaki.Core.Resources;
using Asaki.Editor.Utilities.Extensions;
using Asaki.Unity;
using Asaki.Unity.Services.Resources;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Editor.Debugging
{
    public class AsakiResDebuggerWindow : EditorWindow
    {
        // ==================== 反射字段缓存 ====================
        private FieldInfo cacheField;
        private FieldInfo lockField;
        private FieldInfo strategyField;

        // ResRecord 字段
        private FieldInfo locationField;
        private FieldInfo assetTypeField; // [新增]
        private FieldInfo cacheKeyField; // [新增]
        private FieldInfo assetField;
        private FieldInfo refCountField;
        private FieldInfo dependencyKeysField; // [修改] Locations -> Keys
        private FieldInfo loadingTcsField;
        private FieldInfo progressCallbacksField;

        private PropertyInfo taskProperty;
        private PropertyInfo strategyNameProperty;

        // AsakiContext 反射相关
        private Type asakiContextType;
        private MethodInfo getServiceMethod;

        // ==================== 窗口状态 ====================
        private IAsakiResourceService targetService;
        private Vector2 leftScrollPos;
        private Vector2 rightScrollPos;

        // [修改] 使用 int Key 作为选中标识
        private int selectedKey = 0;

        private float splitterPos = 300f;
        private bool isReflectionInitialized = false;
        private double lastRefreshTime;
        private const float AUTO_REFRESH_INTERVAL = 1f;

        // 自动获取服务状态
        private bool isAutoFetching = true;
        private string fetchStatusMessage = "等待获取服务...";
        private MessageType fetchStatusType = MessageType.Info;

        // ==================== 尺寸限制 ====================
        private const float MIN_LEFT_WIDTH = 200f;
        private const float MIN_RIGHT_WIDTH = 300f;

        [MenuItem("Asaki/Diagnostics/Resources Debugger", false, 52)]
        public static void ShowWindow()
        {
            AsakiResDebuggerWindow window = GetWindow<AsakiResDebuggerWindow>("Resources Debugger");
            window.minSize = new Vector2(800, 500);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeReflection();
            EditorApplication.update += OnEditorUpdate;
            isAutoFetching = true;
            fetchStatusMessage = "正在尝试从 AsakiContext 获取服务...";
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            double currentTime = EditorApplication.timeSinceStartup;

            // 每1秒尝试获取一次服务
            if (isAutoFetching && currentTime - lastRefreshTime > AUTO_REFRESH_INTERVAL)
            {
                TryFetchServiceFromContext();
                lastRefreshTime = currentTime;
            }

            // 每秒刷新UI
            if (currentTime - lastRefreshTime > AUTO_REFRESH_INTERVAL * 0.5f)
            {
                Repaint();
            }
        }

        private void InitializeReflection()
        {
            try
            {
                InitializeContextReflection();
                InitializeServiceReflection();
                isReflectionInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"反射初始化失败: {ex.Message}\n{ex.StackTrace}");
                fetchStatusMessage = $"反射初始化失败: {ex.Message}";
                fetchStatusType = MessageType.Error;
                isAutoFetching = false;
                isReflectionInitialized = false;
            }
        }

        private void InitializeContextReflection()
        {
            string[] possibleTypeNames = new[]
            {
                "Asaki.Core.AsakiContext, Asaki.Core",
                "Asaki.Core.AsakiContext",
                "AsakiContext, Asaki.Core",
                "AsakiContext",
            };

            foreach (string typeName in possibleTypeNames)
            {
                asakiContextType = Type.GetType(typeName);
                if (asakiContextType != null)
                    break;
            }

            if (asakiContextType == null)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var types = assembly
                        .GetTypes()
                        .Where(t => t.Name == "AsakiContext" && t.IsClass && t.IsPublic);
                    if (types.Any())
                    {
                        asakiContextType = types.First();
                        break;
                    }
                }
            }

            if (asakiContextType == null)
            {
                throw new Exception("无法在任何程序集中找到 AsakiContext 类型。");
            }

            MethodInfo genericGetMethod = asakiContextType.GetMethod(
                "Get",
                BindingFlags.Public | BindingFlags.Static
            );
            if (genericGetMethod == null)
                throw new Exception($"AsakiContext 中未找到 Get<T> 方法。");

            getServiceMethod = genericGetMethod.MakeGenericMethod(typeof(IAsakiResourceService));
        }

        private void InitializeServiceReflection()
        {
            Type serviceType = typeof(AsakiResourceService);

            cacheField = serviceType.GetField(
                "_cache",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            lockField = serviceType.GetField(
                "_lock",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            strategyField = serviceType.GetField(
                "_strategy",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (cacheField == null)
                throw new Exception("未找到 '_cache' 字段。");

            Type recordType = serviceType.GetNestedType("ResRecord", BindingFlags.NonPublic);
            if (recordType == null)
                throw new Exception("未找到 ResRecord 嵌套类型。");

            // 获取记录字段 (适配新结构)
            locationField = recordType.GetField("Location");
            assetTypeField = recordType.GetField("AssetType"); // [新增]
            cacheKeyField = recordType.GetField("CacheKey"); // [新增]
            assetField = recordType.GetField("Asset");
            refCountField = recordType.GetField("RefCount");
            dependencyKeysField = recordType.GetField("DependencyKeys"); // [修改]
            loadingTcsField = recordType.GetField("LoadingTcs");
            progressCallbacksField = recordType.GetField("ProgressCallbacks");

            if (loadingTcsField != null)
            {
                Type tcsType = loadingTcsField.FieldType;
                taskProperty = tcsType.GetProperty("Task");
            }

            if (strategyField != null)
            {
                Type strategyType = strategyField.FieldType;
                strategyNameProperty = strategyType.GetProperty("StrategyName");
            }
        }

        private void OnGUI()
        {
            if (!isReflectionInitialized)
            {
                EditorGUILayout.HelpBox("反射初始化失败！", MessageType.Error);
                return;
            }

            DrawToolbar();

            if (targetService == null)
            {
                DrawServiceSelector();
            }
            else
            {
                DrawSplitterLayout();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                Repaint();
            }

            if (
                targetService != null
                && GUILayout.Button(
                    "Clear Selection",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(90)
                )
            )
            {
                selectedKey = 0;
            }

            if (targetService != null)
            {
                GUILayout.Label(
                    $"<color=green>{GetStrategyName()}</color>",
                    EditorStyles.toolbarButton
                );
            }

            GUILayout.FlexibleSpace();

            if (
                GUILayout.Button(
                    isAutoFetching ? "Stop Auto Fetch" : "Start Auto Fetch",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(120)
                )
            )
            {
                isAutoFetching = !isAutoFetching;
                if (isAutoFetching)
                {
                    fetchStatusMessage = "正在尝试从 AsakiContext 获取服务...";
                    fetchStatusType = MessageType.Info;
                }
            }

            if (
                targetService != null
                && GUILayout.Button("Release All", EditorStyles.toolbarButton, GUILayout.Width(80))
            )
            {
                IDictionary cache = GetCacheDictionary();
                if (
                    EditorUtility.DisplayDialog(
                        "Release All Resources",
                        $"释放所有 {cache?.Count ?? 0} 个资源？",
                        "Yes",
                        "No"
                    )
                )
                {
                    ReleaseAllResources();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawServiceSelector()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            if (isAutoFetching)
            {
                EditorGUILayout.HelpBox(fetchStatusMessage, fetchStatusType);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "无法自动从 AsakiContext 获取服务。\n请手动拖拽实例或检查上下文初始化。",
                    MessageType.Warning
                );
            }

            EditorGUILayout.Space();
            Object obj = EditorGUILayout.ObjectField("手动指定服务", null, typeof(Object), true);
            if (obj != null)
            {
                if (
                    obj is IAsakiResourceService service
                    && service.GetType() == typeof(AsakiResourceService)
                )
                {
                    targetService = service;
                    isAutoFetching = false;
                    selectedKey = 0;
                }
            }

            if (!isAutoFetching && GUILayout.Button("重试自动获取"))
            {
                isAutoFetching = true;
                fetchStatusMessage = "正在尝试从 AsakiContext 获取服务...";
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        private void TryFetchServiceFromContext()
        {
            if (getServiceMethod == null)
            {
                isAutoFetching = false;
                return;
            }

            try
            {
                object result = getServiceMethod.Invoke(null, null);
                if (
                    result is IAsakiResourceService service
                    && service.GetType() == typeof(AsakiResourceService)
                )
                {
                    targetService = service;
                    fetchStatusMessage = "✓ 服务获取成功";
                    fetchStatusType = MessageType.Info;
                    isAutoFetching = false;
                }
            }
            catch (Exception)
            {
                // 忽略瞬时错误
            }
        }

        private void DrawSplitterLayout()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(splitterPos));
            DrawLeftPanel();
            GUILayout.EndVertical();

            GUILayoutExtensions.Splitter(
                ref splitterPos,
                MIN_LEFT_WIDTH,
                position.width - MIN_RIGHT_WIDTH
            );

            GUILayout.BeginVertical();
            DrawRightPanel();
            GUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftPanel()
        {
            EditorGUILayout.LabelField("Loaded Resources", EditorStyles.boldLabel);
            leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);

            try
            {
                IDictionary cache = GetCacheDictionary();
                if (cache != null && cache.Count > 0)
                {
                    // 转换为列表以便排序
                    var entries = new List<DictionaryEntry>();
                    foreach (DictionaryEntry entry in cache)
                        entries.Add(entry);

                    // 排序：按 Path 字母顺序 -> 类型名称 -> 引用计数
                    var sortedEntries = entries
                        .OrderBy(e =>
                        {
                            object record = e.Value;
                            string loc = locationField.GetValue(record) as string;
                            return loc;
                        })
                        .ThenBy(e =>
                        {
                            object record = e.Value;
                            Type t = assetTypeField.GetValue(record) as Type;
                            return t?.Name ?? "";
                        });

                    foreach (DictionaryEntry entry in sortedEntries)
                    {
                        DrawResourceButton((int)entry.Key, entry.Value);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No resources loaded.", MessageType.Info);
                }
            }
            catch (Exception ex)
            {
                EditorGUILayout.HelpBox($"列表渲染失败: {ex.Message}", MessageType.Error);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawResourceButton(int key, object record)
        {
            string location = locationField.GetValue(record) as string;
            int refCount = (int)refCountField.GetValue(record);
            Type type = assetTypeField.GetValue(record) as Type;
            Object asset = assetField.GetValue(record) as Object;

            string typeName =
                type != null ? type.Name
                : asset != null ? asset.GetType().Name
                : "Unkown";
            // 显示格式: Location (Type) [RefCount]
            string displayText = $"{location} \n<color=#888888>({typeName})</color> [{refCount}]";

            // 样式设置
            GUIStyle style = new GUIStyle(EditorStyles.label) { richText = true };
            if (selectedKey == key)
            {
                style.fontStyle = FontStyle.Bold;
                // 绘制选中背景
                Rect r = GUILayoutUtility.GetRect(new GUIContent(displayText), style);
                EditorGUI.DrawRect(r, new Color(0.2f, 0.6f, 1f, 0.2f));

                // 恢复Rect绘制文本
                GUI.Label(r, displayText, style);
                // 响应点击
                if (
                    Event.current.type == EventType.MouseDown
                    && r.Contains(Event.current.mousePosition)
                )
                {
                    selectedKey = key;
                    Event.current.Use();
                }
            }
            else
            {
                if (GUILayout.Button(displayText, style))
                {
                    selectedKey = key;
                }
            }
        }

        private void DrawRightPanel()
        {
            if (selectedKey == 0)
            {
                EditorGUILayout.HelpBox("Select a resource to view details.", MessageType.Info);
                return;
            }

            rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);
            try
            {
                IDictionary cache = GetCacheDictionary();
                if (cache != null && cache.Contains(selectedKey))
                {
                    object record = cache[selectedKey];
                    DrawRecordDetails(record);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Selected resource is no longer in cache.",
                        MessageType.Warning
                    );
                    if (GUILayout.Button("Clear Selection"))
                        selectedKey = 0;
                }
            }
            catch (Exception ex)
            {
                EditorGUILayout.HelpBox($"详情渲染失败: {ex.Message}", MessageType.Error);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawRecordDetails(object record)
        {
            DrawBasicInfo(record);
            DrawDependencies(record);
            DrawProgressCallbacks(record);
            DrawLoadingStatus(record);
            DrawActionButtons(record);
        }

        private void DrawBasicInfo(object record)
        {
            EditorGUILayout.LabelField("Resource Details", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            string location = locationField.GetValue(record) as string;
            EditorGUILayout.TextField("Location", location);

            // 显示 Key
            int key = (int)cacheKeyField.GetValue(record);
            EditorGUILayout.TextField("Cache Key (Hash)", key.ToString());

            // 显示请求类型
            Type reqType = assetTypeField.GetValue(record) as Type;
            EditorGUILayout.TextField("Requested Type", reqType?.FullName ?? "None");

            Object asset = assetField.GetValue(record) as Object;
            EditorGUILayout.ObjectField("Asset Reference", asset, typeof(Object), false);

            if (asset != null)
            {
                EditorGUILayout.LabelField("Actual Type", asset.GetType().Name);
                string path = AssetDatabase.GetAssetPath(asset);
                if (!string.IsNullOrEmpty(path))
                {
                    EditorGUILayout.TextField("Physical Path", path);
                }
            }

            int refCount = (int)refCountField.GetValue(record);
            EditorGUILayout.IntField("Reference Count", refCount);

            EditorGUILayout.EndVertical();
        }

        private void DrawDependencies(object record)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dependencies", EditorStyles.boldLabel);

            // 获取依赖 keys (HashSet<int>)
            IEnumerable depKeys = dependencyKeysField.GetValue(record) as IEnumerable;

            if (depKeys != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                IDictionary cache = GetCacheDictionary(); // 需要全量缓存来查找名字
                bool hasDeps = false;

                foreach (object keyObj in depKeys)
                {
                    hasDeps = true;
                    int depKey = (int)keyObj;
                    string depName = "Unknown (Unloaded)";
                    Type depType = null;

                    // 在缓存中查找依赖项的详细信息
                    if (cache != null && cache.Contains(depKey))
                    {
                        object depRecord = cache[depKey];
                        depName = locationField.GetValue(depRecord) as string;
                        depType = assetTypeField.GetValue(depRecord) as Type;
                    }

                    GUIStyle buttonStyle = new GUIStyle(EditorStyles.label);
                    buttonStyle.normal.textColor = new Color(0.4f, 0.8f, 1f);

                    string btnText = $"• {depName} ({depType?.Name ?? "?"})";
                    if (GUILayout.Button(btnText, buttonStyle))
                    {
                        // 点击跳转
                        if (cache != null && cache.Contains(depKey))
                        {
                            selectedKey = depKey;
                        }
                    }
                }

                if (!hasDeps)
                {
                    EditorGUILayout.LabelField("No dependencies", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawProgressCallbacks(object record)
        {
            var progressCallbacks = progressCallbacksField.GetValue(record) as Action<float>;
            if (progressCallbacks != null)
            {
                var list = progressCallbacks.GetInvocationList();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(
                    $"Progress Callbacks ({list.Length})",
                    EditorStyles.boldLabel
                );

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                foreach (Delegate callback in list)
                {
                    string target = callback.Target?.ToString() ?? "Static";
                    string method = callback.Method.Name;
                    EditorGUILayout.LabelField($"• {target}.{method}");
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawLoadingStatus(object record)
        {
            object loadingTcs = loadingTcsField.GetValue(record);
            if (loadingTcs != null && taskProperty != null)
            {
                Task task = taskProperty.GetValue(loadingTcs) as Task;
                if (task != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Task Status", EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.LabelField("State", task.Status.ToString());
                    if (task.IsFaulted)
                    {
                        EditorGUILayout.HelpBox(
                            $"Exception: {task.Exception?.InnerException?.Message}",
                            MessageType.Error
                        );
                    }
                    EditorGUILayout.EndVertical();
                }
            }
        }

        private void DrawActionButtons(object record)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Release (Safe)", GUILayout.Height(30)))
            {
                string loc = locationField.GetValue(record) as string;
                Type type = assetTypeField.GetValue(record) as Type;

                if (
                    EditorUtility.DisplayDialog(
                        "Release",
                        $"Release '{loc}' ({type?.Name})?",
                        "Yes",
                        "No"
                    )
                )
                {
                    // [重要] 使用反射调用 Release(string, Type)
                    MethodInfo releaseMethod = targetService
                        .GetType()
                        .GetMethod("Release", new[] { typeof(string), typeof(Type) });
                    if (releaseMethod != null)
                    {
                        releaseMethod.Invoke(targetService, new object[] { loc, type });
                    }
                    else
                    {
                        // Fallback (虽然不应该发生)
                        targetService.Release(loc, type);
                    }

                    // 如果引用归零被移除了，重置选中
                    IDictionary c = GetCacheDictionary();
                    if (c == null || !c.Contains(selectedKey))
                        selectedKey = 0;
                }
            }

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Force Unload (Internal)", GUILayout.Height(30)))
            {
                if (
                    EditorUtility.DisplayDialog(
                        "Danger",
                        "Force Unload bypasses reference counting. It calls Strategy.UnloadAssetInternal directly.\nAre you sure?",
                        "Yes",
                        "Cancel"
                    )
                )
                {
                    ForceUnloadResource(record);
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void ForceUnloadResource(object record)
        {
            Object asset = assetField.GetValue(record) as Object;
            string location = locationField.GetValue(record) as string;

            // 直接调用 Strategy 的 Unload
            object strategy = strategyField.GetValue(targetService);
            MethodInfo unloadMethod = strategy.GetType().GetMethod("UnloadAssetInternal");

            if (unloadMethod != null)
            {
                unloadMethod.Invoke(strategy, new object[] { location, asset });

                // 还需要手动从 Cache 移除，否则 Service 状态会错乱
                // 这里为了简单，我们只调用 Unload，让 Service 下次 Release 时自行处理（可能会报错），或者手动移除 key
                // 真正的 Force Unload 比较复杂，这里仅作 Strategy 层的卸载演示
                Debug.LogWarning(
                    "[Debugger] Force unloaded asset from memory, but Cache Record remains."
                );
            }
        }

        private void ReleaseAllResources()
        {
            IDictionary cache = GetCacheDictionary();
            if (cache == null)
                return;

            // 收集所有需要释放的信息
            var list = new List<(string loc, Type type)>();
            foreach (DictionaryEntry entry in cache)
            {
                object rec = entry.Value;
                string l = locationField.GetValue(rec) as string;
                Type t = assetTypeField.GetValue(rec) as Type;
                list.Add((l, t));
            }

            // 反射获取 Release 方法
            MethodInfo releaseMethod = targetService
                .GetType()
                .GetMethod("Release", new[] { typeof(string), typeof(Type) });

            foreach ((string loc, Type type) item in list)
            {
                if (releaseMethod != null)
                    releaseMethod.Invoke(targetService, new object[] { item.loc, item.type });
                else
                    targetService.Release(item.loc, item.type);
            }

            selectedKey = 0;
            Repaint();
        }

        private string GetStrategyName()
        {
            if (strategyField == null || targetService == null)
                return "-";
            object st = strategyField.GetValue(targetService);
            return strategyNameProperty?.GetValue(st) as string ?? "Unknown";
        }

        private IDictionary GetCacheDictionary()
        {
            if (targetService == null)
                return null;
            object lockObj = lockField.GetValue(targetService);

            lock (lockObj)
            {
                return cacheField.GetValue(targetService) as IDictionary;
            }
        }

        private int GetRefCount(object record)
        {
            return (int)refCountField.GetValue(record);
        }
    }
}
