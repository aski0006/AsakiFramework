// 文件位置：BatchRenameService.cs

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Asaki.Editor.UI;
using UnityEngine;

namespace Asaki.Editor.Utilities.Tools.BatchRename
{
    public interface IBatchRenameStrategy
    {
        RenameOperation[] GeneratePreview(GameObject[] targets);
    }

    // ==================== 复合策略 ====================

    public class CompositeStrategy : IBatchRenameStrategy
    {
        private readonly List<IBatchRenameStrategy> _strategies;

        public CompositeStrategy(List<IBatchRenameStrategy> strategies)
        {
            _strategies = strategies;
        }

        public RenameOperation[] GeneratePreview(GameObject[] targets)
        {
            if (_strategies.Count == 0)
                return targets.Select(t => new RenameOperation(t, t.name)).ToArray();

            // 链式执行策略
            var ops = _strategies[0].GeneratePreview(targets);
            for (int i = 1; i < _strategies.Count; i++)
            {
                var intermediateTargets = ops.Select(o =>
                    {
                        GameObject go = new GameObject();
                        go.name = o.NewName;
                        return go;
                    })
                    .ToArray();

                var newOps = _strategies[i].GeneratePreview(intermediateTargets);

                // 合并结果
                for (int j = 0; j < ops.Length; j++)
                {
                    ops[j].SetNewName(newOps[j].NewName);
                }

                // 清理临时对象
                foreach (GameObject temp in intermediateTargets)
                    UnityEngine.Object.DestroyImmediate(temp);
            }

            return ops;
        }
    }

    // ==================== 具体策略 ====================

    public class FindReplaceStrategy : IBatchRenameStrategy
    {
        private readonly string _find,
            _replace;
        private readonly bool _useRegex;
        private readonly bool _caseSensitive;

        public FindReplaceStrategy(string find, string replace, bool useRegex, bool caseSensitive)
        {
            _find = find;
            _replace = replace;
            _useRegex = useRegex;
            _caseSensitive = caseSensitive;
        }

        public RenameOperation[] GeneratePreview(GameObject[] targets)
        {
            StringComparison comparison = _caseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            return targets
                .Select(t =>
                {
                    string newName = t.name;
                    if (!string.IsNullOrEmpty(_find))
                    {
                        if (_useRegex)
                        {
                            RegexOptions options = _caseSensitive
                                ? RegexOptions.None
                                : RegexOptions.IgnoreCase;
                            newName = Regex.Replace(t.name, _find, _replace, options);
                        }
                        else
                        {
                            newName = t.name.Replace(_find, _replace, comparison);
                        }
                    }
                    return new RenameOperation(t, newName);
                })
                .ToArray();
        }
    }

    public class TemplateStrategy : IBatchRenameStrategy
    {
        private readonly string _template,
            _prefix,
            _suffix;
        private readonly bool _addSerial;
        private readonly int _start,
            _step,
            _padding;
        private readonly BatchRenameEditorWindow.SerialPosition _pos;

        public TemplateStrategy(
            string template,
            string prefix,
            string suffix,
            bool addSerial,
            int start,
            int step,
            int padding,
            BatchRenameEditorWindow.SerialPosition pos
        )
        {
            _template = string.IsNullOrWhiteSpace(template) ? "{name}" : template;
            _prefix = prefix;
            _suffix = suffix;
            _addSerial = addSerial;
            _start = start;
            _step = step;
            _padding = padding;
            _pos = pos;
        }

        public RenameOperation[] GeneratePreview(GameObject[] targets)
        {
            return targets
                .Select(
                    (t, i) =>
                    {
                        string name = t.name;
                        int serial = _start + i * _step;
                        string serialStr = serial.ToString().PadLeft(_padding, '0');

                        // 执行模板替换
                        string newName = _template
                            .Replace("{name}", name)
                            .Replace("{index}", serialStr)
                            .Replace("{date}", DateTime.Now.ToString("yyyyMMdd"));

                        // 添加前缀/后缀
                        if (!string.IsNullOrEmpty(_prefix))
                            newName = $"{_prefix}{newName}";

                        if (!string.IsNullOrEmpty(_suffix))
                            newName = $"{newName}{_suffix}";

                        // 序列号位置控制
                        if (_addSerial)
                        {
                            newName = _pos switch
                            {
                                BatchRenameEditorWindow.SerialPosition.Prefix =>
                                    $"[{serialStr}]{newName}",
                                BatchRenameEditorWindow.SerialPosition.Suffix =>
                                    $"{newName}[{serialStr}]",
                                BatchRenameEditorWindow.SerialPosition.Replace => serialStr,
                                _ => newName,
                            };
                        }

                        return new RenameOperation(t, newName);
                    }
                )
                .ToArray();
        }
    }

    public class RemoveCharsStrategy : IBatchRenameStrategy
    {
        private readonly string _charsToRemove;

        public RemoveCharsStrategy(string chars)
        {
            _charsToRemove = chars ?? "";
        }

        public RenameOperation[] GeneratePreview(GameObject[] targets)
        {
            return targets
                .Select(t =>
                {
                    string newName = t.name;
                    foreach (char c in _charsToRemove)
                        newName = newName.Replace(c.ToString(), "");
                    return new RenameOperation(t, newName);
                })
                .ToArray();
        }
    }

    public class KeepLastNCharsStrategy : IBatchRenameStrategy
    {
        private readonly int _count;

        public KeepLastNCharsStrategy(int count)
        {
            _count = Mathf.Max(0, count);
        }

        public RenameOperation[] GeneratePreview(GameObject[] targets)
        {
            return targets
                .Select(t =>
                {
                    string newName =
                        _count == 0 ? t.name
                        : t.name.Length <= _count ? t.name
                        : t.name.Substring(t.name.Length - _count);
                    return new RenameOperation(t, newName);
                })
                .ToArray();
        }
    }

    public class CaseConversionStrategy : IBatchRenameStrategy
    {
        private readonly BatchRenameEditorWindow.CaseConversion _conversion;

        public CaseConversionStrategy(BatchRenameEditorWindow.CaseConversion conversion)
        {
            _conversion = conversion;
        }

        public RenameOperation[] GeneratePreview(GameObject[] targets)
        {
            return targets
                .Select(t =>
                {
                    string newName = t.name;
                    newName = _conversion switch
                    {
                        BatchRenameEditorWindow.CaseConversion.Upper => newName.ToUpper(),
                        BatchRenameEditorWindow.CaseConversion.Lower => newName.ToLower(),
                        BatchRenameEditorWindow.CaseConversion.TitleCase =>
                            CultureInfo.CurrentCulture.TextInfo.ToTitleCase(newName),
                        BatchRenameEditorWindow.CaseConversion.PascalCase => ToPascalCase(newName),
                        BatchRenameEditorWindow.CaseConversion.CamelCase => ToCamelCase(newName),
                        _ => newName,
                    };
                    return new RenameOperation(t, newName);
                })
                .ToArray();
        }

        private string ToPascalCase(string input)
        {
            string[] words = input.Split(
                new[] { '_', ' ', '-' },
                StringSplitOptions.RemoveEmptyEntries
            );
            return string.Concat(words.Select(w => char.ToUpper(w[0]) + w.Substring(1).ToLower()));
        }

        private string ToCamelCase(string input)
        {
            string pascal = ToPascalCase(input);
            return char.ToLower(pascal[0]) + pascal.Substring(1);
        }
    }

    // ==================== 冲突检测 ====================

    public static class BatchRenameService
    {
        public static Dictionary<int, ConflictType> DetectConflicts(RenameOperation[] ops)
        {
            var conflicts = new Dictionary<int, ConflictType>();

            // 重复名称检测
            var duplicates = ops.GroupBy(o => o.NewName).Where(g => g.Count() > 1);
            foreach (var group in duplicates)
            foreach (RenameOperation op in group)
                conflicts[op.InstanceId] = ConflictType.DuplicateName;

            // 非法字符检测
            foreach (RenameOperation op in ops)
            {
                if (op.NewName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    conflicts[op.InstanceId] = ConflictType.InvalidCharacters;

                if (op.NewName.Length > 200) // Unity最大名称长度
                    conflicts[op.InstanceId] = ConflictType.NameTooLong;
            }

            return conflicts;
        }
    }
}
