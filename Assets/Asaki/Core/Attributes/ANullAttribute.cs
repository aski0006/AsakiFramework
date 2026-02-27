using System;

namespace Asaki.Core.Attributes
{
    /// <summary>
    /// 标记注入方法的参数为可空依赖。
    /// </summary>
    /// <remarks>
    /// 当使用 [ANull] 标记参数时，如果依赖在容器中不存在，会注入 null 而非抛出异常。
    /// 这适用于某些功能可能在依赖不可用时需要优雅降级的场景。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class ANullAttribute : Attribute { }
}
