// File: Assets/Asaki/Tests/Resources/Mocks/MockAsakiResStrategy.cs
// 模拟的资源策略，用于测试

using System;
using System.Collections.Generic;
using System.Threading;
using Asaki.Core.Resources;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Asaki.Tests.Resources.Mocks
{
    /// <summary>
    /// 模拟的资源策略，用于测试资源服务
    /// 支持类型感知的资源存储，确保相同location但不同类型的资源能正确区分
    /// </summary>
    public class MockAsakiResStrategy : IAsakiResStrategy
    {
        // 使用复合key: "location|typeFullName" 来区分相同location但不同类型的资源
        private readonly Dictionary<string, Object> _assetDatabase = new();
        private readonly Dictionary<string, Type> _assetTypes = new();
        private readonly List<string> _loadedAssets = new();
        private readonly List<string> _unloadedAssets = new();

        public string StrategyName => "MockStrategy";

        /// <summary>
        /// 加载延迟（毫秒）
        /// </summary>
        public int LoadDelayMs { get; set; } = 10;

        /// <summary>
        /// 是否模拟加载失败
        /// </summary>
        public bool ShouldFail { get; set; } = false;

        /// <summary>
        /// 失败时抛出的异常
        /// </summary>
        public Exception ExceptionToThrow { get; set; }

        /// <summary>
        /// 强制返回的错误类型资源（用于测试类型不匹配场景）
        /// 当设置此值时，无论请求什么类型，都返回这个资源
        /// </summary>
        public Object ForceReturnAsset { get; set; }

        /// <summary>
        /// 初始化调用次数
        /// </summary>
        public int InitializeCallCount { get; private set; }

        /// <summary>
        /// 加载调用记录
        /// </summary>
        public IReadOnlyList<string> LoadedAssets => _loadedAssets;

        /// <summary>
        /// 卸载调用记录
        /// </summary>
        public IReadOnlyList<string> UnloadedAssets => _unloadedAssets;

        /// <summary>
        /// 生成复合key: location + type
        /// </summary>
        private string GetCompositeKey(string location, Type type)
        {
            if (type == null)
                type = typeof(Object);
            return $"{location}|{type.FullName}";
        }

        /// <summary>
        /// 注册模拟资源
        /// 使用location和类型组合作为key，支持相同location但不同类型的资源
        /// </summary>
        public void RegisterAsset<T>(string location, T asset) where T : Object
        {
            string key = GetCompositeKey(location, typeof(T));
            _assetDatabase[key] = asset;
            _assetTypes[key] = typeof(T);
        }

        /// <summary>
        /// 清除所有记录
        /// </summary>
        public void Reset()
        {
            _assetDatabase.Clear();
            _assetTypes.Clear();
            _loadedAssets.Clear();
            _unloadedAssets.Clear();
            InitializeCallCount = 0;
            ShouldFail = false;
            ExceptionToThrow = null;
            ForceReturnAsset = null;
        }

        public UniTask InitializeAsync()
        {
            InitializeCallCount++;
            return UniTask.CompletedTask;
        }

        public async UniTask<Object> LoadAssetInternalAsync(
            string location,
            Type type,
            Action<float> onProgress,
            CancellationToken token
        )
        {
            if (ShouldFail)
            {
                throw ExceptionToThrow ?? new Exception($"[Mock] Failed to load: {location}");
            }

            if (token.IsCancellationRequested)
            {
                throw new OperationCanceledException(token);
            }

            // 模拟加载延迟和进度
            if (LoadDelayMs > 0)
            {
                int steps = 5;
                int stepDelay = LoadDelayMs / steps;

                for (int i = 0; i < steps; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(token);
                    }

                    onProgress?.Invoke((float)i / steps);
                    await UniTask.Delay(stepDelay, cancellationToken: token);
                }
            }

            onProgress?.Invoke(1f);

            // 如果设置了强制返回的资源（用于测试类型不匹配），直接返回
            if (ForceReturnAsset != null)
            {
                _loadedAssets.Add(location);
                return ForceReturnAsset;
            }

            // 使用复合key查找资源
            string compositeKey = GetCompositeKey(location, type);
            if (_assetDatabase.TryGetValue(compositeKey, out var asset))
            {
                _loadedAssets.Add(location);
                return asset;
            }

            // 如果没有预注册资源，创建一个默认的
            var defaultAsset = CreateDefaultAsset(type, location);
            if (defaultAsset != null)
            {
                _loadedAssets.Add(location);
            }

            return defaultAsset;
        }

        public void UnloadAssetInternal(string location, Object asset)
        {
            _unloadedAssets.Add(location);
        }

        public async UniTask UnloadUnusedAssets(CancellationToken token)
        {
            // 模拟卸载延迟
            await UniTask.Delay(5, cancellationToken: token);
        }

        private Object CreateDefaultAsset(Type type, string location)
        {
            if (type == null || type == typeof(Object))
            {
                return new GameObject($"MockAsset_{location}");
            }

            if (type == typeof(GameObject))
            {
                return new GameObject($"MockAsset_{location}");
            }

            if (type == typeof(Sprite))
            {
                // 创建2x2的纹理并转换为Sprite
                var texture = new Texture2D(2, 2);
                return Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
            }

            if (type == typeof(Texture2D))
            {
                return new Texture2D(2, 2);
            }

            if (type == typeof(Material))
            {
                return new Material(Shader.Find("Standard")) ?? new Material(Shader.Find("Diffuse"));
            }

            if (type == typeof(AudioClip))
            {
                // 创建一个空的AudioClip
                return AudioClip.Create($"MockAudio_{location}", 44100, 1, 44100, false);
            }

            // 对于ScriptableObject类型
            if (typeof(ScriptableObject).IsAssignableFrom(type))
            {
                return ScriptableObject.CreateInstance(type);
            }

            return new GameObject($"MockAsset_{location}");
        }
    }
}
