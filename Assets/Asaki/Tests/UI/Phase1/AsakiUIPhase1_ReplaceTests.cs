using System.Collections;
using Asaki.Core.Context;
using Asaki.Core.UI;
using Asaki.Unity.Services.UI;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Asaki.Tests.UI.Phase1
{
    public class AsakiUIPhase1_ReplaceTests
    {
        private MockSimulationService _sim;
        private MockResourceService _res;
        private MockPoolService _pool;
        private MockEventService _evt;
        private AsakiUIManageService _svc;

        private const int AId = 2001;
        private const int BId = 2002;

        [SetUp]
        public void SetUp()
        {
            AsakiContext.ClearAll();

            _sim = new MockSimulationService();
            _res = new MockResourceService();
            _pool = new MockPoolService();
            _evt = new MockEventService();

            AsakiContext.Register<Asaki.Core.Simulation.IAsakiSimulationService>(_sim);

            _res.RegisterPrefab("UI/A", Phase1TestUtils.CreateWindowPrefab("APrefab"));
            _res.RegisterPrefab("UI/B", Phase1TestUtils.CreateWindowPrefab("BPrefab"));

            var cfg = Phase1TestUtils.BuildConfig(
                new Asaki.Core.FrameworkSettings.UIInfo
                {
                    ID = AId,
                    Name = "A",
                    AssetPath = "UI/A",
                    Layer = AsakiUILayer.Normal,
                    UsePool = false,
                },
                new Asaki.Core.FrameworkSettings.UIInfo
                {
                    ID = BId,
                    Name = "B",
                    AssetPath = "UI/B",
                    Layer = AsakiUILayer.Normal,
                    UsePool = false,
                }
            );

            _svc = new AsakiUIManageService(
                cfg,
                new UnityEngine.Vector2(1920, 1080),
                0.5f,
                _evt,
                _res,
                _pool
            );
            _svc.OnInit();
            _svc.OnInitAsync().Forget();
        }

        [TearDown]
        public void TearDown()
        {
            _svc?.OnDispose();
            AsakiContext.ClearAll();
        }

        [UnityTest]
        public IEnumerator Replace_ShouldCloseOldAndOpenNew()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                var a = await _svc.OpenAsync<TestWindow>(AId);
                Assert.NotNull(a);
                Assert.IsTrue(_svc.IsOpened(AId));

                var b = await _svc.ReplaceAsync<TestWindow>(BId);
                _sim.SimulateTicks(0.05f);

                Assert.NotNull(b);
                Assert.IsFalse(_svc.IsOpened(AId));
                Assert.IsTrue(_svc.IsOpened(BId));
            });
        }
    }
}
