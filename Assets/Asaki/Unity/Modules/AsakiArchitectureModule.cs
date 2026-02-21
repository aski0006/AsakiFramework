using Asaki.Core.Architecture;
using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Cysharp.Threading.Tasks;

namespace Asaki.Unity.Modules
{
    [AsakiModule(priority: 100)]
    public class AsakiArchitectureModule : IAsakiModule
    {
        private ArchitectureRegister _architectureRegister;

        public void OnDispose()
        {
            _architectureRegister?.Dispose();
        }

        public void OnInit()
        {
            _architectureRegister = new ArchitectureRegister();
            AsakiContext.Register(_architectureRegister);
        }

        public UniTask OnInitAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}
