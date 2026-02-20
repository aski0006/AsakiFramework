using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Asaki.Core.Logging;
using Asaki.Core.Resources;
using Cysharp.Threading.Tasks;

namespace Asaki.Unity.Services.Resources.Preloader
{
    /// <summary>
    /// 预加载状态枚举
    /// </summary>
    public enum PreloadState
    {
        /// <summary>未开始加载</summary>
        Idle,

        /// <summary>正在加载中</summary>
        Loading,

        /// <summary>加载完成</summary>
        Completed,

        /// <summary>加载失败</summary>
        Failed,
    }

    /// <summary>
    /// 预加载执行器
    /// <para>负责执行资源加载操作，管理加载状态和进度报告。</para>
    /// <para>遵循单一职责原则，仅处理加载执行相关逻辑。</para>
    /// </summary>
    public class PreloadExecutor : IDisposable
    {
        private readonly IAsakiResourceService _resourceService;
        private readonly PreloadConfigProvider _configProvider;
        private readonly PreloadResourceRegistry _registry;

        private CancellationTokenSource _loadingCts;

        /// <summary>
        /// 当前加载状态
        /// </summary>
        public PreloadState State { get; private set; } = PreloadState.Idle;

        /// <summary>
        /// 当前加载进度 (0-1)
        /// </summary>
        public float Progress { get; private set; }

        /// <summary>
        /// 是否加载完成
        /// </summary>
        public bool IsCompleted => State == PreloadState.Completed;

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading => State == PreloadState.Loading;

        /// <summary>
        /// 进度变化事件
        /// </summary>
        public event Action<float> OnProgressChanged;

        /// <summary>
        /// 加载完成事件
        /// </summary>
        public event Action OnCompleted;

        /// <summary>
        /// 加载失败事件
        /// </summary>
        public event Action<Exception> OnFailed;

        /// <summary>
        /// 单个资源组加载完成事件
        /// </summary>
        public event Action<string> OnGroupCompleted;

        /// <summary>
        /// 创建预加载执行器实例
        /// </summary>
        /// <param name="resourceService">资源服务</param>
        /// <param name="configProvider">配置提供者</param>
        /// <param name="registry">资源注册表</param>
        public PreloadExecutor(
            IAsakiResourceService resourceService,
            PreloadConfigProvider configProvider,
            PreloadResourceRegistry registry
        )
        {
            _resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        /// 开始预加载所有配置的资源
        /// </summary>
        /// <param name="loadInParallel">是否并行加载各组</param>
        /// <param name="onProgress">进度回调</param>
        /// <returns>加载任务</returns>
        public async UniTask StartPreloadAsync(bool loadInParallel = true, Action<float> onProgress = null)
        {
            if (State == PreloadState.Loading)
            {
                return;
            }

            if (_resourceService == null)
            {
                State = PreloadState.Failed;
                var ex = new InvalidOperationException("ResourceService not available.");
                OnFailed?.Invoke(ex);
                throw ex;
            }

            _loadingCts?.Dispose();
            _loadingCts = new CancellationTokenSource();
            var token = _loadingCts.Token;

            State = PreloadState.Loading;
            Progress = 0f;

            try
            {
                if (loadInParallel)
                {
                    await LoadGroupsInParallelAsync(onProgress, token);
                }
                else
                {
                    await LoadGroupsSequentiallyAsync(onProgress, token);
                }

                State = PreloadState.Completed;
                Progress = 1f;

                OnProgressChanged?.Invoke(1f);
                OnCompleted?.Invoke();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                State = PreloadState.Failed;
                OnFailed?.Invoke(ex);
                throw;
            }
        }

        /// <summary>
        /// 加载指定的资源组
        /// </summary>
        /// <param name="groupName">资源组名称</param>
        /// <param name="onProgress">进度回调</param>
        /// <returns>加载任务</returns>
        public async UniTask LoadGroupAsync(string groupName, Action<float> onProgress = null)
        {
            var group = _configProvider.GetGroup(groupName);
            if (group == null)
            {
                throw new ArgumentException($"Resource group '{groupName}' not found.");
            }

            var token = _loadingCts?.Token ?? CancellationToken.None;
            await LoadGroupInternalAsync(group, onProgress, token);
        }

        /// <summary>
        /// 取消正在进行的加载
        /// </summary>
        public void CancelLoading()
        {
            _loadingCts?.Cancel();
        }

        /// <summary>
        /// 重置状态
        /// </summary>
        public void Reset()
        {
            State = PreloadState.Idle;
            Progress = 0f;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _loadingCts?.Cancel();
            _loadingCts?.Dispose();
            _loadingCts = null;
        }

        private async UniTask LoadGroupsSequentiallyAsync(Action<float> onProgress, CancellationToken token)
        {
            var groups = _configProvider.ResourceGroups;
            float[] groupProgresses = new float[groups.Count];

            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                int groupIndex = i;

                if (group.Resources.Count == 0)
                {
                    groupProgresses[i] = 1f;
                    continue;
                }

                await LoadGroupInternalAsync(
                    group,
                    p =>
                    {
                        groupProgresses[groupIndex] = p;
                        UpdateOverallProgress(groupProgresses, onProgress);
                    },
                    token
                );

                groupProgresses[i] = 1f;
                UpdateOverallProgress(groupProgresses, onProgress);
            }
        }

        private async UniTask LoadGroupsInParallelAsync(Action<float> onProgress, CancellationToken token)
        {
            var groups = _configProvider.ResourceGroups;
            float[] groupProgresses = new float[groups.Count];

            var tasks = new List<UniTask>();

            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                int groupIndex = i;

                if (group.Resources.Count == 0)
                {
                    groupProgresses[i] = 1f;
                    continue;
                }

                var task = LoadGroupInternalAsync(
                    group,
                    p =>
                    {
                        groupProgresses[groupIndex] = p;
                        UpdateOverallProgress(groupProgresses, onProgress);
                    },
                    token
                );

                tasks.Add(task);
            }

            if (tasks.Count > 0)
            {
                await UniTask.WhenAll(tasks);
            }
        }

        private async UniTask LoadGroupInternalAsync(
            ResourceGroup group,
            Action<float> onProgress,
            CancellationToken token
        )
        {
            var entries = group.GetValidEntries().ToList();

            if (entries.Count == 0)
            {
                onProgress?.Invoke(1f);
                return;
            }

            float[] progresses = new float[entries.Count];

            for (int i = 0; i < entries.Count; i++)
            {
                int index = i;
                var entry = entries[i];
                var type = entry.GetActualType();

                Action<float> itemProgress = p =>
                {
                    progresses[index] = p;
                    float overall = progresses.Average();
                    onProgress?.Invoke(overall);
                };

                var handle = await _resourceService.LoadAsync(
                    entry.Location,
                    type,
                    itemProgress,
                    token
                );

                _registry.Register(entry.Location, type, handle, group.GroupName);
                progresses[i] = 1f;
            }

            onProgress?.Invoke(1f);
            OnGroupCompleted?.Invoke(group.GroupName);
        }

        private void UpdateOverallProgress(float[] groupProgresses, Action<float> onProgress)
        {
            float overall = groupProgresses.Average();
            Progress = overall;
            onProgress?.Invoke(overall);
            OnProgressChanged?.Invoke(overall);
        }
    }
}
