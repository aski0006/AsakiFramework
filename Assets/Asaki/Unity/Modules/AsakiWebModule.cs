using System.Threading.Tasks;
using Asaki.Core;
using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.Network;
using Asaki.Unity.Services.Network;
using Cysharp.Threading.Tasks;

namespace Asaki.Unity.Modules
{
    [AsakiModule(priority: 100)]
    public class AsakiWebModule : IAsakiModule
    {
        private AsakiWebService _asakiWebService;

        public void OnInit()
        {
            AsakiFrameworkSetting asakiFrameworkSetting = AsakiContext.Get<AsakiFrameworkSetting>();
            _asakiWebService = new AsakiWebService();
            _asakiWebService.Setup(asakiFrameworkSetting.WebConfig);
            AsakiContext.Register<IAsakiWebService>(_asakiWebService);
        }

        public UniTask OnInitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void OnDispose()
        {
            _asakiWebService?.OnDispose();
        }
    }
}
