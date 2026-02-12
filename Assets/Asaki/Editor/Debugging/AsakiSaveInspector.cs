using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Asaki.Core.Context;
using Asaki.Core.Serialization;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Debugging
{
    /// <summary>
    /// Asaki 存档检查器
    /// 提供槽位管理、自动保存监控、存档操作等功能
    /// </summary>
    public class AsakiSaveInspector : EditorWindow
    {
        [MenuItem("Asaki/Diagnostics/Save Inspector", false, 53)]
        public static void ShowWindow()
        {
            AsakiSaveInspector window = GetWindow<AsakiSaveInspector>();
            window.titleContent = new GUIContent(
                "Save Inspector",
                EditorGUIUtility.IconContent("SaveActive").image
            );
            window.minSize = new Vector2(900, 600);
            window.Show();
        }

        #region 数据模型

        /// <summary>
        /// 槽位视图数据
        /// </summary>
        private class SlotViewData
        {
            public int SlotId;
            public IAsakiSaveSlot SlotInfo;
            public AsakiSaveSlotStatus Status;
            public string SaveName;
            public long LastSaveTime;
            public long FileSize;
            public long PlayTimeSeconds;
            public float ProgressPercent;
            public string CurrentLevel;
            public int PlayerLevel;
            public string PlayerName;
            public string GameVersion;
            public string Description;
            public bool IsLocked;
            public bool IsAutoSaveSlot;
            public bool IsQuickSaveSlot;
            public byte[] ThumbnailData;

            // 快捷属性
            public bool IsEmpty => Status == AsakiSaveSlotStatus.Empty;
            public bool IsOccupied => Status == AsakiSaveSlotStatus.Occupied;
            public bool IsCorrupted => Status == AsakiSaveSlotStatus.Corrupted;
            public string FormattedPlayTime => SlotInfo?.GetFormattedPlayTime() ?? "--";
            public string FormattedSaveTime => SlotInfo?.GetFormattedSaveTime() ?? "--";
        }

        /// <summary>
        /// 自动保存状态快照
        /// </summary>
        private class AutoSaveSnapshot
        {
            public bool IsRunning;
            public bool IsAutoSaving;
            public float TimeUntilNext;
            public long LastAutoSaveTime;
            public int AutoSaveCount;
            public IAsakiAutoSaveConfig Config;
        }

        #endregion

        #region 状态字段

        private readonly List<SlotViewData> _slotData = new List<SlotViewData>();
        private readonly Dictionary<int, bool> _foldoutStates = new Dictionary<int, bool>();
        private AutoSaveSnapshot _autoSaveSnapshot = new AutoSaveSnapshot();

        private string _searchFilter = "";
        private int _selectedSlotId = -1;
        private Vector2 _listScrollPosition;
        private Vector2 _detailsScrollPosition;
        private Vector2 _autoSaveScrollPosition;

        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private float _refreshInterval = 1.0f;
        private float _splitterPosition = 320f;
        private bool _resizingSplitter;

        private bool _showEmptySlots = true;
        private bool _showAutoSaveSlots = true;
        private bool _showQuickSaveSlots = true;
        private SortMode _currentSortMode = SortMode.SlotId;
        private bool _sortAscending = true;

        private Tab _currentTab = Tab.Slots;
        private Texture2D _thumbnailTexture;
        private bool _isLoadingThumbnail;

        private enum Tab
        {
            Slots,
            AutoSave,
            Operations,
        }

        private enum SortMode
        {
            SlotId,
            SaveTime,
            PlayTime,
            Progress,
            FileSize,
            PlayerLevel,
        }

        #endregion

        #region 样式缓存

        private class StyleCache
        {
            public GUIStyle SelectedItemStyle;
            public GUIStyle NormalItemStyle;
            public GUIStyle HeaderStyle;
            public GUIStyle SubHeaderStyle;
            public GUIStyle BoxStyle;
            public GUIStyle StatsLabelStyle;
            public GUIStyle CenteredLabelStyle;
            public GUIStyle ToolbarSearchFieldStyle;
            public GUIStyle SlotStatusStyle;
            public GUIStyle TabStyle;
            public GUIStyle SelectedTabStyle;

            public Texture2D GreenDotTexture;
            public Texture2D YellowDotTexture;
            public Texture2D RedDotTexture;
            public Texture2D GrayDotTexture;
            public Texture2D BlueDotTexture;
            public Texture2D LockIconTexture;

            private bool _initialized;

            public void Initialize()
            {
                if (_initialized)
                    return;

                // 列表项样式
                NormalItemStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(10, 10, 8, 8),
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

                // 标题样式
                HeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    margin = new RectOffset(0, 0, 10, 10),
                };

                SubHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13,
                    margin = new RectOffset(0, 0, 8, 8),
                };

                // 其他样式
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

                SlotStatusStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    padding = new RectOffset(4, 4, 2, 2),
                };

                // Tab 样式
                TabStyle = new GUIStyle(EditorStyles.toolbarButton)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                };

                SelectedTabStyle = new GUIStyle(TabStyle)
                {
                    normal =
                    {
                        textColor = Color.white,
                        background = CreateTexture(2, 2, new Color(0.2f, 0.4f, 0.8f)),
                    },
                    fontStyle = FontStyle.Bold,
                };

                // 状态指示点纹理
                GreenDotTexture = CreateDotTexture(new Color(0.2f, 0.8f, 0.2f));
                YellowDotTexture = CreateDotTexture(new Color(1f, 0.8f, 0.2f));
                RedDotTexture = CreateDotTexture(new Color(0.9f, 0.3f, 0.3f));
                GrayDotTexture = CreateDotTexture(new Color(0.5f, 0.5f, 0.5f));
                BlueDotTexture = CreateDotTexture(new Color(0.3f, 0.6f, 1f));

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

            private static Texture2D CreateDotTexture(Color color)
            {
                const int size = 12;
                Texture2D texture = new Texture2D(size, size);
                Color[] pixels = new Color[size * size];
                Vector2 center = new Vector2(size / 2f, size / 2f);
                float radius = size / 2f - 1;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        pixels[y * size + x] = dist <= radius ? color : Color.clear;
                    }
                }

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
            if (_thumbnailTexture != null)
            {
                DestroyImmediate(_thumbnailTexture);
                _thumbnailTexture = null;
            }
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
            DrawTabs();

            if (!Application.isPlaying)
            {
                DrawPlayModeMessage();
                return;
            }

            switch (_currentTab)
            {
                case Tab.Slots:
                    DrawSlotsTab();
                    break;
                case Tab.AutoSave:
                    DrawAutoSaveTab();
                    break;
                case Tab.Operations:
                    DrawOperationsTab();
                    break;
            }
        }

        #endregion

        #region 工具栏和标签

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

            // 打开文件夹按钮
            if (GUILayout.Button("Open Folder", EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                OpenSaveFolder();
            }

            GUILayout.FlexibleSpace();

            // 搜索框
            _searchFilter = EditorGUILayout.TextField(
                _searchFilter,
                _styles.ToolbarSearchFieldStyle,
                GUILayout.Width(200)
            );

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (
                GUILayout.Button(
                    "Slots",
                    _currentTab == Tab.Slots ? _styles.SelectedTabStyle : _styles.TabStyle
                )
            )
            {
                _currentTab = Tab.Slots;
            }

            if (
                GUILayout.Button(
                    "Auto Save",
                    _currentTab == Tab.AutoSave ? _styles.SelectedTabStyle : _styles.TabStyle
                )
            )
            {
                _currentTab = Tab.AutoSave;
            }

            if (
                GUILayout.Button(
                    "Operations",
                    _currentTab == Tab.Operations ? _styles.SelectedTabStyle : _styles.TabStyle
                )
            )
            {
                _currentTab = Tab.Operations;
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Slots Tab

        private void DrawSlotsTab()
        {
            EditorGUILayout.BeginHorizontal();

            // 左侧面板
            DrawSlotListPanel();

            // 分隔条
            DrawSplitter();

            // 右侧面板
            DrawSlotDetailsPanel();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSlotListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(_splitterPosition));

            // 过滤选项
            DrawFilterOptions();

            GUILayout.Space(4);

            // 槽位列表
            _listScrollPosition = EditorGUILayout.BeginScrollView(_listScrollPosition);

            IEnumerable<SlotViewData> filteredData = _slotData;

            // 搜索过滤
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                string filter = _searchFilter.ToLowerInvariant();
                filteredData = filteredData.Where(d =>
                    d.SaveName?.ToLowerInvariant().Contains(filter) == true
                    || d.PlayerName?.ToLowerInvariant().Contains(filter) == true
                    || d.CurrentLevel?.ToLowerInvariant().Contains(filter) == true
                    || d.SlotId.ToString().Contains(filter)
                );
            }

            // 类型过滤
            if (!_showEmptySlots)
            {
                filteredData = filteredData.Where(d => !d.IsEmpty);
            }
            if (!_showAutoSaveSlots)
            {
                filteredData = filteredData.Where(d => !d.IsAutoSaveSlot);
            }
            if (!_showQuickSaveSlots)
            {
                filteredData = filteredData.Where(d => !d.IsQuickSaveSlot);
            }

            // 排序
            filteredData = SortSlotData(filteredData);

            int index = 0;
            foreach (SlotViewData data in filteredData)
            {
                DrawSlotListItem(data, index++);
            }

            if (index == 0)
            {
                EditorGUILayout.HelpBox("No slots match the current filter.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();

            // 状态栏
            DrawSlotListStatus();

            EditorGUILayout.EndVertical();
        }

        private void DrawFilterOptions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            // 显示选项
            EditorGUILayout.BeginVertical();
            _showEmptySlots = GUILayout.Toggle(_showEmptySlots, "Show Empty", GUILayout.Width(100));
            _showAutoSaveSlots = GUILayout.Toggle(
                _showAutoSaveSlots,
                "Show Auto Save",
                GUILayout.Width(100)
            );
            _showQuickSaveSlots = GUILayout.Toggle(
                _showQuickSaveSlots,
                "Show Quick Save",
                GUILayout.Width(100)
            );
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // 排序选项
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Sort By:", EditorStyles.miniLabel, GUILayout.Width(80));
            _currentSortMode = (SortMode)
                EditorGUILayout.EnumPopup(_currentSortMode, GUILayout.Width(120));
            if (GUILayout.Button(_sortAscending ? "▲ Asc" : "▼ Desc", GUILayout.Width(60)))
            {
                _sortAscending = !_sortAscending;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawSlotListStatus()
        {
            int total = _slotData.Count;
            int occupied = _slotData.Count(d => d.IsOccupied);
            int empty = _slotData.Count(d => d.IsEmpty);
            int corrupted = _slotData.Count(d => d.IsCorrupted);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                $"Total: {total}",
                EditorStyles.miniLabel,
                GUILayout.Width(60)
            );
            EditorGUILayout.LabelField(
                $"Occupied: {occupied}",
                EditorStyles.miniLabel,
                GUILayout.Width(80)
            );
            EditorGUILayout.LabelField(
                $"Empty: {empty}",
                EditorStyles.miniLabel,
                GUILayout.Width(60)
            );
            if (corrupted > 0)
            {
                GUI.color = Color.red;
                EditorGUILayout.LabelField(
                    $"Corrupted: {corrupted}",
                    EditorStyles.miniLabel,
                    GUILayout.Width(80)
                );
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();
        }

        private IEnumerable<SlotViewData> SortSlotData(IEnumerable<SlotViewData> data)
        {
            IOrderedEnumerable<SlotViewData> ordered = _currentSortMode switch
            {
                SortMode.SlotId => _sortAscending
                    ? data.OrderBy(d => d.SlotId)
                    : data.OrderByDescending(d => d.SlotId),
                SortMode.SaveTime => _sortAscending
                    ? data.OrderBy(d => d.LastSaveTime)
                    : data.OrderByDescending(d => d.LastSaveTime),
                SortMode.PlayTime => _sortAscending
                    ? data.OrderBy(d => d.PlayTimeSeconds)
                    : data.OrderByDescending(d => d.PlayTimeSeconds),
                SortMode.Progress => _sortAscending
                    ? data.OrderBy(d => d.ProgressPercent)
                    : data.OrderByDescending(d => d.ProgressPercent),
                SortMode.FileSize => _sortAscending
                    ? data.OrderBy(d => d.FileSize)
                    : data.OrderByDescending(d => d.FileSize),
                SortMode.PlayerLevel => _sortAscending
                    ? data.OrderBy(d => d.PlayerLevel)
                    : data.OrderByDescending(d => d.PlayerLevel),
                _ => data.OrderBy(d => d.SlotId),
            };
            return ordered;
        }

        private void DrawSlotListItem(SlotViewData data, int index)
        {
            bool isSelected = data.SlotId == _selectedSlotId;

            // 构建标签
            string slotType =
                data.IsAutoSaveSlot ? "[A]"
                : data.IsQuickSaveSlot ? "[Q]"
                : "";
            string statusIcon = GetStatusIcon(data.Status);
            string slotName = string.IsNullOrEmpty(data.SaveName)
                ? $"Slot {data.SlotId}"
                : data.SaveName;
            string subInfo = data.IsEmpty
                ? "Empty"
                : $"{data.FormattedSaveTime} | {data.FormattedPlayTime}";

            string label =
                $"{statusIcon} {slotType} Slot {data.SlotId}: {slotName}\n<color=#888888><size=10>{subInfo}</size></color>";

            GUIStyle style = isSelected ? _styles.SelectedItemStyle : _styles.NormalItemStyle;

            if (GUILayout.Button(label, style, GUILayout.Height(40)))
            {
                _selectedSlotId = data.SlotId;
                LoadThumbnail(data);
                GUI.FocusControl(null);
            }
        }

        private string GetStatusIcon(AsakiSaveSlotStatus status)
        {
            return status switch
            {
                AsakiSaveSlotStatus.Occupied => "●",
                AsakiSaveSlotStatus.Empty => "○",
                AsakiSaveSlotStatus.Corrupted => "✕",
                AsakiSaveSlotStatus.Locked => "🔒",
                _ => "?",
            };
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
                _resizingSplitter = true;
                GUI.FocusControl(null);
            }

            if (_resizingSplitter && Event.current.type == EventType.MouseDrag)
            {
                _splitterPosition += Event.current.delta.x;
                _splitterPosition = Mathf.Clamp(_splitterPosition, 250f, position.width - 400f);
                Event.current.Use();
            }

            if (Event.current.type == EventType.MouseUp)
            {
                _resizingSplitter = false;
            }
        }

        private void DrawSlotDetailsPanel()
        {
            EditorGUILayout.BeginVertical();
            _detailsScrollPosition = EditorGUILayout.BeginScrollView(_detailsScrollPosition);

            SlotViewData data = _slotData.FirstOrDefault(d => d.SlotId == _selectedSlotId);

            if (data != null)
            {
                DrawSlotDetails(data);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Select a slot from the list to view details.",
                    MessageType.Info
                );
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawSlotDetails(SlotViewData data)
        {
            // 标题
            EditorGUILayout.LabelField($"Slot {data.SlotId}", _styles.HeaderStyle);

            // 状态徽章
            DrawStatusBadge(data);

            GUILayout.Space(12);

            // 缩略图
            DrawThumbnail(data);

            GUILayout.Space(12);

            // 基本信息
            DrawBasicInfoSection(data);

            GUILayout.Space(12);

            // 游戏进度
            if (!data.IsEmpty)
            {
                DrawProgressSection(data);
                GUILayout.Space(12);
            }

            // 操作按钮
            DrawSlotActions(data);
        }

        private void DrawStatusBadge(SlotViewData data)
        {
            EditorGUILayout.BeginHorizontal();

            // 状态标签
            (string text, Color color) = data.Status switch
            {
                AsakiSaveSlotStatus.Occupied => ("Occupied", new Color(0.2f, 0.8f, 0.2f)),
                AsakiSaveSlotStatus.Empty => ("Empty", new Color(0.5f, 0.5f, 0.5f)),
                AsakiSaveSlotStatus.Corrupted => ("Corrupted", new Color(0.9f, 0.3f, 0.3f)),
                AsakiSaveSlotStatus.Locked => ("Locked", new Color(1f, 0.6f, 0.2f)),
                _ => ("Unknown", Color.gray),
            };

            GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { background = CreateColorTexture(color), textColor = Color.white },
                padding = new RectOffset(8, 8, 4, 4),
                border = new RectOffset(4, 4, 4, 4),
            };

            GUILayout.Label(text, badgeStyle, GUILayout.Width(80));

            // 类型标签
            if (data.IsAutoSaveSlot)
            {
                GUIStyle autoSaveStyle = new GUIStyle(badgeStyle)
                {
                    normal =
                    {
                        background = CreateColorTexture(new Color(0.3f, 0.6f, 1f)),
                        textColor = Color.white,
                    },
                };
                GUILayout.Label("Auto Save", autoSaveStyle, GUILayout.Width(80));
            }
            else if (data.IsQuickSaveSlot)
            {
                GUIStyle quickSaveStyle = new GUIStyle(badgeStyle)
                {
                    normal =
                    {
                        background = CreateColorTexture(new Color(1f, 0.8f, 0.2f)),
                        textColor = Color.black,
                    },
                };
                GUILayout.Label("Quick Save", quickSaveStyle, GUILayout.Width(80));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawThumbnail(SlotViewData data)
        {
            if (data.ThumbnailData != null && data.ThumbnailData.Length > 0)
            {
                if (_thumbnailTexture != null && !_isLoadingThumbnail)
                {
                    float aspectRatio = (float)_thumbnailTexture.width / _thumbnailTexture.height;
                    float maxWidth = Mathf.Min(300, position.width - _splitterPosition - 40);
                    float height = maxWidth / aspectRatio;

                    Rect rect = GUILayoutUtility.GetRect(maxWidth, height);
                    GUI.DrawTexture(rect, _thumbnailTexture, ScaleMode.ScaleToFit);
                }
                else if (_isLoadingThumbnail)
                {
                    EditorGUILayout.LabelField(
                        "Loading thumbnail...",
                        EditorStyles.centeredGreyMiniLabel,
                        GUILayout.Height(100)
                    );
                }
                else
                {
                    EditorGUILayout.LabelField(
                        "Failed to load thumbnail",
                        EditorStyles.centeredGreyMiniLabel,
                        GUILayout.Height(100)
                    );
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.FlexibleSpace();
                GUILayout.Label(
                    "No Thumbnail",
                    EditorStyles.centeredGreyMiniLabel,
                    GUILayout.Height(100)
                );
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawBasicInfoSection(SlotViewData data)
        {
            EditorGUILayout.BeginVertical(_styles.BoxStyle);
            EditorGUILayout.LabelField("Basic Information", _styles.SubHeaderStyle);

            DrawInfoRow("Save Name:", data.SaveName ?? "--");
            DrawInfoRow("Player Name:", data.PlayerName ?? "--");
            DrawInfoRow("Game Version:", data.GameVersion ?? "--");
            DrawInfoRow("File Size:", FormatFileSize(data.FileSize));
            DrawInfoRow("Last Save:", data.FormattedSaveTime);
            DrawInfoRow("Description:", data.Description ?? "--");

            EditorGUILayout.EndVertical();
        }

        private void DrawProgressSection(SlotViewData data)
        {
            EditorGUILayout.BeginVertical(_styles.BoxStyle);
            EditorGUILayout.LabelField("Game Progress", _styles.SubHeaderStyle);

            DrawInfoRow("Play Time:", data.FormattedPlayTime);
            DrawInfoRow("Current Level:", data.CurrentLevel ?? "--");
            DrawInfoRow("Player Level:", data.PlayerLevel > 0 ? data.PlayerLevel.ToString() : "--");

            // 进度条
            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Progress:", GUILayout.Width(80));
            Rect barRect = GUILayoutUtility.GetRect(100, 20, GUILayout.ExpandWidth(true));
            DrawProgressBar(barRect, data.ProgressPercent);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawProgressBar(Rect rect, float percent)
        {
            // 背景
            GUI.DrawTexture(rect, _styles.GrayDotTexture);

            // 填充
            if (percent > 0)
            {
                Rect fillRect = new Rect(
                    rect.x,
                    rect.y,
                    rect.width * (percent / 100f),
                    rect.height
                );
                Texture2D fillTexture =
                    percent < 30f ? _styles.RedDotTexture
                    : percent < 70f ? _styles.YellowDotTexture
                    : _styles.GreenDotTexture;
                GUI.DrawTexture(fillRect, fillTexture);
            }

            // 边框
            GUI.Box(rect, "", GUI.skin.box);

            // 文本
            GUI.Label(rect, $"{percent:F1}%", _styles.CenteredLabelStyle);
        }

        private void DrawInfoRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(100));
            EditorGUILayout.LabelField(value, _styles.StatsLabelStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSlotActions(SlotViewData data)
        {
            EditorGUILayout.LabelField("Actions", _styles.SubHeaderStyle);

            EditorGUILayout.BeginHorizontal();

            // 加载按钮
            GUI.enabled = data.IsOccupied && !data.IsCorrupted;
            if (GUILayout.Button("Load Save", GUILayout.Height(32)))
            {
                LoadSave(data.SlotId);
            }
            GUI.enabled = true;

            // 删除按钮
            if (data.IsOccupied || data.IsCorrupted)
            {
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("Delete", GUILayout.Height(32), GUILayout.Width(80)))
                {
                    DeleteSlot(data.SlotId);
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            // 槽位管理按钮
            if (!data.IsEmpty)
            {
                GUILayout.Space(8);
                EditorGUILayout.BeginHorizontal();

                // 锁定/解锁
                if (data.IsLocked)
                {
                    if (GUILayout.Button("Unlock", GUILayout.Height(28)))
                    {
                        UnlockSlot(data.SlotId);
                    }
                }
                else
                {
                    if (GUILayout.Button("Lock", GUILayout.Height(28)))
                    {
                        LockSlot(data.SlotId);
                    }
                }

                // 复制
                if (GUILayout.Button("Copy To...", GUILayout.Height(28)))
                {
                    CopySlot(data.SlotId);
                }

                // 备份
                if (GUILayout.Button("Backup", GUILayout.Height(28)))
                {
                    CreateBackup(data.SlotId);
                }

                EditorGUILayout.EndHorizontal();

                // 导出/导入
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Export...", GUILayout.Height(28)))
                {
                    ExportSlot(data.SlotId);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        #endregion

        #region AutoSave Tab

        private void DrawAutoSaveTab()
        {
            _autoSaveScrollPosition = EditorGUILayout.BeginScrollView(_autoSaveScrollPosition);

            if (
                !AsakiContext.TryGet<IAsakiAutoSaveService>(
                    out IAsakiAutoSaveService autoSaveService
                )
            )
            {
                EditorGUILayout.HelpBox(
                    "IAsakiAutoSaveService not found in Context.\n"
                        + "Make sure the Bootstrapper has started and AutoSaveModule is initialized.",
                    MessageType.Warning
                );
                EditorGUILayout.EndScrollView();
                return;
            }

            // 状态概览
            DrawAutoSaveStatus(autoSaveService);

            GUILayout.Space(16);

            // 配置信息
            DrawAutoSaveConfig(autoSaveService);

            GUILayout.Space(16);

            // 操作按钮
            DrawAutoSaveActions(autoSaveService);

            EditorGUILayout.EndScrollView();
        }

        private void DrawAutoSaveStatus(IAsakiAutoSaveService service)
        {
            EditorGUILayout.BeginVertical(_styles.BoxStyle);
            EditorGUILayout.LabelField("Auto Save Status", _styles.HeaderStyle);

            bool isEnabled = service.Config?.Enabled ?? false;
            bool isRunning = _autoSaveSnapshot.IsRunning;
            bool isAutoSaving = _autoSaveSnapshot.IsAutoSaving;

            // 状态指示器
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Status:", GUILayout.Width(120));

            string statusText;
            Color statusColor;

            if (isAutoSaving)
            {
                statusText = "Saving...";
                statusColor = new Color(1f, 0.8f, 0.2f);
            }
            else if (isRunning && isEnabled)
            {
                statusText = "Running";
                statusColor = new Color(0.2f, 0.8f, 0.2f);
            }
            else if (!isEnabled)
            {
                statusText = "Disabled";
                statusColor = new Color(0.5f, 0.5f, 0.5f);
            }
            else
            {
                statusText = "Stopped";
                statusColor = new Color(0.9f, 0.3f, 0.3f);
            }

            GUIStyle statusStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = statusColor },
            };
            EditorGUILayout.LabelField(statusText, statusStyle);
            EditorGUILayout.EndHorizontal();

            // 倒计时
            if (
                isRunning
                && isEnabled
                && !isAutoSaving
                && service.Config.Triggers.HasFlag(AsakiAutoSaveTrigger.TimeInterval)
            )
            {
                float timeUntil = _autoSaveSnapshot.TimeUntilNext;
                int minutes = Mathf.FloorToInt(timeUntil / 60f);
                int seconds = Mathf.FloorToInt(timeUntil % 60f);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Next Save:", GUILayout.Width(120));
                EditorGUILayout.LabelField($"{minutes:00}:{seconds:00}", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                // 倒计时进度条
                float totalInterval = service.Config.TimeIntervalSeconds;
                float progress = 1f - (timeUntil / totalInterval);
                Rect barRect = GUILayoutUtility.GetRect(100, 16, GUILayout.ExpandWidth(true));
                DrawProgressBar(barRect, progress * 100f);
            }

            // 统计信息
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Auto Save Count:", GUILayout.Width(120));
            EditorGUILayout.LabelField(_autoSaveSnapshot.AutoSaveCount.ToString());
            EditorGUILayout.EndHorizontal();

            if (_autoSaveSnapshot.LastAutoSaveTime > 0)
            {
                DateTime lastSaveTime = DateTimeOffset
                    .FromUnixTimeMilliseconds(_autoSaveSnapshot.LastAutoSaveTime)
                    .LocalDateTime;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Last Save:", GUILayout.Width(120));
                EditorGUILayout.LabelField(lastSaveTime.ToString("yyyy-MM-dd HH:mm:ss"));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAutoSaveConfig(IAsakiAutoSaveService service)
        {
            IAsakiAutoSaveConfig config = service.Config;
            if (config == null)
            {
                EditorGUILayout.HelpBox("DataTable not available.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical(_styles.BoxStyle);
            EditorGUILayout.LabelField("DataTable", _styles.SubHeaderStyle);

            DrawInfoRow("Enabled:", config.Enabled.ToString());
            DrawInfoRow("Triggers:", config.Triggers.ToString());
            DrawInfoRow("Time Interval:", $"{config.TimeIntervalSeconds:F0} seconds");
            DrawInfoRow("Countdown:", $"{config.CountdownSeconds:F0} seconds");
            DrawInfoRow("Max Auto Saves:", config.MaxAutoSaveCount.ToString());
            DrawInfoRow("Start Index:", config.AutoSaveSlotStartIndex.ToString());
            DrawInfoRow("Show Notification:", config.ShowNotification.ToString());
            DrawInfoRow("Generate Thumbnail:", config.GenerateThumbnail.ToString());

            if (config.GenerateThumbnail)
            {
                EditorGUI.indentLevel++;
                DrawInfoRow("Thumbnail Size:", $"{config.ThumbnailWidth}x{config.ThumbnailHeight}");
                DrawInfoRow("Quality:", $"{config.ThumbnailQuality}%");
                EditorGUI.indentLevel--;
            }

            DrawInfoRow("Check Storage:", config.CheckStorageSpace.ToString());
            DrawInfoRow("Min Free Space:", $"{config.MinFreeSpaceMB} MB");
            DrawInfoRow("Min Interval:", $"{config.MinIntervalBetweenSaves:F0} seconds");
            DrawInfoRow("Keep Latest:", config.KeepLatestAutoSave.ToString());

            EditorGUILayout.EndVertical();
        }

        private void DrawAutoSaveActions(IAsakiAutoSaveService service)
        {
            EditorGUILayout.LabelField("Actions", _styles.SubHeaderStyle);

            EditorGUILayout.BeginHorizontal();

            if (service.IsAutoSaving)
            {
                GUI.enabled = false;
                GUILayout.Button("Saving...", GUILayout.Height(32));
                GUI.enabled = true;
            }
            else
            {
                if (GUILayout.Button("Force Auto Save", GUILayout.Height(32)))
                {
                    ForceAutoSave();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (service.Config?.Enabled == true)
            {
                if (GUILayout.Button("Pause", GUILayout.Height(28)))
                {
                    PauseAutoSave();
                }
            }
            else
            {
                if (GUILayout.Button("Resume", GUILayout.Height(28)))
                {
                    ResumeAutoSave();
                }
            }

            if (GUILayout.Button("Reset Timer", GUILayout.Height(28)))
            {
                ResetAutoSaveTimer();
            }

            if (GUILayout.Button("Cancel Countdown", GUILayout.Height(28)))
            {
                CancelAutoSaveCountdown();
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Operations Tab

        private void DrawOperationsTab()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Batch Operations", _styles.HeaderStyle);

            GUILayout.Space(16);

            // 批量删除
            EditorGUILayout.BeginVertical(_styles.BoxStyle);
            EditorGUILayout.LabelField("Delete All Saves", _styles.SubHeaderStyle);
            EditorGUILayout.LabelField(
                "This will delete all saved slots except auto save and locked slots.",
                EditorStyles.wordWrappedLabel
            );
            GUILayout.Space(8);

            GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
            if (GUILayout.Button("Delete All Manual Saves", GUILayout.Height(32)))
            {
                if (
                    EditorUtility.DisplayDialog(
                        "Delete All Manual Saves",
                        "Are you sure you want to delete all manual saves?\n\n"
                            + "Auto saves and locked slots will be preserved.\n"
                            + "This action cannot be undone!",
                        "Delete All",
                        "Cancel"
                    )
                )
                {
                    DeleteAllManualSaves();
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();

            GUILayout.Space(16);

            // 导入
            EditorGUILayout.BeginVertical(_styles.BoxStyle);
            EditorGUILayout.LabelField("Import Save", _styles.SubHeaderStyle);
            EditorGUILayout.LabelField(
                "Import a save file from external location.",
                EditorStyles.wordWrappedLabel
            );
            GUILayout.Space(8);

            if (GUILayout.Button("Import Save...", GUILayout.Height(32)))
            {
                ImportSave();
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(16);

            // 清理
            EditorGUILayout.BeginVertical(_styles.BoxStyle);
            EditorGUILayout.LabelField("Cleanup", _styles.SubHeaderStyle);
            EditorGUILayout.LabelField(
                "Clean up corrupted or empty slot folders.",
                EditorStyles.wordWrappedLabel
            );
            GUILayout.Space(8);

            if (GUILayout.Button("Clean Up Empty Folders", GUILayout.Height(28)))
            {
                CleanupEmptyFolders();
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(16);

            // 统计
            DrawOperationsStatistics();

            EditorGUILayout.EndVertical();
        }

        private void DrawOperationsStatistics()
        {
            EditorGUILayout.BeginVertical(_styles.BoxStyle);
            EditorGUILayout.LabelField("Statistics", _styles.SubHeaderStyle);

            int totalSlots = _slotData.Count;
            int occupiedSlots = _slotData.Count(d => d.IsOccupied);
            int emptySlots = _slotData.Count(d => d.IsEmpty);
            int corruptedSlots = _slotData.Count(d => d.IsCorrupted);
            int lockedSlots = _slotData.Count(d => d.IsLocked);
            long totalSize = _slotData.Sum(d => d.FileSize);

            DrawInfoRow("Total Slots:", totalSlots.ToString());
            DrawInfoRow("Occupied:", occupiedSlots.ToString());
            DrawInfoRow("Empty:", emptySlots.ToString());
            DrawInfoRow("Corrupted:", corruptedSlots.ToString());
            DrawInfoRow("Locked:", lockedSlots.ToString());
            DrawInfoRow("Total Size:", FormatFileSize(totalSize));

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Helper Methods

        private void DrawPlayModeMessage()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUIContent icon = EditorGUIUtility.IconContent("Console.InfoIcon");
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(icon, GUILayout.Width(32), GUILayout.Height(32));
            GUILayout.Space(8);
            GUILayout.Label("Enter Play Mode to view save data", _styles.CenteredLabelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
        }

        private static Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        #endregion

        #region 数据刷新

        private void RefreshData()
        {
            RefreshSlotData();
            RefreshAutoSaveData();
        }

        private void RefreshSlotData()
        {
            _slotData.Clear();

            // 尝试从 IAsakiSaveSlotManager 获取槽位信息
            if (AsakiContext.TryGet<IAsakiSaveSlotManager>(out IAsakiSaveSlotManager slotManager))
            {
                var slots = slotManager.GetAllSlots();
                int autoSaveIndex = slotManager.AutoSaveSlotIndex;
                int quickSaveIndex = slotManager.QuickSaveSlotIndex;

                foreach (var slot in slots)
                {
                    if (slot == null)
                        continue;

                    _slotData.Add(
                        new SlotViewData
                        {
                            SlotId = slot.SlotId,
                            SlotInfo = slot,
                            Status = slot.Status,
                            SaveName = slot.SaveName,
                            LastSaveTime = slot.LastSaveTime,
                            FileSize = slot.FileSize,
                            PlayTimeSeconds = slot.PlayTimeSeconds,
                            ProgressPercent = slot.ProgressPercent,
                            CurrentLevel = slot.CurrentLevel,
                            PlayerLevel = slot.PlayerLevel,
                            PlayerName = slot.PlayerName,
                            GameVersion = slot.GameVersion,
                            Description = slot.Description,
                            IsLocked = slot.Status == AsakiSaveSlotStatus.Locked,
                            IsAutoSaveSlot = slot.SlotId == autoSaveIndex,
                            IsQuickSaveSlot = slot.SlotId == quickSaveIndex,
                            ThumbnailData = slot.ThumbnailData,
                        }
                    );
                }
            }
            // 回退到 IAsakiSaveService
            else if (AsakiContext.TryGet<IAsakiSaveService>(out IAsakiSaveService saveService))
            {
                var slotInfos = saveService.GetAllSlotInfos();
                foreach (var info in slotInfos)
                {
                    _slotData.Add(
                        new SlotViewData
                        {
                            SlotId = info.SlotId,
                            Status = info.Exists
                                ? AsakiSaveSlotStatus.Occupied
                                : AsakiSaveSlotStatus.Empty,
                            SaveName = info.SaveName,
                            LastSaveTime = info.LastSaveTime,
                            FileSize = info.FileSize,
                            IsAutoSaveSlot = info.SlotId == 0,
                            IsQuickSaveSlot = info.SlotId == 1,
                        }
                    );
                }
            }
            // 回退到文件系统扫描
            else
            {
                RefreshFromFileSystem();
            }
        }

        private void RefreshFromFileSystem()
        {
            string saveRoot = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(saveRoot))
                return;

            DirectoryInfo dir = new DirectoryInfo(saveRoot);
            var slotDirs = dir.GetDirectories("Slot_*");

            foreach (var slotDir in slotDirs)
            {
                if (int.TryParse(slotDir.Name.Substring(5), out int slotId))
                {
                    string metaPath = Path.Combine(slotDir.FullName, "meta.json");
                    string dataPath = Path.Combine(slotDir.FullName, "data.bin");

                    bool exists = File.Exists(metaPath) && File.Exists(dataPath);
                    long fileSize =
                        exists && File.Exists(dataPath) ? new FileInfo(dataPath).Length : 0;

                    _slotData.Add(
                        new SlotViewData
                        {
                            SlotId = slotId,
                            Status = exists
                                ? AsakiSaveSlotStatus.Occupied
                                : AsakiSaveSlotStatus.Empty,
                            LastSaveTime = exists
                                ? new FileInfo(metaPath).LastWriteTimeUtc.Ticks
                                    / TimeSpan.TicksPerMillisecond
                                : 0,
                            FileSize = fileSize,
                            IsAutoSaveSlot = slotId == 0,
                            IsQuickSaveSlot = slotId == 1,
                        }
                    );
                }
            }
        }

        private void RefreshAutoSaveData()
        {
            if (
                AsakiContext.TryGet<IAsakiAutoSaveService>(
                    out IAsakiAutoSaveService autoSaveService
                )
            )
            {
                _autoSaveSnapshot.IsRunning = autoSaveService.CanAutoSave();
                _autoSaveSnapshot.IsAutoSaving = autoSaveService.IsAutoSaving;
                _autoSaveSnapshot.TimeUntilNext = autoSaveService.TimeUntilNextAutoSave;
                _autoSaveSnapshot.LastAutoSaveTime = autoSaveService.LastAutoSaveTime;
                _autoSaveSnapshot.AutoSaveCount = autoSaveService.AutoSaveCount;
                _autoSaveSnapshot.Config = autoSaveService.Config;
            }
        }

        private void LoadThumbnail(SlotViewData data)
        {
            if (_thumbnailTexture != null)
            {
                DestroyImmediate(_thumbnailTexture);
                _thumbnailTexture = null;
            }

            if (data.ThumbnailData == null || data.ThumbnailData.Length == 0)
            {
                _isLoadingThumbnail = false;
                return;
            }

            _isLoadingThumbnail = true;

            try
            {
                _thumbnailTexture = new Texture2D(2, 2);
                if (_thumbnailTexture.LoadImage(data.ThumbnailData))
                {
                    _isLoadingThumbnail = false;
                }
                else
                {
                    DestroyImmediate(_thumbnailTexture);
                    _thumbnailTexture = null;
                    _isLoadingThumbnail = false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AsakiSaveInspector] Failed to load thumbnail: {ex.Message}");
                _isLoadingThumbnail = false;
            }
        }

        #endregion

        #region 槽位操作

        private void LoadSave(int slotId)
        {
            Debug.Log($"[AsakiSaveInspector] Loading save from slot {slotId}...");
            // 实际加载逻辑由调用方实现
        }

        private void DeleteSlot(int slotId)
        {
            if (
                !EditorUtility.DisplayDialog(
                    "Delete Save",
                    $"Are you sure you want to delete the save in slot {slotId}?\n\n"
                        + "This action cannot be undone!",
                    "Delete",
                    "Cancel"
                )
            )
            {
                return;
            }

            if (AsakiContext.TryGet<IAsakiSaveSlotManager>(out IAsakiSaveSlotManager slotManager))
            {
                bool success = slotManager.DeleteSave(slotId);
                if (success)
                {
                    Debug.Log($"[AsakiSaveInspector] Deleted slot {slotId}");
                    if (_selectedSlotId == slotId)
                    {
                        _selectedSlotId = -1;
                    }
                    RefreshData();
                }
                else
                {
                    Debug.LogError($"[AsakiSaveInspector] Failed to delete slot {slotId}");
                }
            }
            else if (AsakiContext.TryGet<IAsakiSaveService>(out IAsakiSaveService saveService))
            {
                bool success = saveService.DeleteSlot(slotId);
                if (success)
                {
                    Debug.Log($"[AsakiSaveInspector] Deleted slot {slotId}");
                    if (_selectedSlotId == slotId)
                    {
                        _selectedSlotId = -1;
                    }
                    RefreshData();
                }
            }
        }

        private void LockSlot(int slotId)
        {
            if (AsakiContext.TryGet<IAsakiSaveSlotManager>(out IAsakiSaveSlotManager slotManager))
            {
                bool success = slotManager.LockSlot(slotId);
                if (success)
                {
                    Debug.Log($"[AsakiSaveInspector] Locked slot {slotId}");
                    RefreshData();
                }
            }
        }

        private void UnlockSlot(int slotId)
        {
            if (AsakiContext.TryGet<IAsakiSaveSlotManager>(out IAsakiSaveSlotManager slotManager))
            {
                bool success = slotManager.UnlockSlot(slotId);
                if (success)
                {
                    Debug.Log($"[AsakiSaveInspector] Unlocked slot {slotId}");
                    RefreshData();
                }
            }
        }

        private void CopySlot(int sourceSlotId)
        {
            GenericMenu menu = new GenericMenu();

            // 找到可用槽位
            var emptySlots = _slotData.Where(d => d.IsEmpty).Select(d => d.SlotId).ToList();
            if (emptySlots.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Copy Save",
                    "No empty slots available.\nPlease delete a slot first.",
                    "OK"
                );
                return;
            }

            foreach (int targetSlotId in emptySlots)
            {
                menu.AddItem(
                    new GUIContent($"Slot {targetSlotId}"),
                    false,
                    () =>
                    {
                        PerformCopySlot(sourceSlotId, targetSlotId);
                    }
                );
            }

            menu.ShowAsContext();
        }

        private async void PerformCopySlot(int sourceSlotId, int targetSlotId)
        {
            if (AsakiContext.TryGet<IAsakiSaveSlotManager>(out IAsakiSaveSlotManager slotManager))
            {
                try
                {
                    await slotManager.CopySaveAsync(sourceSlotId, targetSlotId);
                    Debug.Log($"[AsakiSaveInspector] Copied slot {sourceSlotId} to {targetSlotId}");
                    RefreshData();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AsakiSaveInspector] Failed to copy slot: {ex.Message}");
                }
            }
        }

        private async void CreateBackup(int slotId)
        {
            string backupName = EditorUtility.SaveFilePanel(
                "Create Backup",
                "",
                $"Backup_Slot_{slotId}_{DateTime.Now:yyyyMMdd_HHmmss}",
                ""
            );

            if (string.IsNullOrEmpty(backupName))
                return;

            if (AsakiContext.TryGet<IAsakiSaveSlotManager>(out IAsakiSaveSlotManager slotManager))
            {
                try
                {
                    await slotManager.CreateBackupAsync(slotId, backupName);
                    Debug.Log($"[AsakiSaveInspector] Created backup for slot {slotId}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AsakiSaveInspector] Failed to create backup: {ex.Message}");
                }
            }
        }

        private async void ExportSlot(int slotId)
        {
            string exportPath = EditorUtility.SaveFilePanel(
                "Export Save",
                "",
                $"Save_Slot_{slotId}_{DateTime.Now:yyyyMMdd_HHmmss}",
                "sav"
            );

            if (string.IsNullOrEmpty(exportPath))
                return;

            if (AsakiContext.TryGet<IAsakiSaveService>(out IAsakiSaveService saveService))
            {
                try
                {
                    bool success = await saveService.ExportSlotAsync(slotId, exportPath);
                    if (success)
                    {
                        Debug.Log($"[AsakiSaveInspector] Exported slot {slotId} to {exportPath}");
                    }
                    else
                    {
                        Debug.LogError($"[AsakiSaveInspector] Failed to export slot {slotId}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AsakiSaveInspector] Failed to export slot: {ex.Message}");
                }
            }
        }

        private void ImportSave()
        {
            string importPath = EditorUtility.OpenFilePanel("Import Save", "", "sav");
            if (string.IsNullOrEmpty(importPath))
                return;

            // 选择目标槽位
            GenericMenu menu = new GenericMenu();
            var emptySlots = _slotData.Where(d => d.IsEmpty).Select(d => d.SlotId).ToList();

            foreach (int targetSlotId in emptySlots)
            {
                menu.AddItem(
                    new GUIContent($"Slot {targetSlotId}"),
                    false,
                    async () =>
                    {
                        if (
                            AsakiContext.TryGet<IAsakiSaveService>(
                                out IAsakiSaveService saveService
                            )
                        )
                        {
                            try
                            {
                                bool success = await saveService.ImportSlotAsync(
                                    importPath,
                                    targetSlotId
                                );
                                if (success)
                                {
                                    Debug.Log(
                                        $"[AsakiSaveInspector] Imported save to slot {targetSlotId}"
                                    );
                                    RefreshData();
                                }
                                else
                                {
                                    Debug.LogError($"[AsakiSaveInspector] Failed to import save");
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError(
                                    $"[AsakiSaveInspector] Failed to import save: {ex.Message}"
                                );
                            }
                        }
                    }
                );
            }

            menu.ShowAsContext();
        }

        private void DeleteAllManualSaves()
        {
            if (AsakiContext.TryGet<IAsakiSaveSlotManager>(out IAsakiSaveSlotManager slotManager))
            {
                int autoSaveIndex = slotManager.AutoSaveSlotIndex;
                List<int> slotsToDelete = _slotData
                    .Where(d => !d.IsEmpty && d.SlotId != autoSaveIndex && !d.IsLocked)
                    .Select(d => d.SlotId)
                    .ToList();
                int deletedCount = 0;
                foreach (int t in slotsToDelete)
                {
                    try
                    {
                        if (slotManager.DeleteSave(t))
                            deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(
                            $"[AsakiSaveInspector] Failed to delete save {t}: {ex.Message}"
                        );
                    }
                }
                Debug.Log($"[AsakiSaveInspector] Deleted {deletedCount} manual saves");
                _selectedSlotId = -1;
                RefreshData();
            }
        }

        private void CleanupEmptyFolders()
        {
            string saveRoot = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(saveRoot))
                return;

            int cleanedCount = 0;
            DirectoryInfo dir = new DirectoryInfo(saveRoot);
            foreach (var subDir in dir.GetDirectories())
            {
                // 检查是否为空文件夹或损坏的槽位
                bool hasMeta = File.Exists(Path.Combine(subDir.FullName, "meta.json"));
                bool hasData = File.Exists(Path.Combine(subDir.FullName, "data.bin"));

                if (!hasMeta && !hasData)
                {
                    try
                    {
                        subDir.Delete(true);
                        cleanedCount++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(
                            $"[AsakiSaveInspector] Failed to delete folder {subDir.Name}: {ex.Message}"
                        );
                    }
                }
                else if (hasMeta != hasData)
                {
                    // 损坏的槽位（缺少meta或data文件）
                    Debug.LogWarning(
                        $"[AsakiSaveInspector] Detected corrupted slot: {subDir.Name}"
                    );
                }
            }

            Debug.Log($"[AsakiSaveInspector] Cleaned up {cleanedCount} empty folders");
            RefreshData();
        }

        private void OpenSaveFolder()
        {
            string saveRoot = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(saveRoot))
            {
                Directory.CreateDirectory(saveRoot);
            }
            EditorUtility.RevealInFinder(saveRoot);
        }

        #endregion

        #region 自动保存操作

        private async void ForceAutoSave()
        {
            if (
                AsakiContext.TryGet<IAsakiAutoSaveService>(
                    out IAsakiAutoSaveService autoSaveService
                )
            )
            {
                try
                {
                    bool success = await autoSaveService.ForceAutoSaveAsync(
                        AsakiAutoSaveTrigger.Manual
                    );
                    if (success)
                    {
                        Debug.Log("[AsakiSaveInspector] Force auto save completed");
                        RefreshData();
                    }
                    else
                    {
                        Debug.LogWarning("[AsakiSaveInspector] Force auto save failed");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AsakiSaveInspector] Force auto save error: {ex.Message}");
                }
            }
        }

        private void PauseAutoSave()
        {
            if (
                AsakiContext.TryGet<IAsakiAutoSaveService>(
                    out IAsakiAutoSaveService autoSaveService
                )
            )
            {
                autoSaveService.Pause();
                Debug.Log("[AsakiSaveInspector] Auto save paused");
                RefreshData();
            }
        }

        private void ResumeAutoSave()
        {
            if (
                AsakiContext.TryGet<IAsakiAutoSaveService>(
                    out IAsakiAutoSaveService autoSaveService
                )
            )
            {
                autoSaveService.Resume();
                Debug.Log("[AsakiSaveInspector] Auto save resumed");
                RefreshData();
            }
        }

        private void ResetAutoSaveTimer()
        {
            if (
                AsakiContext.TryGet<IAsakiAutoSaveService>(
                    out IAsakiAutoSaveService autoSaveService
                )
            )
            {
                autoSaveService.ResetTimer();
                Debug.Log("[AsakiSaveInspector] Auto save timer reset");
                RefreshData();
            }
        }

        private void CancelAutoSaveCountdown()
        {
            if (
                AsakiContext.TryGet<IAsakiAutoSaveService>(
                    out IAsakiAutoSaveService autoSaveService
                )
            )
            {
                autoSaveService.CancelCountdown();
                Debug.Log("[AsakiSaveInspector] Auto save countdown cancelled");
                RefreshData();
            }
        }

        #endregion
    }
}
