using System;

namespace Asaki.Core.Attributes
{
	/// <summary>
	/// 标记需要动态生成 <see cref="global::Asaki.Core.MVVM.AsakiProperty{T}"/> 的绑定类的属性
	/// <remarks>需要标记的类必须为 partial </remarks>>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class AsakiBindAttribute : Attribute { }
}
