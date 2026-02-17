using System;

namespace Asaki.Core.Architecture
{
    /// <summary>
    /// IAsakiSystem 扩展方法 - 提供向后兼容性
    /// </summary>
    public static class AsakiSystemExtensions
    {
        /// <summary>
        /// 适配旧的 Setup() 方法到新的生命周期
        /// </summary>
        public static void SetupCompat(this IAsakiSystem system)
        {
            // 如果系统只有 Setup() 实现（通过默认方法），则调用 Setup()
            if (system is LegacySystemAdapter)
            {
                system.Create();
                system.Start();
            }
        }
    }

    /// <summary>
    /// 旧系统适配器 - 兼容只实现了 Setup() 的旧系统
    /// </summary>
    public abstract class LegacySystemAdapter : IAsakiSystem
    {
        public virtual void Create()
        {
            // 默认空实现
        }

        public virtual void Start()
        {
            // 默认空实现
        }

        /// <summary>
        /// 旧的 Setup 方法，子类实现此方法
        /// </summary>
        protected abstract void Setup();

        public abstract void Dispose();
    }
}
