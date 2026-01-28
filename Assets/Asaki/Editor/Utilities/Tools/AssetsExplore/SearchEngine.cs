using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Asaki.Editor.Utilities.Tools.AssetsExplore
{
    public class SearchEngine
    {
        public enum SearchMode
        {
            Fuzzy, // 模糊搜索
            Exact, // 精确匹配
            Regex, // 正则表达式
            Path, // 路径搜索
        }

        // 修复3：使字段可访问
        public SearchMode _mode = SearchMode.Fuzzy;
        private string _searchQuery = "";
        private AssetCategory _categoryFilter = AssetCategory.All;

        // 保存的搜索条件
        private readonly List<string> _savedSearches = new List<string>();
        private const string PREFS_KEY = "AssetExplorer_SavedSearches";

        public SearchEngine()
        {
            LoadSavedSearches();
        }

        public void SetSearchQuery(string query, SearchMode mode = SearchMode.Fuzzy)
        {
            _searchQuery = query?.ToLowerInvariant() ?? "";
            _mode = mode;
        }

        public void SetCategoryFilter(AssetCategory category)
        {
            _categoryFilter = category;
        }

        public bool Matches(AssetInfo asset)
        {
            if (asset == null)
                return false;

            // 分类过滤
            if (_categoryFilter != AssetCategory.All && asset.category != _categoryFilter)
                return false;

            // 空搜索词返回全部
            if (string.IsNullOrEmpty(_searchQuery))
                return true;

            return _mode switch
            {
                SearchMode.Fuzzy => FuzzyMatch(asset),
                SearchMode.Exact => ExactMatch(asset),
                SearchMode.Regex => RegexMatch(asset),
                SearchMode.Path => PathMatch(asset),
                _ => false,
            };
        }

        private bool FuzzyMatch(AssetInfo asset)
        {
            string[] searchParts = _searchQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string targetText = $"{asset.name} {asset.extension} {asset.path}".ToLowerInvariant();

            return searchParts.All(part => targetText.Contains((string)part));
        }

        private bool ExactMatch(AssetInfo asset)
        {
            return asset.name.Equals(_searchQuery, StringComparison.OrdinalIgnoreCase)
                || asset.extension.Equals(_searchQuery, StringComparison.OrdinalIgnoreCase);
        }

        private bool RegexMatch(AssetInfo asset)
        {
            try
            {
                return Regex.IsMatch(asset.name, _searchQuery, RegexOptions.IgnoreCase)
                    || Regex.IsMatch(asset.path, _searchQuery, RegexOptions.IgnoreCase);
            }
            catch
            {
                return false; // 无效正则
            }
        }

        private bool PathMatch(AssetInfo asset)
        {
            return asset.path.ToLowerInvariant().Contains(_searchQuery);
        }

        #region 保存搜索

        public void SaveCurrentSearch()
        {
            if (!string.IsNullOrWhiteSpace(_searchQuery) && !_savedSearches.Contains(_searchQuery))
            {
                _savedSearches.Add(_searchQuery);
                if (_savedSearches.Count > 10) // 限制数量
                    _savedSearches.RemoveAt(0);

                SaveSavedSearches();
            }
        }

        public IReadOnlyList<string> GetSavedSearches()
        {
            return _savedSearches;
        }

        private void LoadSavedSearches()
        {
            try
            {
                string saved = EditorPrefs.GetString(PREFS_KEY, "");
                if (!string.IsNullOrEmpty(saved))
                {
                    _savedSearches.AddRange(
                        saved.Split('|', StringSplitOptions.RemoveEmptyEntries)
                    );
                }
            }
            catch
            { /* 忽略加载错误 */
            }
        }

        private void SaveSavedSearches()
        {
            try
            {
                EditorPrefs.SetString(PREFS_KEY, string.Join("|", _savedSearches));
            }
            catch
            { /* 忽略保存错误 */
            }
        }

        #endregion
    }
}
