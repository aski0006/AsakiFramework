using System;
using Asaki.Core.Network;
using Asaki.Core.Serialization;

namespace Asaki.Core.Network.Examples
{
    /// <summary>
    /// 网络请求响应系统使用示例
    /// </summary>
    /// <remarks>
    /// 本示例展示了如何使用Asaki框架的请求/响应类来实现标准的API通信
    /// </remarks>
    public static class NetworkRequestResponseExample
    {
        #region 数据模型定义

        /// <summary>
        /// 登录请求数据
        /// </summary>
        public class LoginData : IAsakiSavable
        {
            public string Username { get; set; }
            public string Password { get; set; }

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

        /// <summary>
        /// 登录响应数据
        /// </summary>
        public class LoginResultData : IAsakiSavable
        {
            public string Token { get; set; }
            public long ExpiresAt { get; set; }
            public string UserId { get; set; }

            public void Serialize(IAsakiWriter writer)
            {
                writer.WriteString("token", Token);
                writer.WriteLong("expiresAt", ExpiresAt);
                writer.WriteString("userId", UserId);
            }

            public void Deserialize(IAsakiReader reader)
            {
                Token = reader.ReadString("token");
                ExpiresAt = reader.ReadLong("expiresAt");
                UserId = reader.ReadString("userId");
            }
        }

        /// <summary>
        /// 玩家信息数据
        /// </summary>
        public class PlayerInfoData : IAsakiSavable
        {
            public string Nickname { get; set; }
            public int Level { get; set; }
            public int Exp { get; set; }

            public void Serialize(IAsakiWriter writer)
            {
                writer.WriteString("nickname", Nickname);
                writer.WriteInt("level", Level);
                writer.WriteInt("exp", Exp);
            }

            public void Deserialize(IAsakiReader reader)
            {
                Nickname = reader.ReadString("nickname");
                Level = reader.ReadInt("level");
                Exp = reader.ReadInt("exp");
            }
        }

        #endregion

        #region 自定义请求响应类

        /// <summary>
        /// 登录请求
        /// </summary>
        public class LoginRequest : AsakiRequestBase<LoginData>
        {
            public LoginRequest() { }

            public LoginRequest(string username, string password)
            {
                Data.Username = username;
                Data.Password = password;
            }

            public override AsakiRequestValidationResult Validate()
            {
                if (string.IsNullOrEmpty(Data.Username))
                    return AsakiRequestValidationResult.Failure("用户名不能为空");

                if (string.IsNullOrEmpty(Data.Password))
                    return AsakiRequestValidationResult.Failure("密码不能为空");

                if (Data.Password.Length < 6)
                    return AsakiRequestValidationResult.Failure("密码长度不能少于6位");

                return AsakiRequestValidationResult.Success;
            }
        }

        /// <summary>
        /// 登录响应
        /// </summary>
        public class LoginResponse : AsakiResponseBase<LoginResultData>
        {
            /// <summary>
            /// 创建成功的登录响应
            /// </summary>
            public static LoginResponse Success(LoginResultData data, string message = null)
            {
                var response = new LoginResponse();
                response.SetSuccess(data, message);
                return response;
            }

            /// <summary>
            /// 创建失败的登录响应
            /// </summary>
            public static LoginResponse Failure(int code, string message = null)
            {
                var response = new LoginResponse();
                response.SetError(code, message);
                return response;
            }
        }

        /// <summary>
        /// 获取玩家信息请求（无额外数据）
        /// </summary>
        public class GetPlayerInfoRequest : AsakiRequestBase
        {
            protected override void SerializeCore(IAsakiWriter writer) { }

            protected override void DeserializeCore(IAsakiReader reader) { }
        }

        /// <summary>
        /// 获取玩家信息响应
        /// </summary>
        public class GetPlayerInfoResponse : AsakiResponseBase<PlayerInfoData> { }

        #endregion

        #region 使用示例

        /// <summary>
        /// 示例：创建和验证请求
        /// </summary>
        public static void Example_CreateAndValidateRequest()
        {
            // 创建登录请求
            var loginRequest = new LoginRequest("player123", "password123");

            // 验证请求
            var validation = loginRequest.Validate();
            if (!validation.IsValid)
            {
                Console.WriteLine($"请求验证失败: {validation.ErrorMessage}");
                return;
            }

            // 序列化请求
            Console.WriteLine($"请求ID: {loginRequest.RequestId}");
            Console.WriteLine($"请求时间戳: {loginRequest.Timestamp}");
        }

        /// <summary>
        /// 示例：创建响应
        /// </summary>
        public static void Example_CreateResponse()
        {
            // 创建成功响应 - 使用工厂方法
            var successResponse = LoginResponse.Success(
                new LoginResultData
                {
                    Token = "eyJhbGciOiJIUzI1NiIs...",
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeMilliseconds(),
                    UserId = "user_12345",
                },
                "登录成功"
            );

            Console.WriteLine($"响应状态码: {successResponse.Code}");
            Console.WriteLine($"响应消息: {successResponse.Message}");
            Console.WriteLine($"是否成功: {successResponse.IsSuccess}");
            Console.WriteLine($"Token: {successResponse.Data.Token}");

            // 创建失败响应 - 使用工厂方法
            var errorResponse = LoginResponse.Failure(
                AsakiResponseCode.InvalidParameter,
                "用户名或密码错误"
            );

            Console.WriteLine($"错误码: {errorResponse.Code}");
            Console.WriteLine($"错误消息: {errorResponse.Message}");
        }

        /// <summary>
        /// 示例：使用标准响应码
        /// </summary>
        public static void Example_UseResponseCodes()
        {
            // 检查状态码类型
            Console.WriteLine(
                $"Success是否为成功: {AsakiResponseCode.IsSuccess(AsakiResponseCode.Success)}"
            );
            Console.WriteLine(
                $"InvalidParameter是否为客户错误: {AsakiResponseCode.IsClientError(AsakiResponseCode.InvalidParameter)}"
            );
            Console.WriteLine(
                $"TokenExpired是否为授权错误: {AsakiResponseCode.IsAuthError(AsakiResponseCode.TokenExpired)}"
            );
            Console.WriteLine(
                $"ServerError是否为服务器错误: {AsakiResponseCode.IsServerError(AsakiResponseCode.ServerError)}"
            );

            // 获取默认消息
            string message = AsakiResponseCode.GetDefaultMessage(AsakiResponseCode.TokenExpired);
            Console.WriteLine($"Token过期默认消息: {message}");
        }

        #endregion
    }
}
