using System;
using Asaki.Core.Broker;

namespace Asaki.Core.Architecture.Events
{
    public struct OnAsakiArchitectureDisposeEvent : IAsakiEvent
    {
        /// <summary>
        /// Architecture的类型信息。
        /// </summary>
        public Type ArchitectureType { get; }

        /// <summary>
        /// Architecture释放事件。
        /// </summary>
        public OnAsakiArchitectureDisposeEvent(Type architectureType)
        {
            ArchitectureType = architectureType;
        }
    }
}
