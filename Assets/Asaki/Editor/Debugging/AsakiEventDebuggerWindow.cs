using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Editor.Utilities.Extensions;
using UnityEditor;
using UnityEngine;
using AsakiBroker = Asaki.Core.Broker.AsakiBroker;
using Component = UnityEngine.Component;

namespace Asaki.Editor.Debugging
{
    public class AsakiEventDebuggerWindow : EditorWindow
    {
        [MenuItem("Asaki/Debugger/Event Inspector &F8")]
        public static void ShowWindow()
        {
            AsakiEventDebuggerWindow wnd = GetWindow<AsakiEventDebuggerWindow>();
            wnd.titleContent = new GUIContent(
                "Asaki Events",
                EditorGUIUtility.IconContent("d_EventSystem Icon").image
            );
            wnd.minSize = new Vector2(600, 400);
            wnd.Show();
        }

        // --- 状态数据 ---
        private List<Type> _allEventTypes = new List<Type>();
        private Type _selectedEventType;
        private string _searchFilter = "";
        private Vector2 _scrollPosLeft;
        private Vector2 _scrollPosRight;
        private float _leftPanelWidth = 200f;

        // --- 反射缓存 ---
        private Dictionary<Type, object> _paramInstances = new Dictionary<Type, object>();
        private Dictionary<Type, EventDebugInfo> _eventInfoCache =
            new Dictionary<Type, EventDebugInfo>();
        private Dictionary<Type, List<FieldInfo>> _fieldInfoCache =
            new Dictionary<Type, List<FieldInfo>>();

        // --- 性能统计 ---
        private Dictionary<Type, EventStats> _eventStats = new Dictionary<Type, EventStats>();
        private DateTime _sessionStartTime = DateTime.Now;

        private FieldInfo _busBucketsField;
        private FieldInfo _bucketHandlersField;

        private class EventDebugInfo
        {
            public string Namespace;
            public string FullName;
            public List<string> Interfaces = new List<string>();
            public List<FieldInfo> Fields = new List<FieldInfo>();
            public bool HasDocumentation;
            public Type BaseType;
        }

        private class EventStats
        {
            public int PublishCount;
            public double LastPublishTime;
            public int SubscriberCount;
        }

        private void OnEnable()
        {
            RefreshEventTypes();
            InitializeReflection();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void InitializeReflection()
        {
            Type busType = typeof(AsakiEventService);
            _busBucketsField = busType.GetField(
                "_buckets",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _eventStats.Clear();
                _sessionStartTime = DateTime.Now;
            }
            Repaint();
        }

        // 【核心修复】在 OnGUI 开始时强制钳制宽度
        private void OnGUI()
        {
            DrawToolbar();

            // 布局钳制
            float windowWidth = position.width;
            const float minPanelWidth = 150f;
            const float rightPanelMin = 400f;
            float maxPanelWidth = Mathf.Max(minPanelWidth, windowWidth - rightPanelMin);

            _leftPanelWidth = Mathf.Clamp(_leftPanelWidth, minPanelWidth, maxPanelWidth);

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            GUILayoutExtensions.Splitter(
                ref _leftPanelWidth,
                minPanelWidth,
                maxPanelWidth,
                false,
                () => Repaint()
            );
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void RefreshEventTypes()
        {
            _allEventTypes = TypeCache
                .GetTypesDerivedFrom<IAsakiEvent>()
                .Where(t => t.IsValueType && !t.IsAbstract)
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name)
                .ToList();

            foreach (Type type in _allEventTypes)
            {
                CacheEventInfo(type);
                if (!_eventStats.ContainsKey(type))
                {
                    _eventStats[type] = new EventStats();
                }
            }
        }

        private void CacheEventInfo(Type type)
        {
            if (_eventInfoCache.ContainsKey(type))
                return;

            EventDebugInfo info = new EventDebugInfo
            {
                Namespace = type.Namespace,
                FullName = type.FullName,
                BaseType = type.BaseType,
                Fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance).ToList(),
                Interfaces = type.GetInterfaces().Select(i => i.Name).ToList(),
            };

            info.HasDocumentation = !string.IsNullOrEmpty(GetTypeSummary(type));
            _eventInfoCache[type] = info;
            _fieldInfoCache[type] = info.Fields;
        }

        private string GetTypeSummary(Type type)
        {
            DescriptionAttribute attr = type.GetCustomAttribute<DescriptionAttribute>();
            return attr?.Description ?? "";
        }

        // ==================================================================================
        // UI 绘制逻辑 - 完全使用 GUILayout 自动布局
        // ==================================================================================

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                RefreshEventTypes();

            _searchFilter = EditorGUILayout.TextField(
                _searchFilter,
                EditorStyles.toolbarSearchField,
                GUILayout.Width(200)
            );

            GUILayout.FlexibleSpace();
            if (Application.isPlaying)
            {
                double elapsed = (DateTime.Now - _sessionStartTime).TotalSeconds;
                GUILayout.Label($"● LIVE ({elapsed:F0}s)", EditorStyles.boldLabel);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(_leftPanelWidth));
            _scrollPosLeft = EditorGUILayout.BeginScrollView(_scrollPosLeft);

            var filteredTypes = string.IsNullOrEmpty(_searchFilter)
                ? _allEventTypes
                : _allEventTypes
                    .Where(t =>
                        t.Name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0
                    )
                    .ToList();

            foreach (Type type in filteredTypes)
            {
                // 实时更新订阅数
                if (Event.current.type == EventType.Layout && Application.isPlaying)
                {
                    _eventStats[type].SubscriberCount = GetSubscriberCount(type);
                }
                DrawEventListItem(type, _eventStats[type]);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 【核心修复】使用纯 GUILayout，移除所有固定宽度限制
        /// </summary>
        private void DrawEventListItem(Type type, EventStats stats)
        {
            bool isSelected = type == _selectedEventType;
            GUIStyle style = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft };
            if (isSelected)
                style.normal.textColor = Color.cyan;

            EditorGUILayout.BeginHorizontal(style);

            if (GUILayout.Button(type.Name, style, GUILayout.ExpandWidth(true)))
            {
                _selectedEventType = type;
                EnsureParamInstance(type);
                GUI.FocusControl(null);
            }

            // 订阅数 Badge
            if (stats.SubscriberCount > 0)
            {
                Color originalColor = GUI.color;
                GUI.color = Color.green;
                GUILayout.Label(
                    stats.SubscriberCount.ToString(),
                    EditorStyles.miniLabel,
                    GUILayout.Width(20)
                );
                GUI.color = originalColor;
            }

            EditorGUILayout.EndHorizontal();
        }

        private GUIStyle GetListItemStyle(bool isSelected)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(0, 0, 1, 1),
                richText = true,
                fixedHeight = 36,
                wordWrap = true, // [Fix 5] 允许文字换行
            };

            if (isSelected)
            {
                Texture2D bgTex = new Texture2D(1, 1);
                bgTex.SetPixel(0, 0, new Color(0.2f, 0.5f, 0.8f, 0.25f));
                bgTex.Apply();
                style.normal.background = bgTex;
                style.hover.background = bgTex;
                style.normal.textColor = Color.cyan;
                style.hover.textColor = Color.cyan;
            }

            return style;
        }

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();
            _scrollPosRight = EditorGUILayout.BeginScrollView(_scrollPosRight);

            if (_selectedEventType != null)
            {
                EventDebugInfo info = _eventInfoCache[_selectedEventType];
                EventStats stats = _eventStats[_selectedEventType];

                DrawHeader(info);
                DrawManualTriggerCard(_selectedEventType);
                DrawSubscribersCard(_selectedEventType, stats); // 现在能正确显示了
            }
            else
            {
                GUILayout.Label("Select an event to inspect", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawHeader(EventDebugInfo info)
        {
            EditorGUILayout.LabelField(info.FullName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(info.Namespace, EditorStyles.miniLabel);
            EditorGUILayout.Space();
        }

        private void DrawTag(string text, Color color)
        {
            Color bgColor = GUI.color;
            GUI.color = color;
            GUILayout.Label(text, EditorStyles.miniButton, GUILayout.Width(70));
            GUI.color = bgColor;
        }

        private void DrawMetadataCard(EventDebugInfo info)
        {
            EditorGUILayout.BeginVertical(Styles.Card);
            EditorGUILayout.LabelField("Metadata", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Namespace:", GUILayout.Width(90));
            EditorGUILayout.SelectableLabel(
                info.Namespace,
                EditorStyles.textField,
                GUILayout.Height(16)
            );
            EditorGUILayout.EndHorizontal();
            if (info.BaseType != null && info.BaseType != typeof(ValueType))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Base Type:", GUILayout.Width(90));
                EditorGUILayout.LabelField(info.BaseType.Name, EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            if (info.Interfaces.Any())
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Interfaces:", EditorStyles.miniLabel);
                foreach (string iface in info.Interfaces.Take(3))
                {
                    EditorGUILayout.LabelField($"  • {iface}", EditorStyles.miniLabel);
                }
                if (info.Interfaces.Count > 3)
                {
                    EditorGUILayout.LabelField(
                        $"  ... and {info.Interfaces.Count - 3} more",
                        EditorStyles.miniLabel
                    );
                }
            }
            if (info.Fields.Any())
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField(
                    $"Fields ({info.Fields.Count}):",
                    EditorStyles.miniLabel
                );
                foreach (FieldInfo field in info.Fields.Take(5))
                {
                    EditorGUILayout.LabelField(
                        $"  • <b>{field.Name}</b>: {field.FieldType.Name}",
                        Styles.RichTextMiniLabel
                    );
                }
                if (info.Fields.Count > 5)
                {
                    EditorGUILayout.LabelField(
                        $"  ... and {info.Fields.Count - 5} more",
                        EditorStyles.miniLabel
                    );
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawStatsCard(EventStats stats)
        {
            EditorGUILayout.BeginVertical(Styles.Card);
            EditorGUILayout.LabelField("Runtime Statistics", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Total Publishes:", GUILayout.Width(120));
            EditorGUILayout.LabelField(stats.PublishCount.ToString("N0"), Styles.BoldValue);
            EditorGUILayout.EndHorizontal();
            if (stats.LastPublishTime > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Last Publish:", GUILayout.Width(120));
                double timeSinceLast = EditorApplication.timeSinceStartup - stats.LastPublishTime;
                EditorGUILayout.LabelField($"{timeSinceLast:F2}s ago", Styles.BoldValue);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Subscribers:", GUILayout.Width(120));
            GUI.color = stats.SubscriberCount > 0 ? Color.green : Color.gray;
            EditorGUILayout.LabelField(stats.SubscriberCount.ToString(), Styles.BoldValue);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Statistics available only in Play mode.",
                    MessageType.Info
                );
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawSubscribersCard(Type eventType, EventStats stats)
        {
            EditorGUILayout.LabelField("Active Subscribers", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Subscribers only visible in Play Mode.", MessageType.Info);
                return;
            }

            var subscribers = GetSubscribers(eventType);
            if (subscribers.Count == 0)
            {
                EditorGUILayout.LabelField("No active subscribers.", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = 0; i < subscribers.Count; i++)
                {
                    object sub = subscribers[i];
                    EditorGUILayout.BeginHorizontal("box");
                    // 尝试识别 Unity Object
                    if (sub is UnityEngine.Object obj)
                    {
                        EditorGUILayout.ObjectField(obj, typeof(UnityEngine.Object), true);
                    }
                    else
                    {
                        EditorGUILayout.LabelField(sub.GetType().Name);
                        EditorGUILayout.LabelField(sub.ToString(), EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.Space();
        }

        private void DrawSubscriberItem(object handler, int index)
        {
            EditorGUILayout.BeginVertical(Styles.SubscriberItem);
            if (handler == null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(
                    EditorGUIUtility.IconContent("console.erroricon"),
                    GUILayout.Width(20)
                );
                EditorGUILayout.LabelField(
                    $"Handler #{index}: <Destroyed/Null>",
                    Styles.ErrorLabel
                );
                EditorGUILayout.EndHorizontal();
            }
            else if (handler is UnityEngine.Object unityObj)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(
                    unityObj,
                    unityObj.GetType(),
                    true,
                    GUILayout.Height(20)
                );
                if (unityObj is Component comp && comp.gameObject != null)
                {
                    string path = GetGameObjectPath(comp.gameObject);
                    GUI.contentColor = new Color(0.7f, 0.7f, 0.7f);
                    EditorGUILayout.LabelField(path, Styles.MiniLabel, GUILayout.MaxWidth(200));
                    GUI.contentColor = Color.white;
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(
                    EditorGUIUtility.IconContent("cs Script Icon"),
                    GUILayout.Width(20)
                );
                EditorGUILayout.LabelField(handler.GetType().Name, Styles.NormalLabel);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField(handler.ToString(), Styles.MiniLabel);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawManualTriggerCard(Type eventType)
        {
            EditorGUILayout.LabelField("Manual Trigger", EditorStyles.boldLabel);

            EnsureParamInstance(eventType);
            object instance = _paramInstances[eventType];

            if (instance == null)
                return;

            // 绘制字段编辑器
            foreach (FieldInfo field in _fieldInfoCache[eventType])
            {
                DrawEnhancedField(field, instance); // 复用之前的 UI 逻辑
            }

            if (GUILayout.Button("Publish Event"))
            {
                // 使用兼容层触发
                PublishEvent(eventType, instance);
            }
        }

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUIContent icon = EditorGUIUtility.IconContent("d_EventSystem Icon");
            icon.text = "\nSelect an event from the list to inspect";
            GUIStyle style = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                richText = true,
            };
            GUILayout.Label(icon, style, GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
        }

        // ==================================================================================
        // 核心反射逻辑
        // ==================================================================================

        private Type GetBucketType(Type eventType)
        {
            Type genericBucket = typeof(AsakiBroker).GetNestedType(
                "EventBucket`1",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            return genericBucket?.MakeGenericType(eventType);
        }

        private int GetSubscriberCount(Type eventType)
        {
            if (!Application.isPlaying)
                return 0;

            // 1. 获取 Bus 实例
            if (!AsakiContext.TryGet<IAsakiEventService>(out IAsakiEventService busInterface))
                return 0;
            if (!(busInterface is AsakiEventService bus))
                return 0;

            // 2. 反射获取 _buckets 字典
            if (_busBucketsField == null)
                return 0;
            IDictionary buckets = _busBucketsField.GetValue(bus) as IDictionary; // Dictionary<Type, IEventBucket>
            if (buckets == null || !buckets.Contains(eventType))
                return 0;

            // 3. 获取 Bucket 实例
            object bucket = buckets[eventType];
            if (bucket == null)
                return 0;

            // 4. 反射获取 _handlers 列表
            // 注意：bucket 是泛型类 AsakiEventService.EventBucket<T>
            Type bucketType = bucket.GetType();
            FieldInfo handlersField = bucketType.GetField(
                "_handlers",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            if (handlersField == null)
                return 0;

            IList list = handlersField.GetValue(bucket) as IList;
            return list?.Count ?? 0;
        }

        private List<object> GetSubscribers(Type eventType)
        {
            if (!Application.isPlaying)
                return new List<object>();

            if (!AsakiContext.TryGet<IAsakiEventService>(out IAsakiEventService busInterface))
                return new List<object>();
            if (!(busInterface is AsakiEventService bus))
                return new List<object>();

            if (_busBucketsField == null)
                return new List<object>();
            IDictionary buckets = _busBucketsField.GetValue(bus) as IDictionary;
            if (buckets == null || !buckets.Contains(eventType))
                return new List<object>();

            object bucket = buckets[eventType];
            Type bucketType = bucket.GetType();
            FieldInfo handlersField = bucketType.GetField(
                "_handlers",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            IEnumerable list = handlersField?.GetValue(bucket) as IEnumerable;
            return list?.Cast<object>().ToList() ?? new List<object>();
        }

        private void PublishEvent(Type eventType, object data)
        {
            // 使用 AsakiBroker.Publish 泛型方法 (它会自动转发给 Context.Bus)
            MethodInfo method = typeof(AsakiBroker).GetMethod(
                "Publish",
                BindingFlags.Public | BindingFlags.Static
            );
            if (method != null)
            {
                MethodInfo generic = method.MakeGenericMethod(eventType);
                generic.Invoke(null, new[] { data });
            }

            // Debug Log
            Debug.Log($"[Debugger] Published {eventType.Name}");
        }

        private void EnsureParamInstance(Type type)
        {
            if (!_paramInstances.ContainsKey(type) || _paramInstances[type] == null)
            {
                try
                {
                    _paramInstances[type] = Activator.CreateInstance(type);
                }
                catch
                {
                    _paramInstances[type] = null;
                }
            }
        }

        private string GetGameObjectPath(GameObject go)
        {
            string path = go.name;
            Transform parent = go.transform.parent;
            while (parent != null)
            {
                path = $"{parent.name}/{path}";
                parent = parent.parent;
            }
            return path;
        }

        private void DrawEnhancedField(FieldInfo field, object instance)
        {
            object value = field.GetValue(instance);
            string name = ObjectNames.NicifyVariableName(field.Name);
            Type t = field.FieldType;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(name, GUILayout.Width(150));

            try
            {
                if (t == typeof(int))
                    field.SetValue(instance, EditorGUILayout.IntField((int)value));
                else if (t == typeof(float))
                    field.SetValue(instance, EditorGUILayout.FloatField((float)value));
                else if (t == typeof(double))
                    field.SetValue(instance, EditorGUILayout.DoubleField((double)value));
                else if (t == typeof(string))
                    field.SetValue(instance, EditorGUILayout.TextField((string)(value ?? "")));
                else if (t == typeof(bool))
                    field.SetValue(instance, EditorGUILayout.Toggle((bool)value));
                else if (t == typeof(Vector3))
                    field.SetValue(instance, EditorGUILayout.Vector3Field("", (Vector3)value));
                else if (t == typeof(Vector2))
                    field.SetValue(instance, EditorGUILayout.Vector2Field("", (Vector2)value));
                else if (t == typeof(Vector4))
                    field.SetValue(instance, EditorGUILayout.Vector4Field("", (Vector4)value));
                else if (t == typeof(Color))
                    field.SetValue(instance, EditorGUILayout.ColorField((Color)value));
                else if (t == typeof(AnimationCurve))
                    field.SetValue(instance, EditorGUILayout.CurveField((AnimationCurve)value));
                else if (t == typeof(LayerMask))
                    field.SetValue(
                        instance,
                        (LayerMask)EditorGUILayout.LayerField((LayerMask)value)
                    );
                else if (t.IsEnum)
                    field.SetValue(instance, EditorGUILayout.EnumPopup((Enum)value));
                else if (
                    t.IsArray
                    || t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>)
                )
                {
                    EditorGUILayout.LabelField(
                        $"({t.Name}) Use code to set",
                        EditorStyles.miniLabel
                    );
                }
                else if (t.IsValueType && !t.IsPrimitive)
                {
                    EditorGUILayout.LabelField($"Nested: {t.Name}", EditorStyles.miniLabel);
                }
                else
                {
                    GUI.enabled = false;
                    EditorGUILayout.LabelField($"({t.Name}) Unsupported", EditorStyles.miniLabel);
                    GUI.enabled = true;
                }
            }
            catch (Exception ex)
            {
                EditorGUILayout.LabelField($"Error: {ex.Message}", Styles.ErrorLabel);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSeparator()
        {
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        }

        // ==================================================================================
        // 样式定义
        // ==================================================================================
        private static class Styles
        {
            public static GUIStyle Card;
            public static GUIStyle BigTitle;
            public static GUIStyle BoldValue;
            public static GUIStyle NormalLabel;
            public static GUIStyle BoldLabel;
            public static GUIStyle MiniLabel;
            public static GUIStyle RichTextMiniLabel;
            public static GUIStyle ErrorLabel;
            public static GUIStyle SubscriberItem;
            public static GUIStyle Description;
            public static GUIStyle RightAlignedLabel;

            static Styles()
            {
                Card = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(12, 12, 12, 12),
                    margin = new RectOffset(0, 0, 6, 6),
                };

                BigTitle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    fixedHeight = 24,
                    richText = true,
                };

                BoldValue = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    richText = true,
                };

                NormalLabel = new GUIStyle(EditorStyles.label) { richText = true };
                BoldLabel = new GUIStyle(EditorStyles.boldLabel) { richText = true };
                MiniLabel = new GUIStyle(EditorStyles.miniLabel) { richText = true };

                RichTextMiniLabel = new GUIStyle(EditorStyles.miniLabel)
                {
                    richText = true,
                    wordWrap = false,
                };

                ErrorLabel = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = Color.red },
                    richText = true,
                };

                SubscriberItem = new GUIStyle(GUI.skin.box)
                {
                    margin = new RectOffset(0, 0, 2, 2),
                    padding = new RectOffset(8, 8, 6, 6),
                };

                Description = new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    fontStyle = FontStyle.Italic,
                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                    richText = true,
                };

                RightAlignedLabel = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleRight,
                    richText = true,
                };
            }
        }
    }
}
