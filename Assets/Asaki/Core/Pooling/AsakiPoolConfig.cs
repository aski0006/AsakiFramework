using System;
using Asaki.Core.FrameworkSettings;
using UnityEngine;

namespace Asaki.Core.Pooling
{
    /// <summary>
    /// 对象池配置类
    /// 配置参数默认值从全局配置 AsakiPoolGlobalConfig 中获取
    /// </summary>
    [Serializable]
    public class AsakiPoolConfig
    {
        /// <summary>初始对象数量</summary>
        public int InitialSize;

        /// <summary>最大对象数量（0表示无限制）</summary>
        public int MaxSize;

        /// <summary>是否启用对象验证</summary>
        public bool EnableValidation;

        /// <summary>是否启用集合检查（检测重复归还）</summary>
        public bool EnableCollectionCheck;

        /// <summary>是否允许同步创建（当池为空时）</summary>
        public bool AllowSyncCreation;

        /// <summary>操作超时时间（秒，0表示无超时）</summary>
        public float OperationTimeout;

        #region Pool Governance (对象池治理)

        /// <summary>是否启用自动收缩</summary>
        public bool EnableAutoShrink;

        /// <summary>检查间隔（秒）</summary>
        public float CheckInterval;

        /// <summary>对象闲置多久视为"过期" (TTL, 秒)</summary>
        public float IdleTimeout;

        /// <summary>收缩时的保底数量</summary>
        public int KeepMinSize;

        /// <summary>每次收缩的比例（0-1，平滑释放）</summary>
        [Range(0f, 1f)]
        public float ShrinkRatio;

        #endregion

        /// <summary>
        /// 默认构造函数，从全局配置获取默认值
        /// </summary>
        public AsakiPoolConfig()
        {
            var global = AsakiPoolGlobalConfig.Instance;
            InitialSize = global.DefaultInitialSize;
            MaxSize = global.DefaultMaxSize;
            EnableValidation = global.DefaultEnableValidation;
            EnableCollectionCheck = global.DefaultEnableCollectionCheck;
            AllowSyncCreation = global.DefaultAllowSyncCreation;
            OperationTimeout = global.DefaultOperationTimeout;
            EnableAutoShrink = global.DefaultEnableAutoShrink;
            CheckInterval = global.DefaultCheckInterval;
            IdleTimeout = global.DefaultIdleTimeout;
            KeepMinSize = global.DefaultKeepMinSize;
            ShrinkRatio = global.DefaultShrinkRatio;
        }

        /// <summary>
        /// 默认配置（每次返回新实例，从全局配置获取默认值）
        /// </summary>
        public static AsakiPoolConfig Default => new AsakiPoolConfig();

        /// <summary>
        /// 创建适用于 GameObject 的配置
        /// </summary>
        /// <param name="initialSize">初始大小，默认从全局配置获取</param>
        /// <param name="maxSize">最大大小，默认从全局配置获取</param>
        public static AsakiPoolConfig ForGameObject(int? initialSize = null, int? maxSize = null)
        {
            var global = AsakiPoolGlobalConfig.Instance;
            return new AsakiPoolConfig
            {
                InitialSize = initialSize ?? global.DefaultInitialSize,
                MaxSize = maxSize ?? global.DefaultMaxSize,
                EnableValidation = true,
                EnableCollectionCheck = true,
                AllowSyncCreation = false,
            };
        }

        /// <summary>
        /// 创建适用于轻量级对象的配置
        /// </summary>
        /// <param name="maxSize">最大大小，默认从全局配置获取</param>
        public static AsakiPoolConfig ForLightWeightObject(int? maxSize = null)
        {
            var global = AsakiPoolGlobalConfig.Instance;
            return new AsakiPoolConfig
            {
                InitialSize = 0,
                MaxSize = maxSize ?? global.LightWeightPoolDefaultMaxSize,
                EnableValidation = false,
                EnableCollectionCheck = false,
                AllowSyncCreation = true,
            };
        }
    }
}
