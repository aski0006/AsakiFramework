using System;
using Asaki.Core.Resources;
using UnityEngine;

namespace Asaki.Core.FrameworkSettings
{
    /// <summary>
    /// 表示Asaki资源配置的可序列化类，用于管理资源加载相关的设置。
    /// </summary>
    [Serializable]
    public class AsakiResConfig
    {
        /// <summary>
        /// 获取或设置资源加载策略模式，默认值为AsakiResKitMode.Resources。
        /// 此模式决定了资源加载所采用的具体方式。
        /// </summary>
        [Tooltip("资源加载策略模式")]
        public AsakiResKitMode Mode = AsakiResKitMode.Resources;

        /// <summary>
        /// 获取或设置未使用资源释放的超时时间（单位为秒），默认值为60秒。
        /// 当资源在指定时间内未被使用时，将被释放以节省内存。
        /// </summary>
        [Tooltip("未使用的资源释放超时时间 (秒)")]
        public int TimeoutSeconds = 60;
    }
}
