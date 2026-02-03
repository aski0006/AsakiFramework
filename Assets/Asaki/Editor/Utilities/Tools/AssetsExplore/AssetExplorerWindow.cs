// ============================================================================
// 主命名空间：Unity编辑器工具集
// ============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Asaki.Editor.Utilities.Extensions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Editor.Utilities.Tools.AssetsExplore
{
    public class AssetExplorerWindow : EditorWindow
    {
        private const string WINDOW_TITLE = "Asset Explorer Pro";
        private const string PREFS_FAVORITES = "AssetExplorer_Favorites";
        private const float ROW_HEIGHT = 20f;

        // 核心组件
        private AssetScanner _scanner;
        private SearchEngine _searchEngine;

        // UI状态
        private Vector2 _categoryScrollPos;
        private Vector2 _assetScrollPos;
        private string _searchText = "";
        private AssetCategory _selectedCategory = AssetCategory.All;
        private AssetInfo _selectedAsset;
        private readonly List<AssetInfo> _filteredAssets = new List<AssetInfo>();
        private readonly Dictionary<AssetCategory, int> _categoryCounts =
            new Dictionary<AssetCategory, int>();

        // 新增：资源缓冲队列，避免实时处理导致卡顿
        private readonly ConcurrentQueue<AssetInfo> _pendingAssets =
            new ConcurrentQueue<AssetInfo>();
        private readonly HashSet<string> _processedGuids = new HashSet<string>();

        // 布局
        private float _categoryPanelWidth = 200f;
        private float _detailPanelWidth = 300f;
        private readonly SplitterState _categorySplitter = new SplitterState(150f, 100f, 350f);
        private readonly SplitterState _detailSplitter = new SplitterState(250f, 150f, 500f);

        // 状态栏
        private string _statusText = "就绪";
        private double _scanTime;

        // 收藏
        private readonly HashSet<string> _favorites = new HashSet<string>();

        // 设置窗口
        private SettingsWindow _settingsWindow;

        // 新增：预览图标加载队列和节流控制
        private readonly Queue<AssetInfo> _previewLoadQueue = new Queue<AssetInfo>();
        private float _lastPreviewLoadTime;
        private const float PREVIEW_LOAD_INTERVAL = 0.1f; // 每0.1秒加载一个预览

        // 新增：分类计数缓存，限制更新频率
        private Dictionary<AssetCategory, int> _cachedCategoryCounts;
        private float _lastCategoryCountUpdate;

        [MenuItem("Asaki/Tools/Asset Explorer %&a", false, 10)]
        public static void ShowWindow()
        {
            AssetExplorerWindow window = GetWindow<AssetExplorerWindow>(WINDOW_TITLE);
            window.minSize = new Vector2(800, 500);
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(
                WINDOW_TITLE,
                EditorGUIUtility.IconContent("UnityLogo").image
            );

            _scanner = new AssetScanner();
            _searchEngine = new SearchEngine();

            // 事件订阅
            _scanner.OnAssetFound += OnAssetFound;
            _scanner.OnScanComplete += OnScanComplete;
            _scanner.OnError += OnScanError;

            LoadFavorites();
            _cachedCategoryCounts = new Dictionary<AssetCategory, int>();

            // 自动启动扫描
            EditorApplication.delayCall += () => StartScan();
        }

        private void OnDisable()
        {
            SaveFavorites();

            if (_scanner != null)
            {
                _scanner.CancelScan();
                _scanner.OnAssetFound -= OnAssetFound;
                _scanner.OnScanComplete -= OnScanComplete;
                _scanner.OnError -= OnScanError;
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawMainContent();
            DrawStatusbar();
            HandleKeyboardShortcuts();
        }

        // 新增：在Update中异步处理预览加载，避免在OnGUI中阻塞
        private void Update()
        {
            // 处理预览图标加载队列
            if (
                _previewLoadQueue.Count > 0
                && Time.realtimeSinceStartup - _lastPreviewLoadTime > PREVIEW_LOAD_INTERVAL
            )
            {
                AssetInfo asset = _previewLoadQueue.Dequeue();
                LoadPreviewIcon(asset);
                _lastPreviewLoadTime = Time.realtimeSinceStartup;
                Repaint(); // 仅在有预览加载时刷新
            }

            // 扫描完成后处理待处理资源
            if (!_scanner.IsScanning && _pendingAssets.Count > 0)
            {
                ProcessPendingAssets();
            }
        }

        #region 工具栏

        private void DrawToolbar()
        {
            try
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

                // 搜索框
                GUI.SetNextControlName("SearchField");
                GUIStyle searchStyle =
                    GUI.skin.FindStyle("ToolbarSearchTextField") ?? EditorStyles.toolbarTextField;
                if (searchStyle == null)
                    searchStyle = EditorStyles.textField; // 最终保险

                string newSearchText = EditorGUILayout.TextField(
                    _searchText,
                    searchStyle,
                    GUILayout.MinWidth(200),
                    GUILayout.ExpandWidth(true)
                );

                if (newSearchText != _searchText)
                {
                    _searchText = newSearchText;
                    ApplySearch();
                }

                // 搜索模式
                SearchEngine.SearchMode searchMode = (SearchEngine.SearchMode)
                    EditorGUILayout.EnumPopup(_searchEngine._mode, GUILayout.Width(100));
                if (searchMode != _searchEngine._mode)
                {
                    _searchEngine._mode = searchMode;
                    ApplySearch();
                }

                // 快速搜索保存按钮
                GUIStyle cancelStyle =
                    GUI.skin.FindStyle("ToolbarSearchCancelButtonEmpty")
                    ?? EditorStyles.toolbarButton;
                if (GUILayout.Button("", cancelStyle, GUILayout.Width(20)))
                {
                    _searchEngine.SaveCurrentSearch();
                }

                GUILayout.Space(10);

                // 分类筛选
                string[] categoryNames = Enum.GetNames(typeof(AssetCategory));
                int categoryIndex = (int)_selectedCategory;
                int newIndex = EditorGUILayout.Popup(
                    categoryIndex,
                    categoryNames,
                    GUILayout.Width(120)
                );
                if (newIndex != categoryIndex)
                {
                    _selectedCategory = (AssetCategory)newIndex;
                    ApplySearch();
                }

                GUILayout.FlexibleSpace();

                // 刷新按钮
                if (
                    GUILayout.Button(
                        EditorGUIUtility.IconContent("Refresh"),
                        EditorStyles.toolbarButton,
                        GUILayout.Width(30)
                    )
                )
                {
                    StartScan(true);
                }

                // 设置按钮
                if (
                    GUILayout.Button(
                        EditorGUIUtility.IconContent("Settings"),
                        EditorStyles.toolbarButton,
                        GUILayout.Width(30)
                    )
                )
                {
                    ShowSettings();
                }

                EditorGUILayout.EndHorizontal();
            }
            catch (Exception e)
            {
                Debug.LogError($"[AssetExplorer] Toolbar 绘制失败: {e.Message}\n{e.StackTrace}");
                EditorGUILayout.EndHorizontal(); // 确保关闭布局组
            }
        }

        #endregion

        #region 主内容区

        private void DrawMainContent()
        {
            EditorGUILayout.BeginHorizontal();

            // 分类面板
            DrawCategoryPanel();

            // 可拖拽分隔条
            GUILayoutExtensions.Splitter(ref _categoryPanelWidth, 100f, 350f, false);

            // 资产列表
            DrawAssetList();

            // 详情面板
            GUILayoutExtensions.Splitter(ref _detailPanelWidth, 150f, 500f, true);

            DrawDetailPanel();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCategoryPanel()
        {
            GUILayout.BeginVertical(GUILayout.Width(_categoryPanelWidth));

            // 面板标题
            EditorGUILayout.LabelField("资源分类", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // 滚动区域
            _categoryScrollPos = EditorGUILayout.BeginScrollView(_categoryScrollPos);

            // 全部
            DrawCategoryItem(
                "全部",
                AssetCategory.All,
                _filteredAssets.Count,
                _selectedCategory == AssetCategory.All
            );

            // 各分类
            foreach (AssetCategory category in Enum.GetValues(typeof(AssetCategory)))
            {
                if (category == AssetCategory.All)
                    continue;

                int count = _categoryCounts.ContainsKey(category) ? _categoryCounts[category] : 0;
                DrawCategoryItem(
                    category.ToString(),
                    category,
                    count,
                    _selectedCategory == category
                );
            }

            EditorGUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        // 优化：使用精确的布局和点击检测，样式改为更可靠的自定义绘制
        private void DrawCategoryItem(string name, AssetCategory category, int count, bool selected)
        {
            // 获取固定高度的布局矩形
            Rect rect = GUILayoutUtility.GetRect(
                GUIContent.none,
                EditorStyles.label,
                GUILayout.Height(24),
                GUILayout.ExpandWidth(true)
            );

            // 背景绘制
            if (selected)
            {
                EditorGUI.DrawRect(rect, new Color(0.3f, 0.5f, 1f, 0.3f));
            }
            else if (Event.current.type == EventType.Repaint)
            {
                // 悬停效果
                if (rect.Contains(Event.current.mousePosition))
                {
                    EditorGUI.DrawRect(rect, new Color(1, 1, 1, 0.08f));
                }
            }

            // 内容区域定义
            Rect iconRect = new Rect(rect.x + 5, rect.y + 4, 16, 16);
            Rect textRect = new Rect(rect.x + 25, rect.y, rect.width - 70, rect.height);
            Rect countRect = new Rect(rect.x + rect.width - 45, rect.y, 40, rect.height);

            // 绘制图标、名称和计数
            Texture2D icon = GetCategoryIcon(category);
            if (icon != null)
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

            GUI.Label(textRect, name, EditorStyles.label);
            GUI.Label(countRect, count.ToString(), EditorStyles.miniLabel);

            // 精确的事件检测：仅在鼠标左键点击时响应
            if (
                Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && rect.Contains(Event.current.mousePosition)
            )
            {
                _selectedCategory = category;
                ApplySearch();
                Event.current.Use();
                Repaint();
            }
        }

        private void DrawAssetList()
        {
            GUILayout.BeginVertical();
            // 标题栏
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(
                $"资产列表 ({_filteredAssets.Count})",
                EditorStyles.boldLabel,
                GUILayout.Width(150)
            );
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("导出列表", EditorStyles.toolbarButton, GUILayout.Width(80)))
                ExportAssetList();
            EditorGUILayout.EndHorizontal();

            _assetScrollPos = EditorGUILayout.BeginScrollView(
                _assetScrollPos,
                GUIStyle.none,
                GUI.skin.verticalScrollbar
            );

            if (_scanner.IsScanning && _filteredAssets.Count == 0)
            {
                EditorGUILayout.LabelField("正在极速扫描...", EditorStyles.centeredGreyMiniLabel);
            }
            else if (_filteredAssets.Count == 0)
            {
                EditorGUILayout.LabelField("未找到匹配的资源", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                // 2. 计算总内容高度：行数 * 行高
                float totalHeight = _filteredAssets.Count * ROW_HEIGHT;

                // 3. 在 ScrollView 内部保留一个巨大的空矩形占位
                // 这样滚动条才会显示正确的大小
                GUILayout.Label("", GUILayout.Height(totalHeight), GUILayout.ExpandWidth(true));

                // 4. 计算当前视口可见区域
                // position.height 是窗口总高度，减去 TopBar (约100像素) 得到列表可见高度
                float visibleHeight = position.height - 100f;
                float scrollY = _assetScrollPos.y;

                // 5. 计算需要绘制的起始和结束索引
                int startIndex = Mathf.FloorToInt(scrollY / ROW_HEIGHT);
                int endIndex = Mathf.Min(
                    _filteredAssets.Count,
                    startIndex + Mathf.CeilToInt(visibleHeight / ROW_HEIGHT) + 2
                ); // 多画2行做缓冲

                // 边界安全检查
                startIndex = Mathf.Max(0, startIndex);

                // 6. 手动绘制可见行 (Manual Layout)
                // 我们不再依赖 GUILayout 的自动堆叠，而是直接指定 Rect
                Rect rowRect = new Rect(0, startIndex * ROW_HEIGHT, position.width, ROW_HEIGHT);

                for (int i = startIndex; i < endIndex; i++)
                {
                    AssetInfo asset = _filteredAssets[i];
                    DrawAssetRowManual(rowRect, asset, i);
                    rowRect.y += ROW_HEIGHT;
                }
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawAssetRowManual(Rect rect, AssetInfo asset, int index)
        {
            // 背景条纹
            if (index % 2 == 0)
                EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.05f));

            // 选中高亮
            if (asset == _selectedAsset)
                EditorGUI.DrawRect(rect, new Color(0.3f, 0.5f, 1f, 0.3f));

            // 鼠标事件处理
            if (
                Event.current.type == EventType.MouseDown
                && rect.Contains(Event.current.mousePosition)
            )
            {
                if (Event.current.button == 0)
                {
                    _selectedAsset = asset;
                    if (Event.current.clickCount == 2)
                        PingAsset(asset);
                    Repaint();
                }
                else if (Event.current.button == 1)
                {
                    ShowAssetContextMenu(asset);
                }
                Event.current.Use();
            }

            // --- 绘制内容 ---

            // 1. Icon (延迟加载)
            Rect iconRect = new Rect(rect.x + 4, rect.y + 3, 16, 16);
            Texture2D icon = GetAssetPreviewIcon(asset);
            if (icon != null)
                GUI.DrawTexture(iconRect, icon);

            // 2. Name
            Rect nameRect = new Rect(rect.x + 25, rect.y, rect.width - 240, rect.height);
            string displayName = asset.isFavorite ? "★ " + asset.name : asset.name;
            GUI.Label(nameRect, displayName, EditorStyles.label);

            // 3. Size
            Rect sizeRect = new Rect(rect.width - 200, rect.y, 70, rect.height);
            GUI.Label(sizeRect, asset.GetFormattedSize(), EditorStyles.miniLabel);

            // 4. Type
            Rect typeRect = new Rect(rect.width - 120, rect.y, 60, rect.height);
            GUI.Label(typeRect, asset.category.ToString(), EditorStyles.miniLabel);

            // 5. Date
            Rect dateRect = new Rect(rect.width - 50, rect.y, 40, rect.height);
            GUI.Label(dateRect, asset.lastModified.ToString("MM-dd"), EditorStyles.miniLabel);
        }

        private void DrawDetailPanel()
        {
            GUILayout.BeginVertical(GUILayout.Width(_detailPanelWidth));

            if (_selectedAsset == null)
            {
                EditorGUILayout.LabelField(
                    "选择一个资产查看详情",
                    EditorStyles.centeredGreyMiniLabel
                );
            }
            else
            {
                DrawAssetDetails(_selectedAsset);
            }

            GUILayout.EndVertical();
        }

        private void DrawAssetDetails(AssetInfo asset)
        {
            // [修复] 使用固定高度而非宽高比，防止垂直方向被撑大
            const float previewHeight = 200f;

            // 创建固定高度的预览区域
            GUILayout.Label("", GUILayout.Height(previewHeight), GUILayout.ExpandWidth(true));
            Rect previewRect = GUILayoutUtility.GetLastRect();

            Texture2D preview = AssetPreview.GetAssetPreview(asset.GetAssetObject());
            if (preview != null)
            {
                GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.DrawRect(previewRect, Color.gray * 0.3f);
                EditorGUI.LabelField(previewRect, "无预览", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.Space(10);

            // 信息
            EditorGUILayout.LabelField("基本信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("名称:", asset.name);
            EditorGUILayout.LabelField("路径:", asset.path, EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("大小:", asset.GetFormattedSize());
            EditorGUILayout.LabelField("类型:", asset.category.ToString());
            EditorGUILayout.LabelField(
                "修改时间:",
                asset.lastModified.ToString("yyyy-MM-dd HH:mm:ss")
            );

            EditorGUILayout.Space(10);

            // 操作按钮
            if (GUILayout.Button("Ping", GUILayout.Height(25)))
            {
                PingAsset(asset);
            }

            if (GUILayout.Button("在资源管理器中显示", GUILayout.Height(25)))
            {
                ShowInExplorer(asset);
            }

            if (GUILayout.Button("复制路径", GUILayout.Height(25)))
            {
                CopyPath(asset);
            }

            if (GUILayout.Button("打开", GUILayout.Height(25)))
            {
                OpenAsset(asset);
            }

            // 收藏
            string favoriteLabel = asset.isFavorite ? "取消收藏" : "收藏";
            if (GUILayout.Button(favoriteLabel, GUILayout.Height(25)))
            {
                ToggleFavorite(asset);
            }
        }

        #endregion

        #region 状态栏

        private void DrawStatusbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 状态信息
            EditorGUILayout.LabelField(_statusText, GUILayout.Width(300));

            GUILayout.FlexibleSpace();

            // 统计
            int totalCount = _scanner.CachedAssets.Count;
            EditorGUILayout.LabelField($"总计: {totalCount}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"显示: {_filteredAssets.Count}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"扫描: {_scanTime:F1}s", GUILayout.Width(80));

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 交互逻辑

        private void HandleKeyboardShortcuts()
        {
            Event evt = Event.current;
            if (evt.type == EventType.KeyDown)
            {
                switch (evt.keyCode)
                {
                    case KeyCode.F5:
                        StartScan(true);
                        evt.Use();
                        break;
                    case KeyCode.Escape:
                        _searchText = "";
                        ApplySearch();
                        evt.Use();
                        break;
                    case KeyCode.F:
                        // 修复3：正确处理Ctrl+F组合键
                        if (evt.control)
                        {
                            GUI.FocusControl("SearchField");
                            evt.Use();
                        }
                        break;
                }
            }
        }

        private void ShowAssetContextMenu(AssetInfo asset)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Ping"), false, () => PingAsset(asset));
            menu.AddItem(new GUIContent("在资源管理器中显示"), false, () => ShowInExplorer(asset));
            menu.AddItem(new GUIContent("复制路径"), false, () => CopyPath(asset));
            menu.AddItem(new GUIContent("复制GUID"), false, () => CopyGUID(asset));
            menu.AddItem(new GUIContent("打开"), false, () => OpenAsset(asset));

            menu.AddSeparator("");

            string favoriteLabel = asset.isFavorite ? "取消收藏" : "收藏";
            menu.AddItem(new GUIContent(favoriteLabel), false, () => ToggleFavorite(asset));

            menu.ShowAsContext();
        }

        #endregion

        #region 扫描管理

        private void StartScan(bool forceFull = false)
        {
            // 清空处理记录
            _pendingAssets.Clear();
            _processedGuids.Clear();
            _previewLoadQueue.Clear();

            _statusText = forceFull ? "正在完整扫描..." : "正在扫描变更...";
            _scanTime = EditorApplication.timeSinceStartup;
            Repaint();

            _ = _scanner.StartScanAsync(forceFull ? false : true);
        }

        // 优化：扫描期间仅缓冲资源，大幅降低Repaint频率
        private void OnAssetFound(AssetInfo asset)
        {
            // 更新分类计数
            _categoryCounts.TryGetValue(asset.category, out int count);
            _categoryCounts[asset.category] = count + 1;

            // 添加到待处理队列，不立即过滤
            _pendingAssets.Enqueue(asset);

            // 大幅降低刷新频率：每200个资源刷新一次
            if (_pendingAssets.Count % 200 == 0)
            {
                Repaint();
            }
        }

        // 新增：扫描完成后批量处理待处理资源
        private void ProcessPendingAssets()
        {
            int processed = 0;
            const int BATCH_SIZE = 100; // 每帧最多处理100个

            while (_pendingAssets.TryDequeue(out AssetInfo asset) && processed < BATCH_SIZE)
            {
                if (_processedGuids.Contains(asset.guid))
                    continue;

                // 执行搜索匹配
                if (_searchEngine.Matches(asset))
                {
                    _filteredAssets.Add(asset);
                }

                _processedGuids.Add(asset.guid);
                processed++;
            }

            if (_pendingAssets.Count == 0)
            {
                // 全部处理完成
                UpdateCategoryCounts();
            }

            Repaint();
        }

        private void OnScanComplete()
        {
            _scanTime = EditorApplication.timeSinceStartup - _scanTime;
            _statusText = $"扫描完成 ({_scanTime:F1}s)";

            Repaint();
        }

        private void OnScanError(string error)
        {
            Debug.LogError(error);
            _statusText = $"扫描错误: {error}";
        }

        private void UpdateCategoryCounts()
        {
            _categoryCounts.Clear();
            // 基于所有已扫描的资源统计
            foreach (AssetInfo asset in _scanner.CachedAssets.Values)
            {
                _categoryCounts.TryGetValue(asset.category, out int count);
                _categoryCounts[asset.category] = count + 1;
            }
        }

        #endregion

        #region 搜索过滤

        private void ApplySearch()
        {
            _searchEngine.SetSearchQuery(_searchText);
            _searchEngine.SetCategoryFilter(_selectedCategory);

            _filteredAssets.Clear();
            _processedGuids.Clear();

            // 批量处理所有资源
            foreach (AssetInfo asset in _scanner.CachedAssets.Values)
            {
                if (_searchEngine.Matches(asset))
                {
                    _filteredAssets.Add(asset);
                }
            }

            UpdateCategoryCounts();
            Repaint();
        }

        #endregion

        #region 资产操作

        private void PingAsset(AssetInfo asset)
        {
            Object obj = asset.GetAssetObject();
            if (obj != null)
            {
                EditorGUIUtility.PingObject(obj);
                Selection.activeObject = obj;
            }
        }

        private void ShowInExplorer(AssetInfo asset)
        {
            EditorUtility.RevealInFinder(asset.path);
        }

        private void CopyPath(AssetInfo asset)
        {
            EditorGUIUtility.systemCopyBuffer = asset.path;
            _statusText = "路径已复制到剪贴板";
        }

        private void CopyGUID(AssetInfo asset)
        {
            EditorGUIUtility.systemCopyBuffer = asset.guid;
            _statusText = "GUID已复制到剪贴板";
        }

        private void OpenAsset(AssetInfo asset)
        {
            Object obj = asset.GetAssetObject();
            if (obj != null)
            {
                AssetDatabase.OpenAsset(obj);
            }
        }

        private void ToggleFavorite(AssetInfo asset)
        {
            asset.isFavorite = !asset.isFavorite;
            if (asset.isFavorite)
            {
                _favorites.Add(asset.path);
            }
            else
            {
                _favorites.Remove(asset.path);
            }
            SaveFavorites();
            Repaint();
        }

        #endregion

        #region 收藏管理

        private void LoadFavorites()
        {
            try
            {
                string saved = EditorPrefs.GetString(PREFS_FAVORITES, "");
                if (!string.IsNullOrEmpty(saved))
                {
                    _favorites.Clear();
                    _favorites.UnionWith(saved.Split('|'));
                }
            }
            catch
            { /* 忽略错误 */
            }
        }

        private void SaveFavorites()
        {
            try
            {
                EditorPrefs.SetString(PREFS_FAVORITES, string.Join("|", _favorites));
            }
            catch
            { /* 忽略错误 */
            }
        }

        #endregion

        #region 导出与设置

        private void ExportAssetList()
        {
            string path = EditorUtility.SaveFilePanel("导出资产列表", "", "asset_list", "csv");
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                using (StreamWriter writer = new StreamWriter(path))
                {
                    writer.WriteLine("名称,路径,分类,大小,修改时间,GUID");

                    foreach (AssetInfo asset in _filteredAssets)
                    {
                        writer.WriteLine(
                            $"\"{asset.name}\",\"{asset.path}\",{asset.category},"
                                + $"\"{asset.GetFormattedSize()}\",\"{asset.lastModified:yyyy-MM-dd HH:mm:ss}\","
                                + $"\"{asset.guid}\""
                        );
                    }
                }

                _statusText = $"已导出到: {path}";
                EditorUtility.RevealInFinder(path);
            }
            catch (Exception ex)
            {
                Debug.LogError($"导出失败: {ex.Message}");
                _statusText = "导出失败";
            }
        }

        private void ShowSettings()
        {
            if (_settingsWindow == null)
            {
                _settingsWindow = new SettingsWindow(_scanner);
            }
            _settingsWindow.Show();
        }

        #endregion

        #region 工具方法

        private Texture2D GetCategoryIcon(AssetCategory category)
        {
            return category switch
            {
                AssetCategory.Textures => EditorGUIUtility.IconContent("Texture Icon").image
                    as Texture2D,
                AssetCategory.Materials => EditorGUIUtility.IconContent("Material Icon").image
                    as Texture2D,
                AssetCategory.Models => EditorGUIUtility.IconContent("Mesh Icon").image
                    as Texture2D,
                AssetCategory.Prefabs => EditorGUIUtility.IconContent("Prefab Icon").image
                    as Texture2D,
                AssetCategory.Scenes => EditorGUIUtility.IconContent("SceneAsset Icon").image
                    as Texture2D,
                AssetCategory.Scripts => EditorGUIUtility.IconContent("cs Script Icon").image
                    as Texture2D,
                AssetCategory.Audio => EditorGUIUtility.IconContent("AudioClip Icon").image
                    as Texture2D,
                AssetCategory.Animations => EditorGUIUtility.IconContent("AnimationClip Icon").image
                    as Texture2D,
                _ => EditorGUIUtility.IconContent("DefaultAsset Icon").image as Texture2D,
            };
        }

        // 优化：使用队列异步加载预览图标，避免阻塞UI线程
        private Texture2D GetAssetPreviewIcon(AssetInfo asset)
        {
            if (asset.previewIcon == null)
            {
                // 如果尚未在加载队列中，加入队列
                if (!_previewLoadQueue.Contains(asset))
                {
                    _previewLoadQueue.Enqueue(asset);
                }
                // 返回默认占位图标
                return EditorGUIUtility.IconContent("DefaultAsset Icon").image as Texture2D;
            }
            return asset.previewIcon;
        }

        // 新增：在Update中调用，异步加载预览图标
        private void LoadPreviewIcon(AssetInfo asset)
        {
            if (asset == null)
                return;

            Object obj = asset.GetAssetObject();
            if (obj != null)
            {
                asset.previewIcon =
                    AssetPreview.GetMiniThumbnail(obj)
                    ?? EditorGUIUtility.IconContent("DefaultAsset Icon").image as Texture2D;
            }
        }

        #endregion
    }

    public class SplitterState
    {
        public float Value { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }

        public SplitterState(float initial, float min, float max)
        {
            Value = initial;
            Min = min;
            Max = max;
        }
    }
}
