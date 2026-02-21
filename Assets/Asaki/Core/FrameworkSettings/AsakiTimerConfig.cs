using System;
using UnityEngine;

namespace Asaki.Core.FrameworkSettings
{
    /// <summary>
    /// 定时器模块配置类
    /// 集中管理定时器服务的所有配置参数
    /// </summary>
    [Serializable]
    public class AsakiTimerConfig
    {
        #region 容量配置

        /// <summary>
        /// 定时器列表默认初始容量
        /// </summary>
        [Header("Capacity")]
        [Tooltip("定时器列表默认初始容量")]
        public int DefaultInitialCapacity = 64;

        #endregion

        #region 安全配置

        /// <summary>
        /// 单帧最大循环次数，防止死循环
        /// </summary>
        [Header("Safety")]
        [Tooltip("单帧最大循环次数，防止死循环")]
        [Range(1, 100)]
        public int MaxLoopIterations = 10;

        #endregion

#if UNITY_EDITOR
        #region 编辑器配置

        /// <summary>
        /// 编辑器全局时间缩放
        /// </summary>
        [Header("Editor")]
        [Tooltip("编辑器全局时间缩放")]
        [Range(0f, 10f)]
        public float GlobalTimeScale = 1f;

        /// <summary>
        /// 是否启用调试日志
        /// </summary>
        [Tooltip("是否启用调试日志")]
        public bool EnableDebugLog = false;

        #endregion
#endif
    }
}
