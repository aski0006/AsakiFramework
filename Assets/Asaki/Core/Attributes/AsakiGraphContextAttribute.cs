using System;

namespace Asaki.Core.Attributes
{
    /// <summary>
    /// 标记一个图资产，自动生成注册语句到 AsakiGraphWindow/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class AsakiGraphContextAttribute : Attribute
    {
        public Type GraphType { get; private set; }
        public string Path { get; private set; }

        public AsakiGraphContextAttribute(Type graphType, string path)
        {
            GraphType = graphType;
            Path = path;
        }
    }

    /// <summary>
    /// 标记一个图的输入节点，用于生成 NodeView
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AsakiNodeInputAttribute : Attribute
    {
        public string PortName;
        public bool Multiple; // 是否允许多连

        public AsakiNodeInputAttribute(string name = "In", bool multiple = true)
        {
            PortName = name;
            Multiple = multiple;
        }
    }

    /// <summary>
    /// 标记一个图的输出节点，用于生成 NodeView
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AsakiNodeOutputAttribute : Attribute
    {
        public string PortName;
        public bool Multiple;

        public AsakiNodeOutputAttribute(string name = "Out", bool multiple = false)
        {
            PortName = name;
            Multiple = multiple;
        }
    }
}
