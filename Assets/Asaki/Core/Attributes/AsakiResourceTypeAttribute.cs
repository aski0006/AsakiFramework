using System;
using UnityEngine;

namespace Asaki.Core.Attributes
{
    /// <summary>
    /// [Asaki Native] 资源类型序列化标记。
    /// 用于在 Inspector 中显示资源类型的下拉选择列表。
    /// 配合 [SerializeReference] 使用。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AsakiResourceTypeAttribute : PropertyAttribute { }
}
