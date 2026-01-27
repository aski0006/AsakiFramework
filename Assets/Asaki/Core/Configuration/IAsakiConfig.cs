using Asaki.Core.Serialization;

namespace Asaki.Core.Configuration
{
	/// <summary>
	/// 定义配置表数据对象的核心接口，继承自<see cref="IAsakiSavable"/>以支持序列化。
	/// </summary>
	/// <remarks>
	/// <para>实现要求：</para>
	/// <list type="number">
	///     <item>必须包含无参构造函数，用于反序列化时创建实例</item>
	///     <item><see cref="Id"/>属性应映射到配置表的主键列</item>
	///     <item><see cref="Serialize"/>和<see cref="Deserialize"/>方法应保持字段读写顺序一致</item>
	///     <item>建议标记为<see langword="sealed"/>或确保子类正确实现<see cref="CloneConfig"/></item>
	/// </list>
	/// <para>典型实现模式：</para>
	/// <code>
	/// [Serializable]
	/// public class ItemConfig : IAsakiConfig
	/// {
	///     public int Id { get; set; } // 主键
	///     public string Name { get; set; }
	///     public int StackSize { get; set; }
	///     
	///     // 序列化实现
	///     public void Serialize(IAsakiWriter writer)
	///     {
	///         writer.WriteInt(nameof(Id), Id);
	///         writer.WriteString(nameof(Name), Name);
	///         writer.WriteInt(nameof(StackSize), StackSize);
	///     }
	///     
	///     public void Deserialize(IAsakiReader reader)
	///     {
	///         Id = reader.ReadInt(nameof(Id));
	///         Name = reader.ReadString(nameof(Name));
	///         StackSize = reader.ReadInt(nameof(StackSize));
	///     }
	///     
	///     public IAsakiConfig CloneConfig() => new ItemConfig
	///     {
	///         Id = this.Id,
	///         Name = this.Name,
	///         StackSize = this.StackSize
	///     };
	///     
	///     public void AllowConfigSerialization(string permissionKey) { /* 安全检查 */ }
	/// }
	/// </code>
	/// </remarks>
	public interface IAsakiConfig : IAsakiSavable
	{
		/// <summary>
		/// 获取配置表的主键ID，在配置集合中唯一标识此记录。
		/// </summary>
		/// <value>整数类型的主键值，通常从1开始递增。</value>
		/// <remarks>
		/// <para>约束与约定：</para>
		/// <list type="bullet">
		///     <item>同一配置类型中，<see cref="Id"/>必须唯一</item>
		///     <item>建议保持连续，但并非强制要求</item>
		///     <item>0通常保留为"无效"或"空"配置</item>
		///     <item>负值通常保留为系统特殊用途</item>
		/// </list>
		/// 实现者应确保此属性与<see cref="IAsakiConfigService.Get{T}"/>方法的参数匹配。
		/// </remarks>
		int Id { get; }

		/// <summary>
		/// 授权配置序列化操作，提供额外的安全层以防止未授权访问。
		/// </summary>
		/// <param name="permissionKey">权限验证密钥，由配置管理器生成。</param>
		/// <remarks>
		/// <para>安全机制：</para>
		/// <list type="number">
		///     <item>在<see cref="Serialize"/>和<see cref="Deserialize"/>调用前验证</item>
		///     <item>可记录序列化操作日志用于审计</item>
		///     <item>支持基于角色的访问控制（RBAC）扩展</item>
		/// </list>
		/// <para>默认实现可为空方法，高级实现可抛出<see cref="UnauthorizedAccessException"/>。</para>
		/// </remarks>
		void AllowConfigSerialization(string permissionKey);

		/// <summary>
		/// 创建当前配置对象的深拷贝副本。
		/// </summary>
		/// <returns>新的<see cref="IAsakiConfig"/>实例，包含与原始对象相同的值。</returns>
		/// <remarks>
		/// <para>实现要求：</para>
		/// <list type="bullet">
		///     <item>必须执行深拷贝，避免引用类型字段共享</item>
		///     <item>返回类型应为具体实现类型，但通过接口返回</item>
		///     <item>不应影响原始对象的状态</item>
		///     <item>对于大型配置，考虑使用序列化/反序列化实现深拷贝</item>
		/// </list>
		/// <para>典型使用场景：</para>
		/// <list type="bullet">
		///     <item>运行时配置修改的沙箱副本</item>
		///     <item>多人游戏中的客户端独立配置实例</item>
		///     <item>配置编辑器的撤销/重做系统</item>
		///     <item>配置版本对比工具</item>
		/// </list>
		/// </remarks>
		IAsakiConfig CloneConfig();
	}
}
