using System;
using Asaki.Core.Attributes;
using Asaki.Core.Configuration;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Asaki.Core.Pooling;
using Asaki.Core.Pooling.Extensions;
using Asaki.Core.Pooling.Factories;
using Asaki.Core.Pooling.Interfaces;
using Asaki.Core.Resources;
using Asaki.Unity.Extensions;
using Asaki.Unity.Services.Async;
using Cysharp.Threading.Tasks;
using Game.Scripts.Data;
using Game.Scripts.View;
using UnityEngine;

namespace Game.Scripts
{
    public class TestCardViewManager
        : MonoBehaviour,
            IAsakiAutoInject,
            IAsakiSceneService,
            IAsakiInit<IAsakiPoolService, IAsakiConfigService, IAsakiResourceService>
    {
        public GameObject CardViewPrefab;
        private IAsakiPoolService _asakiPoolService;
        private IAsakiConfigService _asakiConfigService;
        private IAsakiResourceService _asakiResourceService;
        private bool _isPoolInitialized = false;

        [AsakiInject]
        public async void Init(
            IAsakiPoolService args1,
            IAsakiConfigService args2,
            IAsakiResourceService args3
        )
        {
            try
            {
                _asakiPoolService = args1;
                _asakiConfigService = args2;
                _asakiResourceService = args3;
                await Prewarn();
            }
            catch (Exception e)
            {
                ALog.Error("[TestCardViewManager] Init failure. Exception: " + e.Message, e);
            }
        }

        private async UniTask Prewarn()
        {
            if (CardViewPrefab != null)
            {
                // 检查对象池是否存在
                if (!_asakiPoolService.HasPool("CardView"))
                {
                    // 创建对象工厂
                    var factory = new GameObjectFactory(CardViewPrefab, transform);

                    // 创建对象池
                    await _asakiPoolService.CreatePoolAsync("CardView", factory);
                    _isPoolInitialized = true;
                }
            }
        }

        [ContextMenu("SpawnCardView")]
        public async UniTaskVoid SpawnCardView()
        {
            // 确保对象池已初始化
            if (!_isPoolInitialized)
            {
                await Prewarn();
            }

            // 获取对象池
            var pool = _asakiPoolService.GetPool<GameObject>("CardView");
            if (pool != null)
            {
                // 获取对象
                var cardObj = await pool.GetAsync();
                if (cardObj != null)
                {
                    // 获取CardView组件
                    CardView cv = cardObj.GetComponent<CardView>();
                    if (cv != null)
                    {
                        // 初始化CardView
                        cv.Init(_asakiResourceService);

                        // 加载卡片数据
                        var cardData = _asakiConfigService.Get<CardData>(0);
                        if (cardData != null)
                        {
                            await cv.LoadCardData(cardData);
                        }
                    }
                }
            }
        }
    }
}
