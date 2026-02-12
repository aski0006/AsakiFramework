using System.Collections.Generic;
using UnityEngine;

namespace Asaki.Plugin.ComboSystem
{
    /// <summary>
    /// 判定框对象
    /// </summary>
    public class HitBox : MonoBehaviour
    {
        public Collider Collider { get; private set; }
        public string CurrentId { get; private set; }

        public void Activate(HitBoxDefinition def)
        {
            CurrentId = def.HitBoxId;

            // 设置形状
            SetupShape(def);

            // 设置位置
            var bone = transform.parent.Find(def.BoneName);
            if (bone != null)
            {
                transform.SetParent(bone);
                transform.localPosition = def.Offset;
            }

            // 激活
            gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            gameObject.SetActive(false);
            transform.SetParent(transform.root);
        }

        void SetupShape(HitBoxDefinition def)
        {
            // 清除现有碰撞器
            foreach (var col in GetComponents<Collider>())
            {
                Destroy(col);
            }

            switch (def.Shape)
            {
                case HitBoxShape.Box:
                    var boxCollider = gameObject.AddComponent<BoxCollider>();
                    boxCollider.size = def.Size;
                    boxCollider.isTrigger = true;
                    Collider = boxCollider;
                    break;

                case HitBoxShape.Sphere:
                    var sphereCollider = gameObject.AddComponent<SphereCollider>();
                    sphereCollider.radius = def.Radius;
                    sphereCollider.isTrigger = true;
                    Collider = sphereCollider;
                    break;

                case HitBoxShape.Capsule:
                    var capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
                    capsuleCollider.radius = def.Radius;
                    capsuleCollider.height = def.Height;
                    capsuleCollider.isTrigger = true;
                    Collider = capsuleCollider;
                    break;
            }
        }
    }

    /// <summary>
    /// 判定框管理器 - 仅负责Collider的激活/禁用
    /// 不处理任何碰撞检测逻辑
    /// </summary>
    public class HitBoxManager : MonoBehaviour
    {
        [SerializeField]
        private Transform hitBoxRoot;

        private Dictionary<string, HitBox> _hitBoxes = new Dictionary<string, HitBox>();
        private List<HitBox> _activeHitBoxes = new List<HitBox>();

        void Awake()
        {
            // 预创建判定框对象池
            InitializeHitBoxes();
        }

        void InitializeHitBoxes()
        {
            if (hitBoxRoot == null)
            {
                hitBoxRoot = new GameObject("HitBoxes").transform;
                hitBoxRoot.SetParent(transform);
                hitBoxRoot.localPosition = Vector3.zero;
            }

            // 预创建一些判定框对象
            for (int i = 0; i < 8; i++)
            {
                var go = new GameObject($"HitBox_{i}");
                go.SetActive(false);
                go.transform.SetParent(hitBoxRoot);
                go.AddComponent<HitBox>();
            }
        }

        /// <summary>
        /// 激活判定框 - 由状态机调用
        /// </summary>
        public void ActivateHitBoxes(HitBoxDefinition[] definitions)
        {
            foreach (var def in definitions)
            {
                var hitBox = GetOrCreateHitBox(def.HitBoxId);
                hitBox.Activate(def);
                _activeHitBoxes.Add(hitBox);
            }
        }

        /// <summary>
        /// 禁用所有判定框
        /// </summary>
        public void DeactivateAllHitBoxes()
        {
            foreach (var hitBox in _activeHitBoxes)
            {
                hitBox.Deactivate();
            }
            _activeHitBoxes.Clear();
        }

        /// <summary>
        /// 获取Collider - 外部CombatSystem使用
        /// </summary>
        public Collider GetCollider(string hitBoxId)
        {
            return _hitBoxes.TryGetValue(hitBoxId, out var hitBox) ? hitBox.Collider : null;
        }

        HitBox GetOrCreateHitBox(string hitBoxId)
        {
            if (_hitBoxes.TryGetValue(hitBoxId, out var existing))
            {
                return existing;
            }

            // 尝试找一个未使用的判定框
            for (int i = 0; i < hitBoxRoot.childCount; i++)
            {
                var child = hitBoxRoot.GetChild(i);
                if (!child.gameObject.activeInHierarchy)
                {
                    var hitBox = child.GetComponent<HitBox>();
                    _hitBoxes[hitBoxId] = hitBox;
                    return hitBox;
                }
            }

            // 创建新的判定框
            var go = new GameObject($"HitBox_{hitBoxId}");
            go.SetActive(false);
            go.transform.SetParent(hitBoxRoot);
            var newHitBox = go.AddComponent<HitBox>();
            _hitBoxes[hitBoxId] = newHitBox;
            return newHitBox;
        }
    }
}
