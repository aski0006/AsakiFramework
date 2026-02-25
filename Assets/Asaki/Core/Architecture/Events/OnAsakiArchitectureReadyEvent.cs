using System;
using Asaki.Core.Broker;

namespace Asaki.Core.Architecture.Events
{
    /// <summary>
    /// Architecture启动完成事件。
    /// 当Architecture完成所有初始化流程（包括OnSetup、Model创建、System创建和启动、OnStart）后发布此事件。
    /// </summary>
    public readonly struct OnAsakiArchitectureReadyEvent : IAsakiEvent
    {
        /// <summary>
        /// Architecture的类型信息。
        /// </summary>
        public Type ArchitectureType { get; }

        /// <summary>
        /// Architecture实例引用。
        /// </summary>
        public IAsakiArchitecture Architecture { get; }

        /// <summary>
        /// 初始化Architecture启动完成事件。
        /// </summary>
        /// <param name="architectureType">Architecture的类型</param>
        /// <param name="architecture">Architecture实例</param>
        public OnAsakiArchitectureReadyEvent(Type architectureType, IAsakiArchitecture architecture)
        {
            ArchitectureType = architectureType;
            Architecture = architecture;
        }
    }
}
