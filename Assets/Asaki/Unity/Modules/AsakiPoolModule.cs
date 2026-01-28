using System.Threading.Tasks;
using Asaki.Core;
using Asaki.Core.Async;
using Asaki.Core.Attributes;
using Asaki.Core.Broker;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Asaki.Core.Pooling;
using Asaki.Core.Resources;
using Asaki.Unity.Services.Resources;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 引用 Pooling 的命名空间

namespace Asaki.Unity.Modules
{
    // 优先级 150：必须在 Coroutines(100) 之后，但在 Resources(200) 之前
    // 因为 Resources 加载资源时可能需要从池中生成对象
    [AsakiModule(
        150,
        typeof(AsakiAsyncModule),
        typeof(AsakiResourcesModule),
        typeof(AsakiEventBusModule)
    )]
    public class AsakiPoolModule : IAsakiModule
    {
        private IAsakiPoolService _poolService;
        private IAsakiEventService _eventService;
        private IAsakiAsyncService _asyncService;
        private IAsakiResourceService _resourceService;

        [AsakiInject]
        public void Init(
            IAsakiAsyncService asyncService,
            IAsakiResourceService resourceService,
            IAsakiEventService eventService
        )
        {
            _asyncService = asyncService;
            _resourceService = resourceService;
            _eventService = eventService;
        }

        public void OnInit()
        {
            // 1. 获取依赖
            _poolService = new AsakiPoolService(_asyncService, _resourceService, _eventService);

            AsakiContext.Register(_poolService);

            ALog.Info("[Asaki] Pooling Service initialized (Async-Native Mode).");
        }

        public UniTask OnInitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void OnDispose() { }
    }
}
