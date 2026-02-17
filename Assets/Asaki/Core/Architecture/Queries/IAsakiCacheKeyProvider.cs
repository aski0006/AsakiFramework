namespace Asaki.Core.Architecture.Queries
{
    /// <summary>
    /// 为带参数的 Query 提供自定义缓存键
    /// </summary>
    public interface IAsakiCacheKeyProvider
    {
        /// <summary>
        /// 获取缓存键
        /// </summary>
        /// <returns>缓存键字符串</returns>
        string GetCacheKey();
    }
}
