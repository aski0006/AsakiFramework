using System;
using System.IO;
using System.Threading;
using Asaki.Core.Async;
using Asaki.Core.Context;
using Asaki.Core.Logging;
using Asaki.Core.Network;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Asaki.Unity.Services.Scene.SceneManagement.Scripts.Examples
{
    public class AsakiDownloadTest : MonoBehaviour
    {
        private IAsakiDownloadService _downloader;
        private IAsakiAsyncService _asakiAsyncService;
        private CancellationTokenSource _cts;

        private void Start()
        {
            _downloader = AsakiContext.Get<IAsakiDownloadService>();
            _asakiAsyncService = AsakiContext.Get<IAsakiAsyncService>();
        }

        [ContextMenu("Test Download Image")]
        public async UniTaskVoid TestDownload()
        {
            // 测试长链接
            string url =
                "https://image.baidu.com/search/down?&tn=download&word=download&ie=utf8&fr=home&url=https%3A%2F%2Fgips2.baidu.com%2Fit%2Fu%3D1651586290%2C17201034%26fm%3D3028%26app%3D3028%26f%3DJPEG%26fmt%3Dauto%26q%3D100%26size%3Df600_800&thumbUrl=https%3A%2F%2Fgips2.baidu.com%2Fit%2Fu%3D1651586290%2C17201034%26fm%3D3028%26app%3D3028%26f%3DJPEG%26fmt%3Dauto%26q%3D100%26size%3Df600_800&iswise=1";

            // [W-006] 路径必须在 PersistentDataPath 下
            string savePath = Path.Combine(
                Application.persistentDataPath,
                "Downloads",
                "test_image.jpg"
            );

            ALog.Info($"[Test] Start downloading to: {savePath}");

            // [M-005] 适配新的进度结构体 AsakiDownloadProgress
            var progress = new Progress<AsakiDownloadProgress>(p =>
            {
                // 展示更多维度的信息：进度、速度、已下载量
                // p.Speed 单位是 Bytes/s，转换为 KB/s
                ALog.Info(
                    $"[Progress] {p.Progress:P0} | Speed: {p.Speed / 1024f:F1} KB/s | Downloaded: {p.DownloadedBytes / 1024} KB"
                );
            });

            _cts = new CancellationTokenSource();

            // [Fix] 修正 API 调用：使用 IAsakiAsyncService 定义的 CreateLinkedToken
            var linkedToken = _asakiAsyncService.CreateLinkedToken(_cts.Token);

            try
            {
                // 获取文件大小 (HEAD 请求)
                long size = await _downloader.GetFileSizeAsync(url);
                ALog.Info($"[Test] Remote File Size: {size / 1024} KB");

                // 开始下载
                await _downloader.DownloadAsync(url, savePath, progress, linkedToken);

                ALog.Info("<color=green>[Test] Download Success!</color>");

                // 验证文件存在
                if (File.Exists(savePath))
                {
                    ALog.Info(
                        $"[Verify] File exists, local size: {new FileInfo(savePath).Length} bytes"
                    );
                }
            }
            catch (OperationCanceledException)
            {
                ALog.Warn("[Test] Download Canceled.");
            }
            catch (Exception ex)
            {
                ALog.Error($"[Test] Failed: {ex.Message}", ex);
            }
        }

        [ContextMenu("Cancel Download")]
        public void Cancel()
        {
            _cts?.Cancel();
            ALog.Info("[Test] Cancel requested...");
        }
    }
}
