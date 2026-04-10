using System.Collections;
using System.Threading;
using Asaki.Core.Context;
using Asaki.Core.FrameworkSettings;
using Asaki.Core.UI;
using Asaki.Unity.Services.UI;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Asaki.Tests.UI.Phase2
{
    public struct TypedBackResult
    {
        public int SelectedId;
        public bool Confirmed;
    }

    [RequireComponent(typeof(CanvasGroup))]
    public class TypedResultReceiverWindow : AsakiUIWindow, IAsakiWindowWithResult<TypedBackResult>
    {
        public bool TypedResultCalled;
        public TypedBackResult LastResult;

        public void OnReturnValue(TypedBackResult value)
        {
            TypedResultCalled = true;
            LastResult = value;
        }

        // 兼容非泛型回调路径：桥接到强类型
        public void OnReturnValue(object value)
        {
            if (value is TypedBackResult typed)
            {
                OnReturnValue(typed);
            }
        }
    }

    [RequireComponent(typeof(CanvasGroup))]
    public class TypedChildWindow : AsakiUIWindow
    {
        // 空窗口，用于被 Back 关闭
    }

    public class AsakiUIPhase2_TypedBackResultTests
    {
        private AsakiUIManageService _svc;
        private Asaki.Tests.UI.Phase1.MockSimulationService _sim;
        private Asaki.Tests.UI.Phase1.MockResourceService _res;
        private Asaki.Tests.UI.Phase1.MockPoolService _pool;
        private Asaki.Tests.UI.Phase1.MockEventService _evt;

        private const int ReceiverId = 42001;
        private const int ChildId = 42002;

        private const string ReceiverPath = "UI/TypedReceiver";
        private const string ChildPath = "UI/TypedChild";

        [SetUp]
        public void SetUp()
        {
            AsakiContext.ClearAll();

            _sim = new Asaki.Tests.UI.Phase1.MockSimulationService();
            _res = new Asaki.Tests.UI.Phase1.MockResourceService();
            _pool = new Asaki.Tests.UI.Phase1.MockPoolService();
            _evt = new Asaki.Tests.UI.Phase1.MockEventService();

            AsakiContext.Register<Asaki.Core.Simulation.IAsakiSimulationService>(_sim);

            _res.RegisterPrefab(ReceiverPath, CreateReceiverPrefab("TypedResultReceiverPrefab"));
            _res.RegisterPrefab(ChildPath, CreateChildPrefab("TypedChildPrefab"));

            var cfg = BuildConfig(
                new UIInfo
                {
                    ID = ReceiverId,
                    Name = "TypedResultReceiverWindow",
                    AssetPath = ReceiverPath,
                    Layer = AsakiUILayer.Normal,
                    UsePool = false,
                },
                new UIInfo
                {
                    ID = ChildId,
                    Name = "TypedChildWindow",
                    AssetPath = ChildPath,
                    Layer = AsakiUILayer.Normal,
                    UsePool = false,
                }
            );

            _svc = new AsakiUIManageService(cfg, new Vector2(1920, 1080), 0.5f, _evt, _res, _pool);

            _svc.OnInit();
            _svc.OnInitAsync().Forget();
        }

        [TearDown]
        public void TearDown()
        {
            if (_svc != null)
            {
                _svc.OnDispose();
                _svc = null;
            }

            AsakiContext.ClearAll();
        }

        [UnityTest]
        public IEnumerator Back_TypedResult_ShouldDeliverToTypedReceiver()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                var receiver = await _svc.OpenAsync<TypedResultReceiverWindow>(ReceiverId);
                var child = await _svc.OpenAsync<TypedChildWindow>(ChildId);

                Assert.NotNull(receiver);
                Assert.NotNull(child);

                var result = new TypedBackResult { SelectedId = 2026, Confirmed = true };

                await _svc.Back(result);
                _sim.SimulateTicks(0.05f);

                Assert.IsTrue(_svc.IsOpened(ReceiverId));
                Assert.IsFalse(_svc.IsOpened(ChildId));

                Assert.IsTrue(receiver.TypedResultCalled);
                Assert.AreEqual(2026, receiver.LastResult.SelectedId);
                Assert.IsTrue(receiver.LastResult.Confirmed);
            });
        }

        private static GameObject CreateReceiverPrefab(string name)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>();
            go.AddComponent<CanvasGroup>();
            go.AddComponent<TypedResultReceiverWindow>();
            return go;
        }

        private static GameObject CreateChildPrefab(string name)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>();
            go.AddComponent<CanvasGroup>();
            go.AddComponent<TypedChildWindow>();
            return go;
        }

        private static AsakiUIConfig BuildConfig(params UIInfo[] infos)
        {
            var cfg = new AsakiUIConfig { ResourceReleaseDelaySeconds = 0.1f };
            cfg.UIList = new System.Collections.Generic.List<UIInfo>(infos);
            cfg.InitializeLookup();
            return cfg;
        }
    }
}
