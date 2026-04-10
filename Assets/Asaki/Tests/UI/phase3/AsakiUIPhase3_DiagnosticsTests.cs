using System.Collections;
using Asaki.Core.Context;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.UI;
using Asaki.Unity.Services.UI;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Asaki.Tests.UI.Phase3
{
    public class AsakiUIPhase3_FinalDiagnosticsJsonTests
    {
        private AsakiUIManageService _svc;
        private Asaki.Tests.UI.Phase1.MockSimulationService _sim;
        private Asaki.Tests.UI.Phase1.MockResourceService _res;
        private Asaki.Tests.UI.Phase1.MockPoolService _pool;
        private Asaki.Tests.UI.Phase1.MockEventService _evt;

        [SetUp]
        public void SetUp()
        {
            AsakiContext.ClearAll();
            _sim = new Asaki.Tests.UI.Phase1.MockSimulationService();
            _res = new Asaki.Tests.UI.Phase1.MockResourceService();
            _pool = new Asaki.Tests.UI.Phase1.MockPoolService();
            _evt = new Asaki.Tests.UI.Phase1.MockEventService();

            AsakiContext.Register<Asaki.Core.Simulation.IAsakiSimulationService>(_sim);

            _res.RegisterPrefab(
                "UI/JsonWin",
                Asaki.Tests.UI.Phase1.Phase1TestUtils.CreateWindowPrefab("JsonWin")
            );

            var cfg = new AsakiUIConfig();
            cfg.UIList.Add(
                new UIInfo
                {
                    ID = 5201,
                    Name = "JsonWin",
                    AssetPath = "UI/JsonWin",
                    Layer = AsakiUILayer.Normal,
                    UsePool = false,
                }
            );
            cfg.InitializeLookup();

            _svc = new AsakiUIManageService(cfg, new Vector2(1920, 1080), 0.5f, _evt, _res, _pool);
            _svc.OnInit();
            _svc.OnInitAsync().Forget();
            _svc.DiagnosticsEnabled = true;
        }

        [TearDown]
        public void TearDown()
        {
            _svc?.OnDispose();
            AsakiContext.ClearAll();
        }

        [UnityTest]
        public IEnumerator DumpDiagnosticsJson_ShouldContainCoreFields()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                await _svc.OpenAsync<Asaki.Tests.UI.Phase1.TestWindow>(5201);

                string json = _svc.DumpDiagnosticsJson(pretty: false);

                Assert.IsTrue(json.Contains("generatedAtUtc"));
                Assert.IsTrue(json.Contains("openedWindowCount"));
                Assert.IsTrue(json.Contains("openPerf"));
                Assert.IsTrue(json.Contains("layerCounters"));
            });
        }
    }
}
