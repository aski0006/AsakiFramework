using System;
using System.Collections.Generic;

namespace Asaki.Core.Architecture.Queries
{
    /// <summary>
    /// Query 结果缓存管理器
    /// 用于缓存频繁查询的结果
    /// 使用 DateTime.UtcNow 实现时间，避免依赖 Unity
    /// </summary>
    internal class QueryCacheManager
    {
        // 缓存条目
        private class CacheEntry
        {
            public object Result;
            public DateTime ExpireTime; // 过期时间（使用 DateTime 避免 Unity 依赖）
            public int AccessCount; // 访问次数（用于 LRU）
        }

        private readonly Dictionary<string, CacheEntry> _cache =
            new Dictionary<string, CacheEntry>();
        private readonly object _lock = new object();

        // 缓存容量限制
        private readonly int _maxCacheSize = 1000;

        // 访问队列（用于 LRU 淘汰）
        private readonly Queue<string> _accessOrder = new Queue<string>();

        // 时间提供者（可用于测试）
        private Func<DateTime> _timeProvider = () => DateTime.UtcNow;

        /// <summary>
        /// 设置时间提供者（主要用于测试）
        /// </summary>
        public void SetTimeProvider(Func<DateTime> timeProvider)
        {
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        /// <summary>
        /// 获取当前时间
        /// </summary>
        private DateTime Now => _timeProvider();

        /// <summary>
        /// 尝试从缓存获取结果
        /// </summary>
        public bool TryGetCache<TResult>(string key, out TResult result)
        {
            DateTime currentTime = Now;

            lock (_lock)
            {
                if (_cache.TryGetValue(key, out CacheEntry entry))
                {
                    // 检查是否过期
                    if (currentTime < entry.ExpireTime)
                    {
                        // 更新访问计数
                        entry.AccessCount++;
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

            result = default(TResult);
            return false;
        }

        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="result">缓存结果</param>
        /// <param name="cacheSeconds">缓存时长（秒）</param>
        public void SetCache<TResult>(string key, TResult result, float cacheSeconds)
        {
            DateTime expireTime = Now.AddSeconds(cacheSeconds);

            lock (_lock)
            {
                // 检查是否需要 LRU 淘汰
                if (!_cache.ContainsKey(key) && _cache.Count >= _maxCacheSize)
                {
                    EvictLRU();
                }

                _cache[key] = new CacheEntry
                {
                    Result = result,
                    ExpireTime = expireTime,
                    AccessCount = 0,
                };

                _accessOrder.Enqueue(key);
            }
        }

        /// <summary>
        /// LRU 淘汰 - 淘汰最早访问的条目
        /// </summary>
        private void EvictLRU()
        {
            // 淘汰队列前端的条目（最早访问的）
            while (_accessOrder.Count > 0)
            {
                var key = _accessOrder.Dequeue();
                if (_cache.Remove(key))
                {
                    return; // 成功淘汰一个
                }
                // 如果 key 不存在于缓存中，继续淘汰下一个
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
                _accessOrder.Clear();
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

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public int GetCacheCount()
        {
            lock (_lock)
            {
                return _cache.Count;
            }
        }
    }
}
