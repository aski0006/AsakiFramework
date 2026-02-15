using System;

namespace Asaki.Core.Broker
{
    /// <summary>
    /// 标记事件为大事件，强制使用类+对象池模式
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Struct | AttributeTargets.Class,
        AllowMultiple = false,
        Inherited = false
    )]
    public sealed class LargeEventAttribute : Attribute
    {
        /// <summary>
        /// 估算的事件大小（字节），可选
        /// </summary>
        public int EstimatedSize { get; set; }

        public LargeEventAttribute() { }

        public LargeEventAttribute(int estimatedSize)
        {
            EstimatedSize = estimatedSize;
        }
    }

    /// <summary>
    /// 标记事件为小事件，强制使用结构体模式
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Struct | AttributeTargets.Class,
        AllowMultiple = false,
        Inherited = false
    )]
    public sealed class SmallEventAttribute : Attribute { }
}
