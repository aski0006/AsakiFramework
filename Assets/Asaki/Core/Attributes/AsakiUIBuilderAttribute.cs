using Asaki.Core.UI;
using System;

namespace Asaki.Core.Attributes
{
	/// <summary>
	/// 标记一个 UI 组件，将会在编辑器中自动生成 UI 组件元素
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class AsakiUIBuilderAttribute : Attribute
	{
		/// <summary>
		/// 组件类型
		/// </summary>
		public AsakiUIWidgetType Type { get; }

		/// <summary>
		/// 指定生成的 GameObject 名称。
		/// <para>若为空，自动使用字段名（去除下划线）。</para>
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// 指定父级路径（相对于 Root）。
		/// <para>例如: "Content/Grid"。若为空，则直接挂载在 Root 下。</para>
		/// </summary>
		public string Parent { get; set; }

		/// <summary>
		/// 指定生成的自定义组件的预制体名称。
		/// </summary>
		public string CustomPrefab { get; }

		/// <summary>
		/// 组件生成顺序
		/// </summary>
		public int Order { get; set; } = 0;

		/// <summary>
		/// 声明一个 UI 组件
		/// </summary>
		/// <param name="type">组件类型</param>
		public AsakiUIBuilderAttribute(AsakiUIWidgetType type)
		{
			Type = type;
		}

		/// <summary>
		/// 声明一个自定义组件
		/// </summary>
		public AsakiUIBuilderAttribute(string customPrefabName)
		{
			Type = AsakiUIWidgetType.Custom;
			CustomPrefab = customPrefabName;
		}
	}
}
