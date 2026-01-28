using System.IO;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Utilities.Tools.Logging
{
    public class AsakiLogToolWindow : EditorWindow
    {
        private string _selectedFilePath;
        private AsakiLogOptimizer.OptimizationResult? _lastResult;
        private string _message;
        private MessageType _messageType;
        private AsakiLogOptimizer.OutputFormat _outputFormat = AsakiLogOptimizer
            .OutputFormat
            .PlainText;

        [MenuItem("Asaki/Tools/Log Optimizer", false, 101)]
        public static void ShowWindow()
        {
            GetWindow<AsakiLogToolWindow>("Log Optimizer").Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("🔧 Asaki Log File Optimizer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Optimize and convert Asaki log files into readable formats.\n"
                    + "Original: Keep . asakilog format (machine readable)\n"
                    + "PlainText: Human-friendly text format\n"
                    + "Markdown: Documentation-ready report",
                MessageType.Info
            );
            GUILayout.Space(10);

            // === 文件选择区域 ===
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Target Log:");
                if (
                    GUILayout.Button(
                        string.IsNullOrEmpty(_selectedFilePath)
                            ? "Select .asakilog..."
                            : Path.GetFileName(_selectedFilePath),
                        EditorStyles.objectField
                    )
                )
                {
                    string path = EditorUtility.OpenFilePanel(
                        "Select Log File",
                        Application.persistentDataPath,
                        "asakilog"
                    );
                    if (!string.IsNullOrEmpty(path))
                    {
                        _selectedFilePath = path;
                        _lastResult = null;
                        _message = "";
                    }
                }
            }

            if (!string.IsNullOrEmpty(_selectedFilePath))
            {
                EditorGUILayout.LabelField("Path:", _selectedFilePath, EditorStyles.miniLabel);
            }

            GUILayout.Space(10);

            // === 输出格式选择 ===
            EditorGUILayout.LabelField("Output Format:", EditorStyles.boldLabel);
            _outputFormat = (AsakiLogOptimizer.OutputFormat)
                EditorGUILayout.EnumPopup("Format", _outputFormat);

            // 格式说明
            string formatDesc = _outputFormat switch
            {
                AsakiLogOptimizer.OutputFormat.Original =>
                    "📦 Optimized . asakilog (for programmatic reading)",
                AsakiLogOptimizer.OutputFormat.PlainText =>
                    "📄 Human-readable . txt with emojis and formatting",
                AsakiLogOptimizer.OutputFormat.Markdown =>
                    "📋 Markdown report (. md) with tables and statistics",
                _ => "",
            };
            EditorGUILayout.HelpBox(formatDesc, MessageType.None);

            GUILayout.Space(15);

            // === 操作区域 ===
            GUI.enabled =
                !string.IsNullOrEmpty(_selectedFilePath) && File.Exists(_selectedFilePath);

            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);

            if (GUILayout.Button("🚀 Start Optimization", GUILayout.Height(35)))
            {
                try
                {
                    _lastResult = AsakiLogOptimizer.Process(_selectedFilePath, _outputFormat);
                    _message =
                        $"✅ Optimization Complete!\nOutput: {Path.GetFileName(_lastResult.Value.OutputPath)}";
                    _messageType = MessageType.Info;
                    EditorUtility.RevealInFinder(_lastResult.Value.OutputPath);
                }
                catch (System.Exception ex)
                {
                    _message = $"❌ Error:  {ex.Message}";
                    _messageType = MessageType.Error;
                    Debug.LogException(ex);
                }
            }

            GUI.backgroundColor = originalColor;
            GUI.enabled = true;

            GUILayout.Space(10);

            // === 结果展示区域 ===
            if (!string.IsNullOrEmpty(_message))
            {
                EditorGUILayout.HelpBox(_message, _messageType);
            }

            if (_lastResult.HasValue)
            {
                AsakiLogOptimizer.OptimizationResult r = _lastResult.Value;
                GUILayout.Space(10);

                EditorGUILayout.LabelField("📊 Optimization Results", EditorStyles.boldLabel);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawStat("Lines", r.OriginalLines, r.OptimizedLines);
                    DrawStat("Size", FormatSize(r.OriginalSize), FormatSize(r.OptimizedSize));
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField($"Log Entries: {r.TotalLogEntries}");
                    EditorGUILayout.LabelField($"Total Occurrences: {r.TotalOccurrences}");
                }

                float compressRatio = 1f - (float)r.OptimizedSize / r.OriginalSize;
                Color boxColor =
                    compressRatio > 0.5f
                        ? new Color(0.3f, 0.8f, 0.4f, 0.2f)
                        : new Color(0.8f, 0.8f, 0.3f, 0.2f);

                GUI.backgroundColor = boxColor;
                EditorGUILayout.HelpBox(
                    $"💾 Compression Ratio: {compressRatio:P2}",
                    MessageType.None
                );
                GUI.backgroundColor = originalColor;

                GUILayout.Space(5);

                if (GUILayout.Button("📂 Open Output Folder"))
                {
                    EditorUtility.RevealInFinder(r.OutputPath);
                }
            }
        }

        private void DrawStat(string label, object original, object optimized)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(60));
                GUILayout.Label($"{original}", EditorStyles.label, GUILayout.Width(80));
                GUILayout.Label("→", GUILayout.Width(20));
                GUILayout.Label($"{optimized}", EditorStyles.boldLabel);
            }
        }

        private string FormatSize(long bytes)
        {
            if (bytes < 1024)
                return bytes + " B";
            if (bytes < 1024 * 1024)
                return (bytes / 1024f).ToString("F2") + " KB";
            return (bytes / 1024f / 1024f).ToString("F2") + " MB";
        }
    }
}
