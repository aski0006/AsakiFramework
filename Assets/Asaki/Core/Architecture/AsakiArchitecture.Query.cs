using System;
using System.Diagnostics;
using Asaki.Core.Architecture.Queries;
using Asaki.Core.Logging;
using Cysharp.Threading.Tasks;

namespace Asaki.Core.Architecture
{
    public abstract partial class AsakiArchitecture
    {
        // ========================================================================
        // Query 配置项
        // ========================================================================

        private bool _enableQueryProfiling = false;
        private bool _enableQueryLogging = false;
        private bool _enableQueryCache = false;
        private QueryCacheManager _queryCache;

        /// <summary>
        /// 启用 Query 性能分析
        /// </summary>
        public void EnableQueryProfiling(bool enable)
        {
            _enableQueryProfiling = enable;
        }

        /// <summary>
        /// 启用 Query 日志记录
        /// </summary>
        public void EnableQueryLogging(bool enable)
        {
            _enableQueryLogging = enable;
        }

        /// <summary>
        /// 启用 Query 缓存
        /// </summary>
        public void EnableQueryCache(bool enable)
        {
            _enableQueryCache = enable;
            if (enable)
            {
                _queryCache ??= new QueryCacheManager();
            }
        }

        // ========================================================================
        // 1. 同步 Query（无缓存）
        // ========================================================================

        /// <summary>
        /// 执行同步 Query
        /// </summary>
        public TResult SendQuery<TQuery, TResult>()
            where TQuery : class, IAsakiQuery<TResult>, new()
        {
            // 尝试从对象池获取，池不存在则使用new
            bool fromPool = QueryPoolManager.TryRent<TQuery>(out TQuery query);
            query ??= new TQuery();

            try
            {
                query.Create(this);

                if (_enableQueryLogging)
                    ALog.Info($"[Query] Executing {typeof(TQuery).Name}");

                TResult result;
                if (_enableQueryProfiling)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    result = query.Query();
                    sw.Stop();
                    ALog.Info($"[Query] {typeof(TQuery).Name} took {sw.ElapsedMilliseconds}ms");
                }
                else
                {
                    result = query.Query();
                }

                return result;
            }
            catch (Exception ex)
            {
                ALog.Error($"[Query] {typeof(TQuery).Name} failed: {ex.Message}", ex);
                throw;
            }
            finally
            {
                // 根据来源决定归还方式
                if (fromPool)
                    QueryPoolManager.TryReturn(query);
            }
        }

        // ========================================================================
        // 2. 同步 Query（带缓存）
        // ========================================================================

        /// <summary>
        /// 执行同步 Query（带缓存支持）
        /// </summary>
        /// <param name="cacheSeconds">缓存时长（秒），0 表示不缓存</param>
        public TResult SendQuery<TQuery, TResult>(float cacheSeconds)
            where TQuery : class, IAsakiQuery<TResult>, new()
        {
            // 如果启用缓存且有有效的缓存时间
            if (_enableQueryCache && cacheSeconds > 0f)
            {
                string cacheKey = typeof(TQuery).FullName;

                // 尝试从缓存获取
                if (_queryCache.TryGetCache<TResult>(cacheKey, out TResult cachedResult))
                {
                    if (_enableQueryLogging)
                        ALog.Info($"[Query] Cache hit: {typeof(TQuery).Name}");
                    return cachedResult;
                }

                // 缓存未命中，执行查询
                TResult result = SendQuery<TQuery, TResult>();

                // 存入缓存
                _queryCache.SetCache(cacheKey, result, cacheSeconds);

                return result;
            }
            // 不使用缓存
            return SendQuery<TQuery, TResult>();
        }

        // ========================================================================
        // 3. 异步 Query（无缓存）
        // ========================================================================

        /// <summary>
        /// 执行异步 Query
        /// </summary>
        public async UniTask<TResult> SendQueryAsync<TQuery, TResult>()
            where TQuery : class, IAsakiQueryAsync<TResult>, new()
        {
            // 尝试从对象池获取，池不存在则使用new
            bool fromPool = QueryPoolManager.TryRent<TQuery>(out TQuery query);
            query ??= new TQuery();

            try
            {
                query.Create(this);

                if (_enableQueryLogging)
                    ALog.Info($"[QueryAsync] Executing {typeof(TQuery).Name}");

                TResult result;
                if (_enableQueryProfiling)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    result = await query.QueryAsync();
                    sw.Stop();
                    ALog.Info(
                        $"[QueryAsync] {typeof(TQuery).Name} took {sw.ElapsedMilliseconds}ms"
                    );
                }
                else
                {
                    result = await query.QueryAsync();
                }

                return result;
            }
            catch (Exception ex)
            {
                ALog.Error($"[QueryAsync] {typeof(TQuery).Name} failed: {ex.Message}", ex);
                throw;
            }
            finally
            {
                // 根据来源决定归还方式
                if (fromPool)
                    QueryPoolManager.TryReturn(query);
            }
        }

        // ========================================================================
        // 4. 异步 Query（带缓存）
        // ========================================================================

        /// <summary>
        /// 执行异步 Query（带缓存支持）
        /// </summary>
        public async UniTask<TResult> SendQueryAsync<TQuery, TResult>(float cacheSeconds)
            where TQuery : class, IAsakiQueryAsync<TResult>, new()
        {
            if (_enableQueryCache && cacheSeconds > 0f)
            {
                string cacheKey = typeof(TQuery).FullName;

                if (_queryCache.TryGetCache<TResult>(cacheKey, out TResult cachedResult))
                {
                    if (_enableQueryLogging)
                        ALog.Info($"[QueryAsync] Cache hit: {typeof(TQuery).Name}");
                    return cachedResult;
                }

                TResult result = await SendQueryAsync<TQuery, TResult>();
                _queryCache.SetCache(cacheKey, result, cacheSeconds);

                return result;
            }
            return await SendQueryAsync<TQuery, TResult>();
        }

        // ========================================================================
        // 5. 带参数配置的 Query
        // ========================================================================

        /// <summary>
        /// 执行带参数配置的同步 Query
        /// </summary>
        public TResult SendQuery<TQuery, TResult>(Action<TQuery> configure, float cacheSeconds = 0f)
            where TQuery : class, IAsakiQuery<TResult>, new()
        {
            // 注意：带参数的 Query 不使用类型名作为缓存键
            // 需要子类自己实现 GetCacheKey() 方法

            // 尝试从对象池获取，池不存在则使用new
            bool fromPool = QueryPoolManager.TryRent<TQuery>(out TQuery query);
            query ??= new TQuery();

            try
            {
                configure?.Invoke(query);
                query.Create(this);

                if (_enableQueryLogging)
                    ALog.Info($"[Query] Executing {typeof(TQuery).Name} (with params)");

                return query.Query();
            }
            catch (Exception ex)
            {
                ALog.Error($"[Query] {typeof(TQuery).Name} failed: {ex.Message}", ex);
                throw;
            }
            finally
            {
                // 根据来源决定归还方式
                if (fromPool)
                    QueryPoolManager.TryReturn(query);
            }
        }

        /// <summary>
        /// 执行带参数配置的异步 Query
        /// </summary>
        public async UniTask<TResult> SendQueryAsync<TQuery, TResult>(
            Action<TQuery> configure,
            float cacheSeconds = 0f
        )
            where TQuery : class, IAsakiQueryAsync<TResult>, new()
        {
            // 尝试从对象池获取，池不存在则使用new
            bool fromPool = QueryPoolManager.TryRent<TQuery>(out TQuery query);
            query ??= new TQuery();

            try
            {
                configure?.Invoke(query);
                query.Create(this);

                if (_enableQueryLogging)
                    ALog.Info($"[QueryAsync] Executing {typeof(TQuery).Name} (with params)");

                return await query.QueryAsync();
            }
            catch (Exception ex)
            {
                ALog.Error($"[QueryAsync] {typeof(TQuery).Name} failed: {ex.Message}", ex);
                throw;
            }
            finally
            {
                // 根据来源决定归还方式
                if (fromPool)
                    QueryPoolManager.TryReturn(query);
            }
        }

        // ========================================================================
        // 6. 缓存管理
        // ========================================================================

        /// <summary>
        /// 清空所有 Query 缓存
        /// </summary>
        public void ClearQueryCache()
        {
            _queryCache?.ClearAll();
        }

        /// <summary>
        /// 移除特定 Query 的缓存
        /// </summary>
        public void InvalidateQueryCache<TQuery>()
            where TQuery : class
        {
            string cacheKey = typeof(TQuery).FullName;
            _queryCache?.Remove(cacheKey);
        }
    }
}
