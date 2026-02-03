using Asaki.Core.Architecture.Entities;

namespace Asaki.Core.Architecture
{
    /// <summary>
    /// 实体模型 - 作为 Architecture 的 Model 层实现
    /// 管理实体世界的生命周期
    /// </summary>
    public class EntityModel : IAsakiModel
    {
        private IEntityWorld _world;

        /// <summary>
        /// 实体世界
        /// </summary>
        public IEntityWorld World => _world;

        /// <summary>
        /// 创建模型
        /// </summary>
        public void Create()
        {
            _world = new EntityWorld();
        }

        /// <summary>
        /// 释放模型
        /// </summary>
        public void Dispose()
        {
            _world?.Dispose();
            _world = null;
        }
    }
}
