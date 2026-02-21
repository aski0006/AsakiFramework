using System.Collections.Generic;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Asaki.Core.Network;
using UnityEngine;

namespace Asaki.Core.FrameworkSettings
{
    [CreateAssetMenu(fileName = "AsakiFrameworkSetting", menuName = "Asaki/AsakiFrameworkSetting")]
    public class AsakiFrameworkSetting : ScriptableObject, IAsakiService
    {
        // =========================================================
        // 1. Core Settings
        // =========================================================
        [Header("Simulation Settings")]
        [Range(30, 120)]
        [SerializeField]
        private int tickRate = 60;
        public int TickRate => tickRate;

        [Header("Performance")]
        [SerializeField]
        private int defaultPoolSize = 128;
        public int DefaultPoolSize => defaultPoolSize;

        // =========================================================
        // 2. Global Service Registry
        // =========================================================

        [Header("Global Services")]
        [Tooltip("全局服务注册表，管理所有全局服务预制体的配置")]
        [SerializeField]
        private GlobalServiceRegistry _globalServiceRegistry;
        public GlobalServiceRegistry GlobalServiceRegistry => _globalServiceRegistry;

        // =========================================================
        // 3. Module Configurations (Embedded POCOs)
        // =========================================================

        [Header("Modules: Logging")]
        [SerializeField]
        private AsakiLogConfig logConfig = new AsakiLogConfig();
        public AsakiLogConfig LogConfig => logConfig;

        [Header("Modules: Resources")]
        [SerializeField]
        private AsakiResConfig resConfig = new AsakiResConfig();
        public AsakiResConfig ResConfig => resConfig;

        [Header("Modules: Audio")]
        [SerializeField]
        private AsakiAudioConfig audioConfig = new AsakiAudioConfig();
        public AsakiAudioConfig AudioConfig => audioConfig;

        [Header("Modules: UI")]
        [SerializeField]
        private AsakiUIConfig uiConfig = new AsakiUIConfig();
        public AsakiUIConfig UIConfig => uiConfig;

        [Header("Modules: Web")]
        [SerializeField]
        private AsakiWebConfig webConfig = new AsakiWebConfig();
        public AsakiWebConfig WebConfig => webConfig;

        [Header("Modules: Save")]
        [SerializeField]
        private AsakiSaveConfig saveConfig = new AsakiSaveConfig();
        public AsakiSaveConfig SaveConfig => saveConfig;

        [Header("Modules: Pooling")]
        [SerializeField]
        private AsakiPoolGlobalConfig poolGlobalConfig = new AsakiPoolGlobalConfig();
        public AsakiPoolGlobalConfig PoolGlobalConfig => poolGlobalConfig;

        [Header("Modules: Timer")]
        [SerializeField]
        private AsakiTimerConfig timerConfig = new AsakiTimerConfig();
        public AsakiTimerConfig TimerConfig => timerConfig;

        // =========================================================
        // 4. Runtime Initialization
        // =========================================================

        public void InitializeRuntimeData()
        {
            uiConfig.InitializeLookup();
            audioConfig.InitializeLookup();
        }

        public List<GameObject> GetGlobalServicePrefabs()
        {
            if (_globalServiceRegistry != null)
            {
                return _globalServiceRegistry.GetEnabledPrefabs();
            }
            return new List<GameObject>();
        }
    }
}
