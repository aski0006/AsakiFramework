// File: Asaki/Unity/Configuration/AsakiCsvUtils.cs

using System.Collections.Generic;
using System.Text;

namespace Asaki.Unity.Utils
{
    public static class AsakiCsvUtils
    {
        /// <summary>
        /// 解析 CSV 行，处理引号包裹的情况 (e.g. "Weapon, Fire", 100)
        /// </summary>
        public static string[] ParseLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return new string[0];

            var result = new List<string>();
            StringBuilder current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    // 检查是否是转义引号 (两个双引号 "")
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // 跳过下一个引号
                    }
                    else
                    {
                        inQuotes = !inQuotes; // 切换状态
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    // 分隔符
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString().Trim());
            return result.ToArray();
        }
    }
}
