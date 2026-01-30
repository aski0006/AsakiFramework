using System;

namespace Asaki.Core.Pooling
{
    /// <summary>
    /// 对象池配置类
    /// </summary>
    [Serializable]
    public class AsakiPoolConfig
    {
        /// <summary>初始对象数量</summary>
        public int InitialSize = 10;

        /// <summary>最大对象数量（0表示无限制）</summary>
        public int MaxSize = 100;

        /// <summary>是否启用对象验证</summary>
        public bool EnableValidation = true;

        /// <summary>是否启用集合检查（检测重复归还）</summary>
        public bool EnableCollectionCheck = true;

        /// <summary>是否允许同步创建（当池为空时）</summary>
        public bool AllowSyncCreation = false;

        /// <summary>操作超时时间（秒，0表示无超时）</summary>
        public float OperationTimeout = 0f;

        /// <summary>默认配置</summary>
        public static AsakiPoolConfig Default =>
            new AsakiPoolConfig
            {
                InitialSize = 0,
                MaxSize = 0,
                EnableValidation = true,
                EnableCollectionCheck = true,
                AllowSyncCreation = false,
                OperationTimeout = 0f,
            };

        /// <summary>
        /// 创建适用于 GameObject 的配置
        /// </summary>
        public static AsakiPoolConfig ForGameObject(int initialSize = 10, int maxSize = 100)
        {
            return new AsakiPoolConfig
            {
                InitialSize = initialSize,
                MaxSize = maxSize,
                EnableValidation = true,
                EnableCollectionCheck = true,
                AllowSyncCreation = false,
            };
        }

        /// <summary>
        /// 创建适用于轻量级对象的配置
        /// </summary>
        public static AsakiPoolConfig ForLightWeightObject(int maxSize = 1024)
        {
            return new AsakiPoolConfig
            {
                InitialSize = 0,
                MaxSize = maxSize,
                EnableValidation = false,
                EnableCollectionCheck = false,
                AllowSyncCreation = true,
            };
        }
    }
}
