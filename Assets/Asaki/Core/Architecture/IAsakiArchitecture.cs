using Asaki.Core.Context;
using System;

namespace Asaki.Core.Architecture
{
    public interface IAsakiArchitecture : IAsakiSceneService, IAsakiServiceProvider,IDisposable
    {
        T GetSystem<T>()
            where T : class, IAsakiSystem;
        T GetModel<T>()
            where T : class, IAsakiModel;

        /// <summary>
        /// 获取实体世界（便捷方法）
        /// </summary>
        Entities.IEntityWorld GetEntityWorld();
    }
}
