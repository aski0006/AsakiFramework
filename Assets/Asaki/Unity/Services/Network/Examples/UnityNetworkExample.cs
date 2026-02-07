using System;
using System.Threading;
using Asaki.Core.Network;
using Asaki.Core.Serialization;
using Asaki.Unity.Services.Serialization;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Asaki.Unity.Services.Network.Examples
{
    /// <summary>
    /// Unity环境下网络请求响应系统使用示例
    /// </summary>
    /// <remarks>
    /// 本示例展示了如何在Unity中使用Asaki框架的请求/响应类进行网络通信
    /// </remarks>
    public class UnityNetworkExample : MonoBehaviour
    {
        #region 数据模型

        /// <summary>
        /// 用户数据
        /// </summary>
        [Serializable]
        public class UserData : IAsakiSavable
        {
            public string UserId;
            public string Username;
            public int Level;

            public void Serialize(IAsakiWriter writer)
            {
                writer.WriteString("userId", UserId);
                writer.WriteString("username", Username);
                writer.WriteInt("level", Level);
            }

            public void Deserialize(IAsakiReader reader)
            {
                UserId = reader.ReadString("userId");
                Username = reader.ReadString("username");
                Level = reader.ReadInt("level");
            }
        }

        /// <summary>
        /// 登录数据
        /// </summary>
        [Serializable]
        public class LoginData : IAsakiSavable
        {
            public string Username;
            public string Password;

            public void Serialize(IAsakiWriter writer)
            {
                writer.WriteString("username", Username);
                writer.WriteString("password", Password);
            }

            public void Deserialize(IAsakiReader reader)
            {
                Username = reader.ReadString("username");
                Password = reader.ReadString("password");
            }
        }

        #endregion

        #region 请求响应类

        /// <summary>
        /// 登录请求
        /// </summary>
        public class LoginRequest : AsakiRequest<LoginData> { }

        /// <summary>
        /// 登录响应
        /// </summary>
        public class LoginResponse : AsakiResponse<UserData> { }

        /// <summary>
        /// 获取用户信息响应
        /// </summary>
        public class GetUserResponse : AsakiResponse<UserData> { }

        #endregion

        #region 使用示例

        /// <summary>
        /// 示例：使用工厂方法创建请求
        /// </summary>
        private void Example_CreateRequest()
        {
            // 使用工厂方法创建请求
            var request = AsakiRequest<LoginData>
                .Create()
                .WithData(new LoginData { Username = "player123", Password = "password123" });

            Debug.Log($"请求ID: {request.RequestId}");
        }

        /// <summary>
        /// 示例：使用工厂方法创建响应
        /// </summary>
        private void Example_CreateResponse()
        {
            // 创建成功响应
            var successResponse = AsakiResponse<UserData>.Success(
                new UserData
                {
                    UserId = "123",
                    Username = "Player",
                    Level = 10,
                },
                "获取用户信息成功"
            );

            // 创建失败响应
            var errorResponse = AsakiResponse<UserData>.Failure(
                AsakiResponseCode.ResourceNotFound,
                "用户不存在"
            );
        }

        /// <summary>
        /// 示例：使用API结果包装器
        /// </summary>
        private async UniTask Example_ApiResult(IAsakiWebService webService)
        {
            // 发送请求并获取API结果
            var result = await webService.GetAsync<GetUserResponse>("/api/user/info").ToApiResult();

            // 使用Match处理结果
            result.Match(
                onSuccess: response =>
                {
                    Debug.Log($"用户: {response.Data.Username}, 等级: {response.Data.Level}");
                },
                onFailure: (code, message) =>
                {
                    Debug.LogError($"请求失败: [{code}] {message}");
                }
            );

            // 使用链式调用
            result
                .OnSuccess(response =>
                {
                    Debug.Log("处理成功逻辑");
                })
                .OnFailure(
                    (code, message) =>
                    {
                        Debug.Log("处理失败逻辑");
                    }
                );
        }

        /// <summary>
        /// 示例：序列化和反序列化
        /// </summary>
        private void Example_Serialization()
        {
            // 创建请求
            var request = AsakiRequest<LoginData>
                .Create()
                .WithData(new LoginData { Username = "test", Password = "123456" });

            // 序列化为JSON
            string json = request.ToJsonRequest();
            Debug.Log($"序列化后的JSON: {json}");

            // 从JSON反序列化
            if (
                AsakiNetworkSerializer.TryDeserializeRequest(
                    json,
                    out LoginRequest deserializedRequest
                )
            )
            {
                Debug.Log($"反序列化成功: {deserializedRequest.Data.Username}");
            }
        }

        /// <summary>
        /// 示例：完整的登录流程
        /// </summary>
        private async UniTask Example_FullLoginFlow(IAsakiWebService webService)
        {
            // 创建登录请求数据
            var loginData = new LoginData { Username = "player123", Password = "securepassword" };

            // 创建登录请求
            var loginRequest = new LoginRequest();
            loginRequest.Data = loginData;

            // 验证请求
            var validation = loginRequest.Validate();
            if (!validation.IsValid)
            {
                Debug.LogError($"请求验证失败: {validation.ErrorMessage}");
                return;
            }

            // 发送登录请求
            var result = await webService
                .PostAsync<LoginRequest, LoginResponse>("/api/auth/login", loginRequest)
                .ToApiResult();

            // 处理结果
            result.Match(
                onSuccess: response =>
                {
                    Debug.Log($"登录成功! 欢迎, {response.Data.Username}");
                    Debug.Log($"用户等级: {response.Data.Level}");
                },
                onFailure: (code, message) =>
                {
                    if (AsakiResponseCode.IsAuthError(code))
                    {
                        Debug.LogWarning($"认证失败: {message}");
                    }
                    else if (AsakiResponseCode.IsNetworkError(code))
                    {
                        Debug.LogError($"网络错误: {message}");
                    }
                    else
                    {
                        Debug.LogError($"登录失败: [{code}] {message}");
                    }
                }
            );
        }

        #endregion
    }
}
