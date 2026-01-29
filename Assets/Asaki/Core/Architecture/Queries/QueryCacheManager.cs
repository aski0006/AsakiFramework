using System;
using System.Collections.Generic;

namespace Asaki.Core.Architecture.Queries
{
    /// <summary>
    /// Query 结果缓存管理器
    /// 用于缓存频繁查询的结果
    /// </summary>
    internal class QueryCacheManager
    {
        // 缓存条目
        private class CacheEntry
        {
            public object Result;
            public float ExpireTime; // 过期时间（Unity Time.time）
        }

        private readonly Dictionary<string, CacheEntry> _cache =
            new Dictionary<string, CacheEntry>();
        private readonly object _lock = new object();

        /// <summary>
        /// 尝试从缓存获取结果
        /// </summary>
        public bool TryGetCache<TResult>(string key, out TResult result)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var entry))
                {
                    // 检查是否过期
                    if (UnityEngine.Time.time < entry.ExpireTime)
                    {
                        result = (TResult)entry.Result;
                        return true;
                    }
                    else
                    {
                        // 过期则移除
                        _cache.Remove(key);
                    }
                }
            }

            result = default;
            return false;
        }

        /// <summary>
        /// 设置缓存
        /// </summary>
        public void SetCache<TResult>(string key, TResult result, float cacheSeconds)
        {
            lock (_lock)
            {
                _cache[key] = new CacheEntry
                {
                    Result = result,
                    ExpireTime = UnityEngine.Time.time + cacheSeconds,
                };
            }
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                _cache.Clear();
            }
        }

        /// <summary>
        /// 移除特定缓存
        /// </summary>
        public void Remove(string key)
        {
            lock (_lock)
            {
                _cache.Remove(key);
            }
        }
    }
}
