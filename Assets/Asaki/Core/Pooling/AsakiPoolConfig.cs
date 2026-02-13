using System;
using UnityEngine;

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

        #region Pool Governance (对象池治理)

        /// <summary>是否启用自动收缩</summary>
        public bool EnableAutoShrink = true;

        /// <summary>检查间隔（秒）</summary>
        public float CheckInterval = 30f;

        /// <summary>对象闲置多久视为"过期" (TTL, 秒)</summary>
        public float IdleTimeout = 60f;

        /// <summary>收缩时的保底数量</summary>
        public int KeepMinSize = 5;

        /// <summary>每次收缩的比例（0-1，平滑释放）</summary>
        [Range(0f, 1f)]
        public float ShrinkRatio = 0.5f;

        #endregion

        private static readonly AsakiPoolConfig _defaultTemplate = new AsakiPoolConfig
        {
            InitialSize = 0,
            MaxSize = 100,
            EnableValidation = true,
            EnableCollectionCheck = true,
            AllowSyncCreation = false,
            OperationTimeout = 0f,
        };

        /// <summary>
        /// 默认配置（每次返回新实例，防止被意外修改）
        /// </summary>
        public static AsakiPoolConfig Default => new AsakiPoolConfig
        {
            InitialSize = _defaultTemplate.InitialSize,
            MaxSize = _defaultTemplate.MaxSize,
            EnableValidation = _defaultTemplate.EnableValidation,
            EnableCollectionCheck = _defaultTemplate.EnableCollectionCheck,
            AllowSyncCreation = _defaultTemplate.AllowSyncCreation,
            OperationTimeout = _defaultTemplate.OperationTimeout,
            EnableAutoShrink = _defaultTemplate.EnableAutoShrink,
            CheckInterval = _defaultTemplate.CheckInterval,
            IdleTimeout = _defaultTemplate.IdleTimeout,
            KeepMinSize = _defaultTemplate.KeepMinSize,
            ShrinkRatio = _defaultTemplate.ShrinkRatio,
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
