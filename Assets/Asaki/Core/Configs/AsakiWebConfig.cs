using System;
using Asaki.Core.Attributes;
using Asaki.Core.Network;
using UnityEngine;

namespace Asaki.Core.Configs
{
    /// <summary>
    /// 表示Asaki网络配置的可序列化类，用于管理与网络请求相关的设置。
    /// </summary>
    [Serializable]
    public class AsakiWebConfig
    {
        /// <summary>
        /// 获取或设置网络请求的基础URL。
        /// 所有网络请求的地址将基于此基础URL构建。
        /// </summary>
        [field: SerializeField]
        public string BaseUrl { get; set; }

        /// <summary>
        /// 获取或设置网络请求的超时时间（单位为秒）。
        /// 如果请求在指定时间内未完成，将视为超时。
        /// </summary>
        [field: SerializeField]
        public int TimeoutSeconds { get; set; }

        /// <summary>
        /// 获取或设置初始的网络拦截器数组。
        /// 这些拦截器将在网络请求处理流程的初始阶段执行，
        /// 用于对请求进行预处理或对响应进行后处理等操作。
        /// 此属性使用了 <see cref="AsakiInterfaceAttribute"/> 标记，限定类型为 <see cref="IAsakiWebInterceptor"/>。
        /// </summary>
        [SerializeReference]
        [AsakiInterface(typeof(IAsakiWebInterceptor))]
        public IAsakiWebInterceptor[] InitialInterceptors;
    }
}
