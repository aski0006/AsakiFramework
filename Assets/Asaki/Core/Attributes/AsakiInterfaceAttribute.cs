using System;
using UnityEngine;

namespace Asaki.Core.Attributes
{
    /// <summary>
    /// [Asaki Native] 接口序列化标记。
    /// 用于在 Inspector 中显示接口实现的下拉选择列表。
    /// 配合 [SerializeReference] 使用。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AsakiInterfaceAttribute : PropertyAttribute
    {
        public Type InterfaceType { get; private set; }

        public AsakiInterfaceAttribute(Type interfaceType)
        {
            InterfaceType = interfaceType;
        }
    }
}
