using Asaki.Core.Broker;
using Asaki.Generated;
using UnityEngine;

namespace Asaki.Unity.Services.Scene.SceneManagement.Test
{
    // ==========================================
    // 示例1：小事件 - 自动使用结构体
    // ==========================================
    public struct PlayerJumpEvent : IAsakiEvent
    {
        public int PlayerId;
    }

    // ==========================================
    // 示例2：大事件 - 自动或强制使用类+对象池
    // ==========================================
    [LargeEvent(EstimatedSize = 200)]
    public class PlayerDamageEvent : IResettableEvent
    {
        public int PlayerId;
        public float DamageAmount;
        public Vector3 HitPosition;
        public Vector3 HitNormal;
        public int DamageSourceId;
        public string DamageType;
        public float KnockbackForce;
        public Vector3 KnockbackDirection;
        public bool IsCriticalHit;
        public float CriticalMultiplier;

        public void Reset()
        {
            PlayerId = 0;
            DamageAmount = 0;
            HitPosition = Vector3.zero;
            HitNormal = Vector3.zero;
            DamageSourceId = 0;
            DamageType = null;
            KnockbackForce = 0;
            KnockbackDirection = Vector3.zero;
            IsCriticalHit = false;
            CriticalMultiplier = 1f;
        }
    }

    // ==========================================
    // 示例3：强制使用小事件模式
    // ==========================================
    [SmallEvent]
    public struct GameStateChangedEvent : IAsakiEvent
    {
        public int NewState;
    }

    // ==========================================
    // 示例使用类
    // ==========================================
    public class HybridEventExample
        : MonoBehaviour,
            IAsakiHandler<PlayerJumpEvent>,
            IAsakiHandler<PlayerDamageEvent>
    {
        private void OnEnable()
        {
            this.AsakiRegister();
        }

        private void OnDisable()
        {
            this.AsakiUnregister();
        }

        public void OnEvent(in PlayerJumpEvent e)
        {
            Debug.Log($"[HybridEventExample] Player {e.PlayerId} jumped! (Struct mode)");
        }

        public void OnEvent(in PlayerDamageEvent e)
        {
            Debug.Log(
                $"[HybridEventExample] Player {e.PlayerId} took {e.DamageAmount} damage! (ClassPool mode)"
            );
        }

        [ContextMenu("Test Small Event")]
        public void TestSmallEvent()
        {
            var evt = new PlayerJumpEvent { PlayerId = 1 };
            AsakiBroker.Publish(evt);

            var strategy = EventStrategySelector.GetStrategy<PlayerJumpEvent>();
            Debug.Log($"[HybridEventExample] PlayerJumpEvent strategy: {strategy}");
        }

        [ContextMenu("Test Large Event")]
        public void TestLargeEvent()
        {
            // 方式1：直接 new（不推荐，会产生GC）
            // var evt = new PlayerDamageEvent { PlayerId = 1, DamageAmount = 100 };

            // 方式2：使用对象池（推荐）
            var evt = EventPool.Rent<PlayerDamageEvent>();
            evt.PlayerId = 1;
            evt.DamageAmount = 100;
            evt.HitPosition = new Vector3(1, 2, 3);

            AsakiBroker.Publish(evt);

            // 使用完后归还到池
            EventPool.Return(evt);

            var strategy = EventStrategySelector.GetStrategy<PlayerDamageEvent>();
            Debug.Log($"[HybridEventExample] PlayerDamageEvent strategy: {strategy}");
        }

        [ContextMenu("Show Threshold")]
        public void ShowThreshold()
        {
            Debug.Log($"[HybridEventExample] Current threshold: {EventPool.Threshold} bytes");
        }

        [ContextMenu("Set Threshold to 64")]
        public void SetThreshold64()
        {
            EventPool.Threshold = 64;
            EventStrategySelector.ClearCache();
            Debug.Log($"[HybridEventExample] Threshold set to {EventPool.Threshold} bytes");
        }
    }
}
