using System;

namespace Asaki.Core.Attributes
{
    /// <summary>
    /// 标记一个类或结构体需要自动生成 IAsakiSavable 实现
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public class AsakiSaveAttribute : Attribute
    {
        // 用于版本控制，防止坏档
        public int Version { get; }

        public AsakiSaveAttribute(int version = 1)
        {
            Version = version;
        }
    }

    /// <summary>
    /// 标记一个字段或属性需要被序列化
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    public class AsakiSaveMemberAttribute : Attribute
    {
        // 可选：指定序列化时的 Key（默认使用字段名）
        // 在 Binary 模式下此 Key 无效，仅用于 Debug JSON
        public string Key { get; set; }

        // 可选：手动指定排序顺序 (Binary模式下字段写入顺序很重要)
        public int Order { get; set; }

        public AsakiSaveMemberAttribute(string key = null, int order = 0)
        {
            Key = key;
            Order = order;
        }
    }
}
