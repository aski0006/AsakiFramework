using Asaki.Core.Pooling.Interfaces;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace Asaki.Core.Pooling.Factories
{
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

		public UniTask<GameObject> CreateAsync(CancellationToken token = default)
		{
			if (!_prefab)
			{
				throw new InvalidOperationException("预制体已被销毁");
			}

			GameObject instance = _parent
				? UnityEngine.Object.Instantiate(_prefab, _parent, _worldPositionStays)
				: UnityEngine.Object.Instantiate(_prefab);

			instance.SetActive(false); // 默认创建时禁用

			return UniTask.FromResult(instance);
		}

		public void OnGet(GameObject obj)
		{
			if (obj) obj.SetActive(true);
		}

		public void OnReturn(GameObject obj)
		{
			if (!obj) return;

			obj.SetActive(false);

			// 归位到父节点
			if (_parent && obj.transform.parent != _parent)
			{
				obj.transform.SetParent(_parent, _worldPositionStays);
			}
		}

		public void OnDestroy(GameObject obj)
		{
			if (obj) UnityEngine.Object.Destroy(obj);
		}

		public bool Validate(GameObject obj)
		{
			return obj;
		}
	}
}
