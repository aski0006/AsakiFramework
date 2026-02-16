using System.Collections.Generic;
using Asaki.Core.Logging;

namespace Asaki.Core.Context
{
    public static class AsakiGlobalInjector
    {
        private static readonly List<IAsakiInjector> _injectors = new List<IAsakiInjector>();

        public static void Register(IAsakiInjector injector)
        {
            if (injector == null || _injectors.Contains(injector))
                return;
            _injectors.Add(injector);
            ALog.Info($"[Asaki] Registered injector: {injector.GetType().Name}");
        }

        public static void Inject(object target, IAsakiResolver resolver = null)
        {
            if (target == null)
                return;

            foreach (IAsakiInjector injector in _injectors)
            {
                injector.Inject(target, resolver);
            }
        }
    }
}
