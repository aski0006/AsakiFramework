using System;
using Asaki.Core.Context;
using Asaki.Core.Network;
using Asaki.Core.Serialization;
using UnityEngine;

namespace Asaki.Unity.Services.Scene.SceneManagement.Scripts.Examples
{
    // =========================================================
    // 1. 定义 DTO (模拟 Roslyn 生成的代码)
    // =========================================================

    // 模拟登录请求
    public class MockLoginReq : IAsakiSavable
    {
        public string Username;
        public string Password;
        public int ClientVersion;

        public void Serialize(IAsakiWriter writer)
        {
            writer.BeginObject("MockLoginReq");
            writer.WriteString("Username", Username);
            writer.WriteString("Password", Password);
            writer.WriteInt("ClientVersion", ClientVersion);
            writer.EndObject();
        }

        public void Deserialize(IAsakiReader reader)
        {
            Username = reader.ReadString("Username");
            Password = reader.ReadString("Password");
            ClientVersion = reader.ReadInt("ClientVersion");
        }
    }

    // 模拟 HttpBin 的响应结构
    // HttpBin 会把我们发的 json 放在 "json" 字段里返回
    public class HttpBinResponse : IAsakiSavable
    {
        public string origin; // 请求IP
        public string url;
        public MockLoginReq json; // 嵌套对象测试

        public void Serialize(
            IAsakiWriter writer
        ) { /* 只读，不需要写 */
        }

        public void Deserialize(IAsakiReader reader)
        {
            origin = reader.ReadString("origin");
            url = reader.ReadString("url");
            // 测试嵌套反序列化
            json = reader.ReadObject<MockLoginReq>("json");
        }
    }

    // =========================================================
    // 2. 测试逻辑
    // =========================================================

    public class AsakiWebTest : MonoBehaviour
    {
        private IAsakiWebService _webService;

        private void Start()
        {
            _webService = AsakiContext.Get<IAsakiWebService>();
        }

        [ContextMenu("Run Web Test")]
        public async void RunTest()
        {
            Debug.Log("<color=yellow>[AsakiWebTest] === Start Integration Test ===</color>");

            try
            {
                // --- Step 1: 测试 POST JSON ---
                Debug.Log("[Test 1] Testing POST JSON...");

                var req = new MockLoginReq
                {
                    Username = "AsakiUser",
                    Password = "SuperSecretPassword",
                    ClientVersion = 500,
                };

                // 发送请求
                var res = await _webService.PostAsync<MockLoginReq, HttpBinResponse>("post", req);

                // 验证结果
                if (res.json != null && res.json.Username == "AsakiUser")
                {
                    Debug.Log(
                        $"<color=green>[Success] POST Response Validated!</color>\nEcho IP: {res.origin}"
                    );
                }
                else
                {
                    Debug.LogError("[Fail] POST Response Data Mismatch!");
                }

                // --- Step 2: 测试 GET (模拟) ---
                Debug.Log("[Test 2] Testing GET...");
                // httpbin/get 返回结构类似，我们复用 HttpBinResponse 里的 url 字段验证
                var getRes = await _webService.GetAsync<HttpBinResponse>("get?foo=bar");

                if (getRes.url.Contains("foo=bar"))
                {
                    Debug.Log(
                        $"<color=green>[Success] GET Response Validated!</color>\nURL: {getRes.url}"
                    );
                }
                else
                {
                    Debug.LogError("[Fail] GET Response URL Mismatch");
                }

                // --- Step 3: 测试异常捕获 (404) ---
                Debug.Log("[Test 3] Testing Error Handling (404)...");
                try
                {
                    await _webService.GetAsync<HttpBinResponse>("status/404");
                    Debug.LogError("[Fail] Should verify 404 but got success!");
                }
                catch (AsakiWebException ex)
                {
                    if (ex.ResponseCode == 404)
                    {
                        Debug.Log(
                            $"<color=green>[Success] Caught expected 404 error: {ex.Message}</color>"
                        );
                    }
                    else
                    {
                        Debug.LogError($"[Fail] Caught wrong error code: {ex.ResponseCode}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Critical Fail] Unexpected Exception: {e}");
            }

            Debug.Log("<color=yellow>[AsakiWebTest] === Test Finished ===</color>");
        }
    }
}
