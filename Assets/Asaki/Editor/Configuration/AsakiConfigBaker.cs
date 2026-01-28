using System.IO;
using Asaki.Core.Broker;
using Asaki.Unity.Services.Configuration;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Configuration
{
    public static class AsakiConfigBaker
    {
        [MenuItem("Asaki/Configuration/Bake All Configs to Binary")]
        public static async void BakeAllConfigs()
        {
            // 1. 准备路径
            // 注意：这里我们把 Bin 放在 StreamingAssets 旁边，或者直接覆盖
            // 通常做法是：开发用 CSV，发布只留 Bin。
            // 这里演示生成到 PersistentDataPath 供测试，或者生成到 StreamingAssets/Binary 供打包

            string csvDir = Path.Combine(Application.streamingAssetsPath, "Configs");
            // 假设我们发布时把 Bin 放在 StreamingAssets/ConfigBin
            string binOutDir = Path.Combine(Application.streamingAssetsPath, "ConfigBin");

            if (!Directory.Exists(binOutDir))
                Directory.CreateDirectory(binOutDir);

            // 2. 初始化服务 (需要借助 Service 的解析能力)
            AsakiEventService eventService = new AsakiEventService();
            AsakiConfigService service = new AsakiConfigService(eventService);
            // 手动注入路径 (因为 EditMode 没有 Application.persistentDataPath 的运行时逻辑)
            // 这里我们用反射或者临时修改 Service 逻辑来支持烘焙，
            // 或者更简单：直接运行游戏，Service 会自动在 PersistentDataPath 生成 Bin。

            // 由于 Service 逻辑比较依赖运行时，我们这里用一种 "模拟运行" 的方式：
            // 直接调用 Service 的 LoadInternalAsync，它会自动生成缓存文件。

            // 既然 LoadInternalAsync 已经有 Auto-Bake 功能，我们只需要触发所有表的加载即可！
            // 但 LoadInternalAsync 写入的是 PersistentDataPath。
            // 我们可以在这里做一个文件搬运。

            Debug.Log("Start Baking...");

            // 必须先初始化
            service.OnInit();

            // 加载所有表 (这会触发 Auto-Bake 到 PersistentDataPath)
            await service.LoadAllAsync();

            // 3. 搬运文件
            string cacheDir = Path.Combine(Application.persistentDataPath, "ConfigCache");
            string[] binFiles = Directory.GetFiles(cacheDir, "*.bin");

            foreach (string binPath in binFiles)
            {
                string fileName = Path.GetFileName(binPath);
                string destPath = Path.Combine(binOutDir, fileName);
                File.Copy(binPath, destPath, true);
                Debug.Log($"Baked: {fileName}");
            }

            AssetDatabase.Refresh();
            Debug.Log($"Baking Complete! Output: {binOutDir}");
        }
    }
}
