using Asaki.Core.Context;
using Asaki.Core.Logging;
using Asaki.Core.Network;
using UnityEngine;

namespace Asaki.Core.Configs
{
    [CreateAssetMenu(fileName = "AsakiConfig", menuName = "Asaki/AsakiConfig")]
    public class AsakiConfig : ScriptableObject, IAsakiService
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
        // 2. Module Configurations (Embedded POCOs)
        // =========================================================

        [Header("Modules: Logging")]
        [SerializeField]
        private AsakiLogConfig logConfig = new AsakiLogConfig();
        public AsakiLogConfig LogConfig => logConfig;

        [Header("Modules: Resources")]
        [SerializeField]
        private AsakiResConfig resConfig = new AsakiResConfig(); // 可以在字段上直接初始化默认值
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

        [Header("Modules: Localization")]
        [SerializeField]
        private AsakiLocalizationConfig localizationConfig = new AsakiLocalizationConfig();
        public AsakiLocalizationConfig LocalizationConfig => localizationConfig;

        // =========================================================
        // 3. Runtime Initialization
        // =========================================================

        // 可选：在运行时初始化字典等缓存结构
        public void InitializeRuntimeData()
        {
            uiConfig.InitializeLookup();
            audioConfig.InitializeLookup();
        }
    }
}
