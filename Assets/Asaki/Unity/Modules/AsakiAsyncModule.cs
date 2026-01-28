using System.Threading.Tasks;
using Asaki.Core;
using Asaki.Core.Async;
using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Asaki.Unity.Services.Async;
using Cysharp.Threading.Tasks;

namespace Asaki.Unity.Modules
{
    [AsakiModule(100)]
    public class AsakiAsyncModule : IAsakiModule
    {
        private IAsakiAsyncService _asakiAsyncService;

        public void OnInit()
        {
            // 1. 创建具体服务实现
            _asakiAsyncService = new AsakiAsyncProvider();
            // 2. 注册服务接口 (供其他模块通过 Get<IAsakiAsyncService> 获取)
            AsakiContext.Register(_asakiAsyncService);
        }

        public UniTask OnInitAsync()
        {
            // Coroutines 服务本身无需异步初始化
            return UniTask.CompletedTask;
        }

        public void OnDispose() { }
    }
}
