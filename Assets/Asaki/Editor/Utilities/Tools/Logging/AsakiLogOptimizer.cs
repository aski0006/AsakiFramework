using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Asaki.Editor.Utilities.Tools.Logging
{
    /// <summary>
    /// 日志瘦身核心算法
    /// <para>原理：合并分散的 $INC 指令，将 N 条增量记录压缩为 1 条。</para>
    /// </summary>
    public static class AsakiLogOptimizer
    {
        public enum OutputFormat
        {
            /// <summary>保持原始 . asakilog 格式（机器可读）</summary>
            Original,

            /// <summary>纯文本格式（人类可读）</summary>
            PlainText,

            /// <summary>Markdown 格式（文档友好）</summary>
            Markdown,
        }

        public struct OptimizationResult
        {
            public int OriginalLines;
            public int OptimizedLines;
            public long OriginalSize;
            public long OptimizedSize;
            public string OutputPath;
            public int TotalLogEntries;
            public int TotalOccurrences;
        }

        /// <summary>
        /// 解析后的日志条目
        /// </summary>
        private class LogEntry
        {
            public int ID;
            public int Level;
            public long Timestamp;
            public string Message;
            public string Payload;
            public string FilePath;
            public int Line;
            public string StackJson;
            public int TotalCount;

            public string LevelName
            {
                get
                {
                    // 根据 AsakiLogLevel 枚举映射
                    switch (Level)
                    {
                        case 0:
                            return "Debug";
                        case 1:
                            return "Trace";
                        case 2:
                            return "Info";
                        case 3:
                            return "Warning";
                        case 4:
                            return "Error";
                        case 5:
                            return "Fatal";
                        default:
                            return "Unknown";
                    }
                }
            }

            public string TimeString
            {
                get
                {
                    try
                    {
                        DateTime dt = new DateTime(Timestamp);
                        return dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss. fff");
                    }
                    catch
                    {
                        return "Unknown Time";
                    }
                }
            }

            public string LocationString => $"{FilePath}:{Line}";
        }

        public static OptimizationResult Process(
            string srcPath,
            OutputFormat format = OutputFormat.Original
        )
        {
            if (!File.Exists(srcPath))
                throw new FileNotFoundException(srcPath);

            OptimizationResult result = new OptimizationResult
            {
                OriginalSize = new FileInfo(srcPath).Length,
                OriginalLines = 0,
            };

            // === 1. 内存聚合 (Memory Aggregation) ===
            var idOrder = new List<int>();
            var logEntries = new Dictionary<int, LogEntry>();
            StringBuilder headers = new StringBuilder();

            using (StreamReader reader = new StreamReader(srcPath, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    result.OriginalLines++;

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // 保留头部元数据
                    if (line.StartsWith("#"))
                    {
                        headers.AppendLine(line);
                        continue;
                    }

                    // 处理定义行
                    if (line.StartsWith("$DEF|"))
                    {
                        LogEntry entry = ParseDefLine(line);
                        if (entry != null && !logEntries.ContainsKey(entry.ID))
                        {
                            idOrder.Add(entry.ID);
                            logEntries[entry.ID] = entry;
                        }
                    }
                    // 处理增量行
                    else if (line.StartsWith("$INC|"))
                    {
                        (int id, int inc) = ParseIncLine(line);
                        if (logEntries.ContainsKey(id))
                        {
                            logEntries[id].TotalCount += inc;
                        }
                    }
                }
            }

            // 统计信息
            result.TotalLogEntries = logEntries.Count;
            result.TotalOccurrences = logEntries.Values.Sum(e => e.TotalCount);

            // === 2. 根据格式写入新文件 ===
            string destPath = GenerateOutputPath(srcPath, format);

            switch (format)
            {
                case OutputFormat.Original:
                    WriteOriginalFormat(
                        destPath,
                        headers.ToString(),
                        idOrder,
                        logEntries,
                        ref result
                    );
                    break;
                case OutputFormat.PlainText:
                    WritePlainTextFormat(
                        destPath,
                        headers.ToString(),
                        idOrder,
                        logEntries,
                        ref result
                    );
                    break;
                case OutputFormat.Markdown:
                    WriteMarkdownFormat(
                        destPath,
                        headers.ToString(),
                        srcPath,
                        idOrder,
                        logEntries,
                        ref result
                    );
                    break;
            }

            result.OutputPath = destPath;
            result.OptimizedSize = new FileInfo(destPath).Length;
            return result;
        }

        #region Parsing

        private static LogEntry ParseDefLine(string line)
        {
            try
            {
                // 格式:  $DEF|ID|Level|Timestamp|Message|Payload|Path:Line|StackJson
                string[] parts = line.Split('|');
                if (parts.Length < 7)
                    return null;

                LogEntry entry = new LogEntry
                {
                    ID = int.Parse(parts[1]),
                    Level = int.Parse(parts[2]),
                    Timestamp = long.Parse(parts[3]),
                    Message = parts[4].Replace("¦", "|"), // 还原转义
                    Payload = parts.Length > 5 ? parts[5].Replace("¦", "|") : "",
                    TotalCount = 1,
                };

                // 解析 Path:Line
                if (parts.Length > 6)
                {
                    string[] caller = parts[6].Split(':');
                    entry.FilePath = caller.Length > 0 ? caller[0] : "";
                    entry.Line =
                        caller.Length > 1 && int.TryParse(caller[1], out int lineNum) ? lineNum : 0;
                }

                // 堆栈信息
                entry.StackJson = parts.Length > 7 ? parts[7].Replace("¦", "|") : "";

                return entry;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LogOptimizer] Failed to parse DEF line: {ex.Message}");
                return null;
            }
        }

        private static (int id, int inc) ParseIncLine(string line)
        {
            try
            {
                string[] parts = line.Split('|');
                if (parts.Length >= 3)
                {
                    int id = int.Parse(parts[1]);
                    int inc = int.Parse(parts[2]);
                    return (id, inc);
                }
            }
            catch { }
            return (0, 0);
        }

        #endregion

        #region Output Formats

        private static string GenerateOutputPath(string srcPath, OutputFormat format)
        {
            string dir = Path.GetDirectoryName(srcPath);
            string fileName = Path.GetFileNameWithoutExtension(srcPath);

            string suffix = format switch
            {
                OutputFormat.Original => "_Optimized. asakilog",
                OutputFormat.PlainText => "_Readable.txt",
                OutputFormat.Markdown => "_Report.md",
                _ => "_Output.txt",
            };

            return Path.Combine(dir, $"{fileName}{suffix}");
        }

        private static void WriteOriginalFormat(
            string destPath,
            string headers,
            List<int> idOrder,
            Dictionary<int, LogEntry> entries,
            ref OptimizationResult result
        )
        {
            using (StreamWriter writer = new StreamWriter(destPath, false, Encoding.UTF8))
            {
                writer.Write(headers);

                foreach (int id in idOrder)
                {
                    if (!entries.TryGetValue(id, out LogEntry entry))
                        continue;

                    // 重建 $DEF 行
                    string defLine =
                        $"$DEF|{entry.ID}|{entry.Level}|{entry.Timestamp}|"
                        + $"{entry.Message.Replace("|", "¦")}|{entry.Payload.Replace("|", "¦")}|"
                        + $"{entry.FilePath}:{entry.Line}|{entry.StackJson.Replace("|", "¦")}";
                    writer.WriteLine(defLine);
                    result.OptimizedLines++;

                    // 写合并后的 $INC
                    if (entry.TotalCount > 1)
                    {
                        writer.WriteLine($"$INC|{entry.ID}|{entry.TotalCount - 1}");
                        result.OptimizedLines++;
                    }
                }
            }
        }

        private static void WritePlainTextFormat(
            string destPath,
            string headers,
            List<int> idOrder,
            Dictionary<int, LogEntry> entries,
            ref OptimizationResult result
        )
        {
            using (StreamWriter writer = new StreamWriter(destPath, false, Encoding.UTF8))
            {
                // 写入头部信息
                writer.WriteLine(
                    "╔════════════════════════════════════════════════════════════════╗"
                );
                writer.WriteLine(
                    "║          ASAKI LOG REPORT (Plain Text Format)                  ║"
                );
                writer.WriteLine(
                    "╚════════════════════════════════════════════════════════════════╝"
                );
                writer.WriteLine();
                writer.WriteLine(headers.ToString().TrimEnd());
                writer.WriteLine($"# Total Entries: {entries.Count}");
                writer.WriteLine($"# Total Occurrences:  {entries.Values.Sum(e => e.TotalCount)}");
                writer.WriteLine();
                writer.WriteLine(new string('═', 80));
                writer.WriteLine();

                int index = 1;
                foreach (int id in idOrder)
                {
                    if (!entries.TryGetValue(id, out LogEntry entry))
                        continue;

                    // 根据日志级别选择符号
                    string levelSymbol = GetLevelSymbol(entry.Level);

                    writer.WriteLine(
                        $"[{index++}] {levelSymbol} [{entry.LevelName.ToUpper()}] {entry.TimeString}"
                    );
                    writer.WriteLine($"    Message: {entry.Message}");

                    if (!string.IsNullOrEmpty(entry.Payload))
                    {
                        writer.WriteLine($"    Payload: {entry.Payload}");
                    }

                    writer.WriteLine($"    Location: {entry.LocationString}");

                    if (entry.TotalCount > 1)
                    {
                        writer.WriteLine($"    ⚠ Occurrences: {entry.TotalCount} times");
                    }

                    // 解析并显示堆栈（如果有）
                    if (!string.IsNullOrEmpty(entry.StackJson) && entry.StackJson != "{}")
                    {
                        writer.WriteLine($"    Stack Trace:");
                        WriteStackTrace(writer, entry.StackJson);
                    }

                    writer.WriteLine();
                    result.OptimizedLines += 5;
                }

                writer.WriteLine(new string('═', 80));
                writer.WriteLine($"End of Report - {entries.Count} unique log entries");
            }
        }

        private static void WriteMarkdownFormat(
            string destPath,
            string headers,
            string srcPath,
            List<int> idOrder,
            Dictionary<int, LogEntry> entries,
            ref OptimizationResult result
        )
        {
            using (StreamWriter writer = new StreamWriter(destPath, false, Encoding.UTF8))
            {
                // Markdown 头部
                writer.WriteLine("# 📋 Asaki Log Analysis Report");
                writer.WriteLine();
                writer.WriteLine($"**Source File:** `{Path.GetFileName(srcPath)}`  ");
                writer.WriteLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}  ");
                writer.WriteLine($"**Total Entries:** {entries.Count}  ");
                writer.WriteLine(
                    $"**Total Occurrences:** {entries.Values.Sum(e => e.TotalCount)}  "
                );
                writer.WriteLine();

                // 统计汇总
                writer.WriteLine("## 📊 Summary Statistics");
                writer.WriteLine();
                var levelGroups = entries.Values.GroupBy(e => e.LevelName).OrderBy(g => g.Key);
                writer.WriteLine("| Level | Count | Total Occurrences |");
                writer.WriteLine("|-------|-------|-------------------|");
                foreach (var group in levelGroups)
                {
                    int count = group.Count();
                    int occurrences = group.Sum(e => e.TotalCount);
                    string emoji = GetLevelEmoji(group.First().Level);
                    writer.WriteLine($"| {emoji} {group.Key} | {count} | {occurrences} |");
                }
                writer.WriteLine();

                // 详细日志
                writer.WriteLine("## 📝 Detailed Logs");
                writer.WriteLine();

                foreach (int id in idOrder)
                {
                    if (!entries.TryGetValue(id, out LogEntry entry))
                        continue;

                    string emoji = GetLevelEmoji(entry.Level);
                    string badge = entry.TotalCount > 1 ? $" `×{entry.TotalCount}`" : "";

                    writer.WriteLine($"### {emoji} [{entry.LevelName}] {entry.Message}{badge}");
                    writer.WriteLine();
                    writer.WriteLine($"- **Time:** {entry.TimeString}");
                    writer.WriteLine($"- **Location:** `{entry.LocationString}`");

                    if (!string.IsNullOrEmpty(entry.Payload))
                    {
                        writer.WriteLine($"- **Payload:**");
                        writer.WriteLine($"  ```json");
                        writer.WriteLine($"  {entry.Payload}");
                        writer.WriteLine($"  ```");
                    }

                    if (!string.IsNullOrEmpty(entry.StackJson) && entry.StackJson != "{}")
                    {
                        writer.WriteLine($"- **Stack Trace:**");
                        WriteStackTraceMarkdown(writer, entry.StackJson);
                    }

                    writer.WriteLine();
                    writer.WriteLine("---");
                    writer.WriteLine();
                    result.OptimizedLines += 8;
                }

                writer.WriteLine($"*End of Report - Generated by Asaki Log Optimizer*");
            }
        }

        #endregion

        #region Helpers

        private static string GetLevelSymbol(int level)
        {
            return level switch
            {
                0 => "🔍", // Debug
                1 => "📍", // Trace
                2 => "ℹ️", // Info
                3 => "⚠️", // Warning
                4 => "❌", // Error
                5 => "💀", // Fatal
                _ => "❓",
            };
        }

        private static string GetLevelEmoji(int level)
        {
            return level switch
            {
                0 => "🔍",
                1 => "📍",
                2 => "ℹ️",
                3 => "⚠️",
                4 => "❌",
                5 => "💀",
                _ => "❓",
            };
        }

        private static void WriteStackTrace(StreamWriter writer, string stackJson)
        {
            try
            {
                // 简单解析堆栈 JSON (不依赖 JsonUtility，因为是 Editor 工具)
                if (stackJson.Contains("\"F\":["))
                {
                    // 提取堆栈帧信息（简化处理）
                    writer.WriteLine($"        (Stack available - see raw data for details)");
                }
            }
            catch
            {
                writer.WriteLine($"        (Unable to parse stack trace)");
            }
        }

        private static void WriteStackTraceMarkdown(StreamWriter writer, string stackJson)
        {
            try
            {
                if (stackJson.Contains("\"F\":["))
                {
                    writer.WriteLine($"  ```");
                    writer.WriteLine($"  {stackJson}");
                    writer.WriteLine($"  ```");
                }
            }
            catch { }
        }

        #endregion
    }
}
