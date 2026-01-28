using System;

namespace Asaki.Core.Attributes
{
    /// <summary>
    /// 标记一个自定义数据结构为黑板数据结构，可在黑板中使用
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class AsakiBlackboardValueSchemaAttribute : Attribute { }
}
