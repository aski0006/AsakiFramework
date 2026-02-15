// File: Assets/Asaki/Tests/Resources/Mocks/MockEventService.cs
// 模拟的事件服务

using Asaki.Core.Broker;

namespace Asaki.Tests.Resources.Mocks
{
    /// <summary>
    /// 模拟的事件服务
    /// </summary>
    public class MockEventService : IAsakiEventService
    {
        public int PublishCallCount { get; private set; }
        public object LastPublishedEvent { get; private set; }

        public void Publish<T>(in T eventData)
            where T : IAsakiEvent
        {
            PublishCallCount++;
            LastPublishedEvent = eventData;
        }

        public void Subscribe<T>(IAsakiHandler<T> handler)
            where T : IAsakiEvent { }

        public void Unsubscribe<T>(IAsakiHandler<T> handler)
            where T : IAsakiEvent { }

        public void Dispose() { }

        public void Reset()
        {
            PublishCallCount = 0;
            LastPublishedEvent = null;
        }
    }
}
