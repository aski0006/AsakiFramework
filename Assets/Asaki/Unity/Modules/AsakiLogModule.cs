using System.Threading.Tasks;
using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Core.Configs;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Cysharp.Threading.Tasks;

namespace Asaki.Unity.Modules
{
    [AsakiModule(priority: -100)] // 极高优先级，确保在业务模块之前运行
    public class AsakiLogModule : IAsakiModule
    {
        private IAsakiLoggingService _service;

        public void OnInit()
        {
            // 检查是否已在 Bootstrapper 中初始化
            if (!AsakiContext.TryGet(out IAsakiLoggingService iService))
            {
                // 只有在 Bootstrapper 未初始化时才创建服务
                _service = new AsakiLoggingService();
                AsakiContext.Register<IAsakiLoggingService>(_service);

                // 应用配置
                AsakiLogConfig logConfig = null;
                if (AsakiContext.TryGet(out AsakiConfig config))
                {
                    logConfig = config.LogConfig;
                }

                // 确保配置不为null
                if (logConfig == null)
                {
                    logConfig = new AsakiLogConfig();
                }

                _service.ApplyConfig(logConfig);
            }
            else
            {
                _service = iService as AsakiLoggingService;
            }
        }

        public UniTask OnInitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void OnDispose()
        {
            _service = null;
        }
    }
}
