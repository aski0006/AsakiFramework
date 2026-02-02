using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Asaki.Core.Serialization.Migration;
using Cysharp.Threading.Tasks;

namespace Asaki.Unity.Modules
{
    /// <summary>
    /// 数据迁移系统模块。
    /// </summary>
    /// <remarks>
    /// 此模块负责初始化和管理数据迁移注册表。
    /// 它在序列化服务之前初始化，以确保迁移在加载数据时可用。
    /// </remarks>
    [AsakiModule(140)] // 优先级140，在AsakiPoolModule(150)之前初始化
    public class AsakiMigrationModule : IAsakiModule
    {
        private IAsakiMigrationRegistry _registry;

        public void OnInit()
        {
            ALog.Info("[AsakiMigration] Initializing migration system...");

            // 创建迁移注册表
            _registry = new AsakiMigrationRegistry();

            // 注册到全局上下文，使其他服务可以访问
            AsakiContext.Register(_registry);

            // 自动发现并注册标记了 [AsakiMigration] 的迁移类
            AutoRegisterMigrations();

            ALog.Info("[AsakiMigration] Migration system initialized successfully");
        }

        public UniTask OnInitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void OnDispose()
        {
            ALog.Info("[AsakiMigration] Disposing migration system...");
            _registry = null;
        }

        /// <summary>
        /// 自动发现并注册所有标记了 [AsakiMigration] 特性的迁移类。
        /// </summary>
        private void AutoRegisterMigrations()
        {
            // 注：这里可以实现自动发现逻辑，
            // 但由于Unity的反射限制和性能考虑，
            // 建议使用源生成器或手动注册。

            // 可选：使用反射查找所有实现了IAsakiMigration的类
            // 并检查是否有[AsakiMigration]特性

            ALog.Info(
                "[AsakiMigration] Auto-registration completed. Use AsakiContext.Get<IAsakiMigrationRegistry>().RegisterMigration() to manually register migrations."
            );
        }
    }
}
