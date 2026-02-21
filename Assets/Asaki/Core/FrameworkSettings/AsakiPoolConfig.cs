using System;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using UnityEngine;

namespace Asaki.Core.FrameworkSettings
{
    /// <summary>
    /// 对象池全局配置类
    /// 集中管理所有对象池相关的配置参数，支持JSON序列化与反序列化
    /// 通过 AsakiFrameworkSetting 进行面板序列化配置
    /// </summary>
    [Serializable]
    public class AsakiPoolGlobalConfig
    {
        #region 基础池配置

        /// <summary>默认初始对象数量</summary>
        public int DefaultInitialSize = 10;

        /// <summary>默认最大对象数量（0表示无限制）</summary>
        public int DefaultMaxSize = 100;

        /// <summary>默认是否启用对象验证</summary>
        public bool DefaultEnableValidation = true;

        /// <summary>默认是否启用集合检查（检测重复归还）</summary>
        public bool DefaultEnableCollectionCheck = true;

        /// <summary>默认是否允许同步创建（当池为空时）</summary>
        public bool DefaultAllowSyncCreation = false;

        /// <summary>默认操作超时时间（秒，0表示无超时）</summary>
        public float DefaultOperationTimeout = 0f;

        /// <summary>默认预热时每帧创建的对象数量</summary>
        public int DefaultPrewarmItemsPerFrame = 5;

        /// <summary>默认池容量（当InitialSize为0时使用）</summary>
        public int DefaultPoolCapacity = 16;

        #endregion

        #region 池治理配置

        /// <summary>默认是否启用自动收缩</summary>
        public bool DefaultEnableAutoShrink = true;

        /// <summary>默认检查间隔（秒）</summary>
        public float DefaultCheckInterval = 30f;

        /// <summary>默认对象闲置多久视为"过期" (TTL, 秒)</summary>
        public float DefaultIdleTimeout = 60f;

        /// <summary>默认收缩时的保底数量</summary>
        public int DefaultKeepMinSize = 5;

        /// <summary>默认每次收缩的比例（0-1，平滑释放）</summary>
        [Range(0f, 1f)]
        public float DefaultShrinkRatio = 0.5f;

        #endregion

        #region 事件池配置

        /// <summary>事件池默认阈值：32字节以下使用结构体，以上使用类</summary>
        public int EventPoolDefaultThreshold = 32;

        /// <summary>事件池内部对象池最大容量</summary>
        public int EventPoolMaxSize = 64;

        #endregion

        #region StringBuilder池配置

        /// <summary>StringBuilder池初始Stack容量</summary>
        public int StringBuilderPoolInitialCapacity = 32;

        /// <summary>StringBuilder池最大保留容量（字节）</summary>
        public int StringBuilderMaxRetainCapacity = 64 * 1024;

        /// <summary>新建StringBuilder时的初始容量</summary>
        public int StringBuilderInitialCapacity = 1024;

        #endregion

        #region 日志命令池配置

        /// <summary>日志命令池最大容量，防止高负载场景下内存无限增长</summary>
        public int LogCommandPoolMaxSize = 256;

        #endregion

        #region 架构池配置

        /// <summary>架构池初始大小（懒加载，无预热）</summary>
        public int ArchitecturePoolInitialSize = 0;

        /// <summary>架构池最大缓存数量</summary>
        public int ArchitecturePoolMaxSize = 128;

        /// <summary>架构池是否启用验证</summary>
        public bool ArchitecturePoolEnableValidation = true;

        /// <summary>架构池是否启用集合检查</summary>
        public bool ArchitecturePoolEnableCollectionCheck = false;

        /// <summary>架构池是否允许同步创建</summary>
        public bool ArchitecturePoolAllowSyncCreation = true;

        #endregion

        #region 音频池配置

        /// <summary>音频池默认初始大小</summary>
        public int AudioPoolDefaultInitialSize = 16;

        /// <summary>音频池默认最大大小</summary>
        public int AudioPoolDefaultMaxSize = 100;

        /// <summary>音频池默认活跃音频字典初始容量</summary>
        public int AudioPoolDefaultActiveAgentCapacity = 32;

        #endregion

        #region 轻量级对象池配置

        /// <summary>轻量级对象池默认最大大小</summary>
        public int LightWeightPoolDefaultMaxSize = 1024;

        #endregion

        #region 静态实例与工厂方法

        private static AsakiPoolGlobalConfig _instance;

        /// <summary>
        /// 获取全局配置单例实例
        /// 优先从 AsakiFrameworkSetting 获取配置，否则使用默认值
        /// </summary>
        public static AsakiPoolGlobalConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = TryGetFromFrameworkSetting() ?? new AsakiPoolGlobalConfig();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 尝试从 AsakiFrameworkSetting 获取配置
        /// </summary>
        private static AsakiPoolGlobalConfig TryGetFromFrameworkSetting()
        {
            try
            {
                if (AsakiContext.TryGet<AsakiFrameworkSetting>(out var setting) && setting != null)
                {
                    var config = setting.PoolGlobalConfig;
                    if (config != null)
                    {
                        ALog.Info(
                            "[AsakiPoolConfig] Loaded pool config from AsakiFrameworkSetting"
                        );
                        return config;
                    }
                }
            }
            catch (System.Exception ex)
            {
                ALog.Warn(
                    $"[AsakiPoolConfig] Failed to get config from AsakiFrameworkSetting: {ex.Message}"
                );
            }
            return null;
        }

        /// <summary>
        /// 设置全局配置实例（由框架初始化时调用）
        /// </summary>
        /// <param name="config">配置实例</param>
        public static void SetInstance(AsakiPoolGlobalConfig config)
        {
            _instance = config;
            ALog.Info("[AsakiPoolConfig] Global config instance set");
        }

        /// <summary>
        /// 从JSON字符串加载配置
        /// </summary>
        /// <param name="json">JSON配置字符串</param>
        /// <returns>配置实例</returns>
        public static AsakiPoolGlobalConfig FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new AsakiPoolGlobalConfig();
            }
            try
            {
                return JsonUtility.FromJson<AsakiPoolGlobalConfig>(json);
            }
            catch (Exception ex)
            {
                ALog.Warn(
                    $"[AsakiPoolConfig] Failed to parse JSON config: {ex.Message}, using defaults"
                );
                return new AsakiPoolGlobalConfig();
            }
        }

        /// <summary>
        /// 将配置序列化为JSON字符串
        /// </summary>
        /// <returns>JSON配置字符串</returns>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        /// <summary>
        /// 重置为默认配置
        /// </summary>
        public void ResetToDefaults()
        {
            DefaultInitialSize = 10;
            DefaultMaxSize = 100;
            DefaultEnableValidation = true;
            DefaultEnableCollectionCheck = true;
            DefaultAllowSyncCreation = false;
            DefaultOperationTimeout = 0f;
            DefaultPrewarmItemsPerFrame = 5;
            DefaultPoolCapacity = 16;

            DefaultEnableAutoShrink = true;
            DefaultCheckInterval = 30f;
            DefaultIdleTimeout = 60f;
            DefaultKeepMinSize = 5;
            DefaultShrinkRatio = 0.5f;

            EventPoolDefaultThreshold = 32;
            EventPoolMaxSize = 64;

            StringBuilderPoolInitialCapacity = 32;
            StringBuilderMaxRetainCapacity = 64 * 1024;
            StringBuilderInitialCapacity = 1024;

            LogCommandPoolMaxSize = 256;

            ArchitecturePoolInitialSize = 0;
            ArchitecturePoolMaxSize = 128;
            ArchitecturePoolEnableValidation = true;
            ArchitecturePoolEnableCollectionCheck = false;
            ArchitecturePoolAllowSyncCreation = true;

            AudioPoolDefaultInitialSize = 16;
            AudioPoolDefaultMaxSize = 100;
            AudioPoolDefaultActiveAgentCapacity = 32;

            LightWeightPoolDefaultMaxSize = 1024;
        }

        /// <summary>
        /// 应用配置到全局实例
        /// </summary>
        public void Apply()
        {
            _instance = this;
        }

        #endregion
    }
}
