using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Asaki.Core.Context;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.Logging;
using Asaki.Editor.Utilities.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Asaki.Editor.Debugging
{
    public class AsakiLogDashboard : EditorWindow
    {
        private enum DashboardMode
        {
            Live,
            Local,
        }

        // --- 现代化配色常量 ---
        private static readonly Color ColorBgDark = new Color(0.16f, 0.17f, 0.20f); // VSCode 风格深色背景
        private static readonly Color ColorBgLight = new Color(0.21f, 0.22f, 0.25f);
        private static readonly Color ColorUserCode = new Color(0.30f, 0.62f, 1.0f); // 醒目的蓝色
        private static readonly Color ColorSystemCode = new Color(0.5f, 0.5f, 0.5f);
        private static readonly Color ColorAccentError = new Color(0.95f, 0.35f, 0.35f);
        private static readonly Color ColorAccentWarn = new Color(1.0f, 0.8f, 0.2f);
        private static readonly Color ColorBadgeBg = new Color(1f, 0.3f, 0.3f, 0.8f);

        // UI Elements
        private ListView _listView;
        private ScrollView _stackScrollView;
        private VisualElement _toolbar;
        private Label _statusLabel;

        // Data
        private AsakiLogAggregator _liveAggregator;
        private List<AsakiLogModel> _localLogs = new List<AsakiLogModel>();
        private DashboardMode _mode = DashboardMode.Local;

        // Logic
        private double _lastRefreshTime;
        private double _refreshInterval = 0.05f; // 默认 20fps，可从配置读取

        [MenuItem("Asaki/Window/Log Dashboard", false, 11)]
        public static void ShowWindow()
        {
            AsakiLogDashboard w = GetWindow<AsakiLogDashboard>("Asaki Logs");
            w.minSize = new Vector2(600, 400);
            w.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        // =========================================================
        // UI 构建 (Modern UI Toolkit)
        // =========================================================

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.backgroundColor = ColorBgDark;

            // 1. Toolbar (Top)
            DrawToolbar(root);

            // 2. Split View (Main Content) - 左侧70%，右侧30%
            float leftPaneWidth = position.width * 0.7f;
            TwoPaneSplitView split = new TwoPaneSplitView(
                0,
                leftPaneWidth,
                TwoPaneSplitViewOrientation.Horizontal
            );
            split.style.flexGrow = 1;
            root.Add(split);

            // --- Left Pane: Log List (70%) ---
            _listView = new ListView();
            _listView.style.flexGrow = 1;
            _listView.style.backgroundColor = ColorBgLight;
            _listView.fixedItemHeight = 28;
            _listView.makeItem = MakeLogItem;
            _listView.bindItem = BindLogItem;
            _listView.selectionChanged += OnLogSelected;
            split.Add(_listView);

            // --- Right Pane: Details (30%) ---
            VisualElement rightPane = new VisualElement
            {
                style = { flexGrow = 1, backgroundColor = ColorBgDark },
            };
            _stackScrollView = new ScrollView();
            _stackScrollView.style.SetPadding(20);
            rightPane.Add(_stackScrollView);
            split.Add(rightPane);

            // Initial State
            if (Application.isPlaying)
                _mode = DashboardMode.Live;
            else
                _mode = DashboardMode.Local;
        }

        private void DrawToolbar(VisualElement root)
        {
            _toolbar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    height = 32,
                    backgroundColor = new Color(0.13f, 0.13f, 0.13f),
                    alignItems = Align.Center,
                    paddingLeft = 8,
                    paddingRight = 8,
                    borderBottomWidth = 1,
                    borderBottomColor = new Color(0.1f, 0.1f, 0.1f),
                },
            };

            // Helper to create styled buttons
            Button CreateBtn(string text, System.Action onClick, float width)
            {
                Button btn = new Button(onClick) { text = text };
                btn.style.width = width;
                btn.style.height = 22;
                btn.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
                btn.style.SetBorderRadius(3);
                btn.style.SetBorderWidth(0);
                return btn;
            }

            _toolbar.Add(
                CreateBtn(
                    "Clear",
                    () =>
                    {
                        _liveAggregator?.Clear();
                        _localLogs.Clear();
                        _listView.RefreshItems();
                        _stackScrollView.Clear();
                    },
                    60
                )
            );

            _toolbar.Add(new VisualElement { style = { width = 10 } });

            _toolbar.Add(CreateBtn("📂 History", OpenLogFile, 80));
            _toolbar.Add(new VisualElement { style = { width = 5 } });
            _toolbar.Add(
                CreateBtn(
                    "Show Folder",
                    () =>
                        EditorUtility.RevealInFinder(
                            Path.Combine(Application.persistentDataPath, "Logs")
                        ),
                    90
                )
            );

            _toolbar.Add(new VisualElement { style = { width = 10 } });

            // 导出按钮
            _toolbar.Add(CreateBtn("📤 Export", ExportLogsToTxt, 80));

            // Status Label (Right aligned logic handled by spacer or flex)
            _statusLabel = new Label("Status: Idle")
            {
                style =
                {
                    marginLeft = 15,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = Color.gray,
                },
            };
            _toolbar.Add(_statusLabel);

            root.Add(_toolbar);
        }

        // =========================================================
        // 列表渲染 (List Rendering) - 复合控件
        // =========================================================

        // 创建复杂的列表项结构
        private VisualElement MakeLogItem()
        {
            VisualElement root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignItems = Align.Center;
            root.style.paddingLeft = 5;

            // 1. Color Strip (左侧色条指示等级)
            VisualElement strip = new VisualElement { name = "level-strip" };
            strip.style.width = 4;
            strip.style.height = 18;
            strip.style.marginRight = 6;
            strip.style.SetBorderRadius(2);
            root.Add(strip);

            // 2. Message Label (支持自动换行和展开)
            Label label = new Label { name = "msg-label" };
            label.style.flexGrow = 1;
            label.style.fontSize = 12;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.overflow = Overflow.Hidden;
            label.style.whiteSpace = WhiteSpace.Normal;
            root.Add(label);

            // 3. Count Badge (聚合计数徽章)
            Label badge = new Label { name = "count-badge" };
            badge.style.backgroundColor = ColorBadgeBg;
            badge.style.color = Color.white;
            badge.style.fontSize = 10;
            badge.style.unityFontStyleAndWeight = FontStyle.Bold;
            badge.style.SetPadding(2, 6);
            badge.style.SetBorderRadius(8);
            badge.style.marginRight = 5;
            badge.style.display = DisplayStyle.None;
            root.Add(badge);

            // 4. Time Label
            Label time = new Label { name = "time-label" };
            time.style.fontSize = 11;
            time.style.color = new Color(0.5f, 0.5f, 0.5f);
            time.style.width = 60;
            time.style.unityTextAlign = TextAnchor.MiddleRight;
            root.Add(time);

            return root;
        }

        private void BindLogItem(VisualElement e, int i)
        {
            var list = _mode == DashboardMode.Live ? _liveAggregator?.GetSnapshot() : _localLogs;
            if (list == null || i >= list.Count)
                return;

            AsakiLogModel log = list[i];

            // Get references
            VisualElement strip = e.Q("level-strip");
            Label label = e.Q<Label>("msg-label");
            Label badge = e.Q<Label>("count-badge");
            Label time = e.Q<Label>("time-label");

            // Data Binding
            string message = log.Message;
            label.text = message;
            label.tooltip = message;
            time.text = log.DisplayTime;

            // 根据消息长度动态调整高度
            if (message != null && message.Length > 50)
            {
                e.style.minHeight = 40;
                e.style.maxHeight = 80;
                label.style.whiteSpace = WhiteSpace.Normal;
            }
            else
            {
                e.style.minHeight = StyleKeyword.Null;
                e.style.maxHeight = StyleKeyword.Null;
                label.style.whiteSpace = WhiteSpace.Pre;
            }

            // Badge Logic
            if (log.Count > 1)
            {
                badge.style.display = DisplayStyle.Flex;
                badge.text = log.Count > 999 ? "999+" : log.Count.ToString();
            }
            else
            {
                badge.style.display = DisplayStyle.None;
            }

            // Styling based on Level
            Color stripColor;
            Color textColor;

            switch (log.Level)
            {
                case AsakiLogLevel.Error:
                case AsakiLogLevel.Fatal:
                    stripColor = ColorAccentError;
                    textColor = new Color(1f, 0.6f, 0.6f);
                    break;
                case AsakiLogLevel.Warning:
                    stripColor = ColorAccentWarn;
                    textColor = new Color(1f, 0.9f, 0.5f);
                    break;
                default:
                    stripColor = ColorSystemCode;
                    textColor = new Color(0.8f, 0.8f, 0.8f);
                    break;
            }

            strip.style.backgroundColor = stripColor;
            label.style.color = textColor;
        }

        // =========================================================
        // 详情渲染 (Details & Stack Trace)
        // =========================================================

        private void OnLogSelected(IEnumerable<object> selection)
        {
            AsakiLogModel log = selection.FirstOrDefault() as AsakiLogModel;
            if (log == null)
                return;

            _stackScrollView.Clear();

            // === 1. Header Area ===
            VisualElement header = new VisualElement { style = { marginBottom = 15 } };

            // Level Badge in Header
            Label lvlBadge = new Label(log.Level.ToString().ToUpper())
            {
                style =
                {
                    alignSelf = Align.FlexStart,
                    fontSize = 10,
                    color = Color.black,
                    marginBottom = 5,
                    backgroundColor = GetColorByLevel(log.Level),
                },
            };
            lvlBadge.style.SetPadding(2, 6);
            lvlBadge.style.SetBorderRadius(3);
            header.Add(lvlBadge);

            // Message (支持自动换行和完整显示)
            Label msgLabel = new Label(log.Message)
            {
                style =
                {
                    fontSize = 14,
                    whiteSpace = WhiteSpace.Normal,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new Color(0.9f, 0.9f, 0.9f),
                },
            };
            // 复制菜单
            msgLabel.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 1)
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(
                        new GUIContent("Copy Message"),
                        false,
                        () => EditorGUIUtility.systemCopyBuffer = log.Message
                    );
                    menu.AddItem(
                        new GUIContent("Copy Full JSON"),
                        false,
                        () => EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(log, true)
                    );
                    menu.ShowAsContext();
                }
            });
            header.Add(msgLabel);

            // 添加调用位置信息
            if (!string.IsNullOrEmpty(log.CallerPath))
            {
                Label locationLabel = new Label($"📍 {log.CallerPath}:{log.CallerLine}")
                {
                    style =
                    {
                        fontSize = 11,
                        color = new Color(0.5f, 0.7f, 0.9f),
                        marginTop = 5,
                        whiteSpace = WhiteSpace.Normal,
                    },
                };
                locationLabel.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button == 0)
                    {
                        string sysPath = log.CallerPath.Replace('/', Path.DirectorySeparatorChar);
                        UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(
                            sysPath,
                            log.CallerLine
                        );
                    }
                });
                header.Add(locationLabel);
            }

            _stackScrollView.Add(header);

            // === 2. Payload Area (Code Block Style) ===
            if (!string.IsNullOrEmpty(log.PayloadJson))
            {
                VisualElement pContainer = new VisualElement { style = { marginBottom = 20 } };
                pContainer.Add(
                    new Label("PAYLOAD")
                    {
                        style =
                        {
                            fontSize = 10,
                            color = Color.gray,
                            marginBottom = 4,
                            unityFontStyleAndWeight = FontStyle.Bold,
                        },
                    }
                );

                VisualElement pBox = new VisualElement
                {
                    style =
                    {
                        backgroundColor = new Color(0.12f, 0.12f, 0.12f), // Darker code block
                        borderLeftColor = new Color(0.4f, 0.4f, 0.45f),
                        borderLeftWidth = 2,
                    },
                };
                pBox.style.SetPadding(10);
                pBox.style.SetBorderRadius(4);

                Label pText = new Label(log.PayloadJson)
                {
                    style = { color = new Color(0.6f, 0.8f, 0.6f), whiteSpace = WhiteSpace.Normal },
                }; // Greenish code color
                pBox.Add(pText);
                pContainer.Add(pBox);
                _stackScrollView.Add(pContainer);
            }

            // === 3. Waterfall Stack Trace ===
            _stackScrollView.Add(
                new Label("STACK TRACE")
                {
                    style =
                    {
                        fontSize = 10,
                        color = Color.gray,
                        marginBottom = 10,
                        unityFontStyleAndWeight = FontStyle.Bold,
                    },
                }
            );

            if (log.StackFrames != null)
            {
                for (int i = 0; i < log.StackFrames.Count; i++)
                {
                    RenderStackRow(log.StackFrames[i], i == log.StackFrames.Count - 1);
                }
            }
        }

        /// <summary>
        /// 渲染单行堆栈 (时间线风格)
        /// </summary>
        private void RenderStackRow(StackFrameModel frame, bool isLast)
        {
            VisualElement row = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, minHeight = 26 },
            };

            // --- A. Timeline Graphic ---
            VisualElement timeline = new VisualElement
            {
                style = { width = 24, alignItems = Align.Center },
            };

            // Vertical Line (贯穿线)
            if (!isLast)
            {
                VisualElement line = new VisualElement
                {
                    style =
                    {
                        width = 1,
                        backgroundColor = new Color(0.3f, 0.3f, 0.35f),
                        flexGrow = 1,
                    },
                };
                // 微调位置使其连接圆点
                line.style.marginTop = 0;
                timeline.Add(line);
            }

            // Dot (节点) - 使用 Absolute 定位覆盖在线上
            VisualElement dot = new VisualElement
            {
                style =
                {
                    width = 9,
                    height = 9,
                    position = Position.Absolute,
                    top = 8, // Center vertically roughly
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = ColorBgDark,
                    borderBottomColor = ColorBgDark,
                    borderLeftColor = ColorBgDark,
                    borderRightColor = ColorBgDark, // Stroke effect
                },
            };
            dot.style.SetBorderRadius(4.5f);

            // Color Logic
            if (frame.IsUserCode)
            {
                dot.style.backgroundColor = ColorUserCode;
            }
            else
            {
                dot.style.backgroundColor = ColorSystemCode;
            }

            timeline.Add(dot);
            row.Add(timeline);

            // --- B. Content Interactive Area ---
            Button contentBtn = new Button(() =>
            {
                if (!string.IsNullOrEmpty(frame.FilePath))
                {
                    string sysPath = frame.FilePath.Replace('/', Path.DirectorySeparatorChar);
                    UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(
                        sysPath,
                        frame.LineNumber
                    );
                }
            })
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Column,
                    justifyContent = Justify.Center,
                    backgroundColor = Color.clear,
                    borderTopWidth = 0,
                    borderBottomWidth = 0,
                    borderLeftWidth = 0,
                    borderRightWidth = 0,
                    paddingTop = 2,
                    paddingBottom = 4,
                    paddingLeft = 4,
                    marginLeft = 4,
                },
            };

            // Hover effect logic handled by Unity Button default, but we can customize if needed

            // Method Name
            Label methodLabel = new Label($"{frame.DeclaringType}.{frame.MethodName}")
            {
                style =
                {
                    fontSize = 12,
                    color = frame.IsUserCode
                        ? new Color(0.9f, 0.9f, 0.95f)
                        : new Color(0.55f, 0.55f, 0.6f),
                    unityFontStyleAndWeight = frame.IsUserCode ? FontStyle.Bold : FontStyle.Normal,
                },
            };
            contentBtn.Add(methodLabel);

            // File Path
            if (frame.IsUserCode)
            {
                Label pathLabel = new Label(
                    $"{Path.GetFileName(frame.FilePath)}:{frame.LineNumber}"
                )
                {
                    style =
                    {
                        fontSize = 10,
                        color = new Color(0.35f, 0.55f, 0.75f),
                        marginTop = 1,
                    },
                };
                contentBtn.Add(pathLabel);
            }

            row.Add(contentBtn);
            _stackScrollView.Add(row);
        }

        // =========================================================
        // 核心逻辑 (保持原有功能)
        // =========================================================

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (_statusLabel == null || _listView == null)
                return;

            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                if (_liveAggregator != null)
                {
                    var snapshot = _liveAggregator.GetSnapshot();
                    if (snapshot != null && snapshot.Count > 0)
                        SnapshotLiveLogs();
                }
                _mode = DashboardMode.Local;
                _statusLabel.text = "Status: Local Snapshot";
                _statusLabel.style.color = ColorAccentWarn;
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _mode = DashboardMode.Live;
                if (_localLogs == null)
                    _localLogs = new List<AsakiLogModel>();
                _localLogs.Clear();
                _liveAggregator = null;
                _statusLabel.text = "Status: Connecting...";
                _statusLabel.style.color = Color.gray;
            }
        }

        private void SnapshotLiveLogs()
        {
            if (_liveAggregator == null || _listView == null || _statusLabel == null)
                return;

            var tempSnapshot = _liveAggregator.GetSnapshot();
            _localLogs.Clear();
            if (tempSnapshot.Count > 0)
            {
                LogListWrapper wrapper = new LogListWrapper { List = tempSnapshot };
                string json = JsonUtility.ToJson(wrapper);
                LogListWrapper deserialized = JsonUtility.FromJson<LogListWrapper>(json);
                if (deserialized != null && deserialized.List != null)
                    _localLogs = deserialized.List;
            }
            _listView.itemsSource = _localLogs;
            _listView.Rebuild();
            _statusLabel.text = "Status: Local Snapshot";
            _statusLabel.style.color = ColorAccentWarn;
        }

        private void OnEditorUpdate()
        {
            // 从配置读取刷新间隔
            UpdateRefreshIntervalFromConfig();

            if (_mode == DashboardMode.Local)
                return;

            // 尝试连接聚合器
            if (_liveAggregator == null)
            {
                TryConnectAggregator();
            }

            // 按配置间隔刷新 UI
            if (EditorApplication.timeSinceStartup - _lastRefreshTime > _refreshInterval)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                RefreshLogList();
            }
        }

        /// <summary>
        /// 从配置读取刷新间隔
        /// </summary>
        private void UpdateRefreshIntervalFromConfig()
        {
            if (AsakiContext.TryGet(out AsakiFrameworkSetting config) && config.LogConfig != null)
            {
                _refreshInterval = config.LogConfig.DashboardRefreshInterval;
            }
        }

        /// <summary>
        /// 尝试连接到日志聚合器
        /// </summary>
        private void TryConnectAggregator()
        {
            if (!AsakiContext.TryGet(out IAsakiLoggingService service))
                return;

            if (service is AsakiLoggingService s)
            {
                _liveAggregator = s.Aggregator;
                if (_liveAggregator != null)
                {
                    _listView.itemsSource = _liveAggregator.GetSnapshot();
                    _listView.Rebuild();
                    _statusLabel.text = $"● Live ({(int)(1f / _refreshInterval)}fps)";
                    _statusLabel.style.color = new Color(0.4f, 1f, 0.4f);
                }
            }
        }

        /// <summary>
        /// 刷新日志列表
        /// </summary>
        private void RefreshLogList()
        {
            if (_listView == null || _liveAggregator == null)
                return;

            var snapshot = _liveAggregator.GetSnapshot();
            if (snapshot.Count > 0)
            {
                _listView.RefreshItems();
            }
        }

        private void OpenLogFile()
        {
            string dir = Path.Combine(Application.persistentDataPath, "Logs");
            string path = EditorUtility.OpenFilePanel("Load Asaki Log", dir, "asakilog");
            if (!string.IsNullOrEmpty(path))
            {
                _localLogs = AsakiLogFileReader.LoadFile(path);
                _mode = DashboardMode.Local;
                _listView.itemsSource = _localLogs;
                _listView.Rebuild();
                _statusLabel.text = $"Loaded: {Path.GetFileName(path)}";
                _statusLabel.style.color = new Color(0.4f, 0.8f, 1f);
            }
        }

        private Color GetColorByLevel(AsakiLogLevel level)
        {
            switch (level)
            {
                case AsakiLogLevel.Error:
                    return ColorAccentError;
                case AsakiLogLevel.Warning:
                    return ColorAccentWarn;
                case AsakiLogLevel.Fatal:
                    return Color.red;
                default:
                    return ColorSystemCode;
            }
        }

        /// <summary>
        /// 导出日志到TXT文件
        /// </summary>
        private void ExportLogsToTxt()
        {
            var logs = _mode == DashboardMode.Live ? _liveAggregator?.GetSnapshot() : _localLogs;
            if (logs == null || logs.Count == 0)
            {
                EditorUtility.DisplayDialog("Export Logs", "No logs to export.", "OK");
                return;
            }

            // 选择保存路径
            string defaultName = $"AsakiLog_Export_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string defaultDir = Path.Combine(Application.persistentDataPath, "Logs");
            if (!Directory.Exists(defaultDir))
                Directory.CreateDirectory(defaultDir);

            string savePath = EditorUtility.SaveFilePanel(
                "Export Logs",
                defaultDir,
                defaultName,
                "txt"
            );

            if (string.IsNullOrEmpty(savePath))
                return;

            try
            {
                ExportLogsToFile(logs, savePath);
                EditorUtility.RevealInFinder(savePath);
                _statusLabel.text = $"Exported: {Path.GetFileName(savePath)}";
                _statusLabel.style.color = new Color(0.4f, 1f, 0.4f);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(
                    "Export Error",
                    $"Failed to export logs: {ex.Message}",
                    "OK"
                );
                Debug.LogError($"[AsakiLogDashboard] Export failed: {ex}");
            }
        }

        /// <summary>
        /// 将日志列表导出到文件
        /// </summary>
        private void ExportLogsToFile(List<AsakiLogModel> logs, string filePath)
        {
            using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            {
                // 写入文件头
                writer.WriteLine(
                    "╔════════════════════════════════════════════════════════════════╗"
                );
                writer.WriteLine(
                    "║                    ASAKI LOG EXPORT REPORT                     ║"
                );
                writer.WriteLine(
                    "╚════════════════════════════════════════════════════════════════╝"
                );
                writer.WriteLine();
                writer.WriteLine($"Export Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                writer.WriteLine($"Total Entries: {logs.Count}");
                writer.WriteLine($"Total Occurrences: {logs.Sum(l => l.Count)}");
                writer.WriteLine();
                writer.WriteLine(new string('═', 80));
                writer.WriteLine();

                // 按级别分组统计
                var levelGroups = logs.GroupBy(l => l.Level).OrderBy(g => (int)g.Key);
                writer.WriteLine("📊 SUMMARY BY LEVEL:");
                writer.WriteLine();
                writer.WriteLine($"{"Level", -10} {"Count", -10} {"Occurrences", -15}");
                writer.WriteLine(new string('-', 35));
                foreach (var group in levelGroups)
                {
                    string levelName = group.Key.ToString();
                    int count = group.Count();
                    int occurrences = group.Sum(l => l.Count);
                    writer.WriteLine($"{levelName, -10} {count, -10} {occurrences, -15}");
                }
                writer.WriteLine();
                writer.WriteLine(new string('═', 80));
                writer.WriteLine();

                // 写入详细日志
                int index = 1;
                foreach (var log in logs)
                {
                    string levelIcon = GetLevelIcon(log.Level);
                    string time = new DateTime(log.LastTimestamp)
                        .ToLocalTime()
                        .ToString("yyyy-MM-dd HH:mm:ss.fff");

                    writer.WriteLine(
                        $"[{index++}] {levelIcon} [{log.Level.ToString().ToUpper()}] {time}"
                    );
                    writer.WriteLine($"    Message: {log.Message}");

                    if (!string.IsNullOrEmpty(log.PayloadJson))
                    {
                        writer.WriteLine($"    Payload: {log.PayloadJson}");
                    }

                    writer.WriteLine($"    Location: {log.CallerPath}:{log.CallerLine}");

                    if (log.Count > 1)
                    {
                        writer.WriteLine($"    Occurrences: {log.Count} times");
                    }

                    // 写入堆栈信息
                    if (log.StackFrames != null && log.StackFrames.Count > 0)
                    {
                        writer.WriteLine("    Stack Trace:");
                        foreach (var frame in log.StackFrames)
                        {
                            string userCodeMarker = frame.IsUserCode ? "→" : " ";
                            writer.WriteLine(
                                $"      {userCodeMarker} {frame.DeclaringType}.{frame.MethodName}"
                            );
                            if (frame.IsUserCode && !string.IsNullOrEmpty(frame.FilePath))
                            {
                                writer.WriteLine(
                                    $"          at {frame.FilePath}:{frame.LineNumber}"
                                );
                            }
                        }
                    }

                    writer.WriteLine();
                }

                writer.WriteLine(new string('═', 80));
                writer.WriteLine($"End of Report - {logs.Count} unique log entries");
                writer.WriteLine($"Generated by Asaki Log Dashboard");
            }
        }

        /// <summary>
        /// 获取日志级别对应的图标
        /// </summary>
        private string GetLevelIcon(AsakiLogLevel level)
        {
            return level switch
            {
                AsakiLogLevel.Debug => "🔍",
                AsakiLogLevel.Info => "ℹ️",
                AsakiLogLevel.Warning => "⚠️",
                AsakiLogLevel.Error => "❌",
                AsakiLogLevel.Fatal => "💀",
                _ => "❓",
            };
        }

        [System.Serializable]
        private class LogListWrapper
        {
            public List<AsakiLogModel> List;
        }
    }
}
