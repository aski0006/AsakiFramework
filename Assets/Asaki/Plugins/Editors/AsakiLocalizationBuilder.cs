using System.Collections.Generic;
using System.IO;
using System.Text;
using Asaki.Core.Configs;
using UnityEditor;
using UnityEngine;

namespace Asaki.Plugins.Editors
{
    public static class AsakiLocalizationBuilder
    {
        [MenuItem("Asaki/Tools/Localization/Build Table (Excel -> Config)")]
        public static void Build()
        {
            // 1. 加载配置
            AsakiConfig config = LoadGlobalConfig();
            if (config == null)
                return;

            AsakiLocalizationConfig locConfig = config.LocalizationConfig;
            string rawPath = locConfig.RawCsvPath;

            // ---------------------------------------------------------
            // 新增功能 1: 自动创建缺失的源文件
            // ---------------------------------------------------------
            if (!File.Exists(rawPath))
            {
                string rawDir = Path.GetDirectoryName(rawPath);
                if (!string.IsNullOrEmpty(rawDir) && !Directory.Exists(rawDir))
                {
                    Directory.CreateDirectory(rawDir);
                }

                // 创建默认模板：Key, 备注, 中文, 英文
                string defaultContent =
                    "Key,Remark,zh-CN,en-US\nUI_EXAMPLE,这是一个示例,示例,Example";
                File.WriteAllText(rawPath, defaultContent, Encoding.UTF8);

                Debug.Log($"[AsakiLoc] ⚠️ Raw CSV not found. Created a new one at: {rawPath}");
                AssetDatabase.Refresh();
                // 创建后继续执行，让它生成对应的 Config 文件
            }

            // 3. 读取源文件 (横向)
            string[] lines = File.ReadAllLines(rawPath);
            if (lines.Length < 2)
            {
                Debug.LogWarning(
                    "[AsakiLoc] Raw CSV is empty or invalid (Needs header + at least 1 row)."
                );
                return;
            }

            // 4. 解析并转换 (横向 -> 纵向)
            string csvContent = ConvertHorizontalToVertical(lines);

            // 5. 写入目标路径
            string outputDir = Path.Combine(
                Application.streamingAssetsPath,
                locConfig.OutputRelativePath
            );
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            string outputPath = Path.Combine(outputDir, $"{locConfig.OutputTableName}.csv");
            File.WriteAllText(outputPath, csvContent, Encoding.UTF8);

            AssetDatabase.Refresh();
            Debug.Log($"[AsakiLoc] ✅ Successfully built localization table at: {outputPath}");
        }

        private static AsakiConfig LoadGlobalConfig()
        {
            string[] guids = AssetDatabase.FindAssets("t:AsakiConfig");
            if (guids.Length == 0)
            {
                Debug.LogError("[AsakiLoc] Could not find any 'AsakiConfig' asset in the project!");
                return null;
            }
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<AsakiConfig>(path);
        }

        private static string ConvertHorizontalToVertical(string[] lines)
        {
            // ---------------------------------------------------------
            // 新增功能 2: 解析 Remark 并调整列偏移
            // ---------------------------------------------------------

            // Header 预期格式: Key, Remark, zh-CN, en-US, ...
            string[] headers = ParseCsvLine(lines[0]);
            var langCodes = new List<string>();

            // 检查表头是否符合规范
            // 第0列是 Key
            // 第1列是 Remark (备注)
            // 从第2列开始是语言代码
            int langStartIndex = 2;

            // 简单的兼容性检查：如果第1列看起来像语言代码（比如只有2个字符），可能用户还没更新CSV格式
            // 这里强制要求第1列是 Remark，开发者需要调整 Excel 格式

            for (int i = langStartIndex; i < headers.Length; i++)
            {
                langCodes.Add(headers[i].Trim());
            }

            StringBuilder sb = new StringBuilder();

            // 写入目标表头，必须与 LocalizationTable.cs 的属性名匹配
            // 注意: LocalizationTable 中属性是 LanguageCode 而不是 Language
            sb.AppendLine("Id,Key,LanguageCode,Content,Remark");

            int uniqueId = 1;

            for (int r = 1; r < lines.Length; r++)
            {
                string rowLine = lines[r];
                if (string.IsNullOrWhiteSpace(rowLine))
                    continue;

                string[] columns = ParseCsvLine(rowLine);

                // 容错处理
                if (columns.Length < 2)
                    continue;

                string key = columns[0].Trim();
                // 获取备注内容，如果列不足则为空
                string remark = columns.Length > 1 ? columns[1].Trim() : "";

                // 遍历这一行的所有语言列
                for (int c = 0; c < langCodes.Count; c++)
                {
                    // 语言列在 columns 中的实际索引 = langStartIndex + c
                    int colIndex = langStartIndex + c;

                    if (colIndex >= columns.Length)
                        break;

                    string content = columns[colIndex].Trim();
                    string lang = langCodes[c];

                    // 写入一行纵向数据
                    // 格式: Id, Key, LanguageCode, Content, Remark
                    sb.Append(uniqueId).Append(",");
                    sb.Append(Escape(key)).Append(",");
                    sb.Append(Escape(lang)).Append(",");
                    sb.Append(Escape(content)).Append(",");
                    sb.Append(Escape(remark)); // 备注对所有语言都是一样的
                    sb.AppendLine();

                    uniqueId++;
                }
            }

            return sb.ToString();
        }

        // 简单的 CSV 行解析器，处理引号内的逗号
        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            StringBuilder current = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    // 处理双引号转义 ("")
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        current.Append('\"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result.ToArray();
        }

        private static string Escape(string val)
        {
            if (string.IsNullOrEmpty(val))
                return "";
            if (val.Contains(",") || val.Contains("\"") || val.Contains("\n") || val.Contains("\r"))
            {
                val = val.Replace("\"", "\"\"");
                return $"\"{val}\"";
            }
            return val;
        }
    }
}
