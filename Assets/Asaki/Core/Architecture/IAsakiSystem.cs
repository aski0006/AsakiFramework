using System;

namespace Asaki.Core.Architecture
{
    public interface IAsakiSystem : IDisposable
    {
        /// <summary>
        /// 系统创建时调用（依赖已注入）
        /// </summary>
        void Create();

        /// <summary>
        /// 所有系统创建完成后调用（可安全访问其他系统）
        /// </summary>
        void Start();
    }
}
