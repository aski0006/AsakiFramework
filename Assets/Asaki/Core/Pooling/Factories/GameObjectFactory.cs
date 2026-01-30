using System;
using System.Threading;
using Asaki.Core.Pooling.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Asaki.Core.Pooling.Factories
{
    /// <summary>
    /// GameObject 工厂 - 从预制体创建 GameObject 实例
    /// </summary>
    public class GameObjectFactory : IAsakiPoolObjectFactory<GameObject>
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly bool _worldPositionStays;

        public GameObjectFactory(
            GameObject prefab,
            Transform parent = null,
            bool worldPositionStays = false
        )
        {
            _prefab = prefab ? prefab : throw new ArgumentNullException(nameof(prefab));
            _parent = parent;
            _worldPositionStays = worldPositionStays;
        }

        public UniTask<GameObject> CreateAsync(CancellationToken token = default(CancellationToken))
        {
            return UniTask.FromResult(CreateSync());
        }

        public GameObject CreateSync()
        {
            if (!_prefab)
            {
                throw new InvalidOperationException("Prefab has been destroyed");
            }

            GameObject instance = _parent
                ? UnityEngine.Object.Instantiate(_prefab, _parent, _worldPositionStays)
                : UnityEngine.Object.Instantiate(_prefab);

            instance.SetActive(false);
            return instance;
        }

        public void OnGet(GameObject obj)
        {
            if (obj)
                obj.SetActive(true);
        }

        public void OnReturn(GameObject obj)
        {
            if (!obj)
                return;

            obj.SetActive(false);

            if (_parent && obj.transform.parent != _parent)
            {
                obj.transform.SetParent(_parent, _worldPositionStays);
            }
        }

        public void OnDestroy(GameObject obj)
        {
            if (obj)
                UnityEngine.Object.Destroy(obj);
        }

        public bool Validate(GameObject obj)
        {
            return obj != null;
        }
    }
}
