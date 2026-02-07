using Asaki.Core.Network;
using Asaki.Core.Serialization;
using Asaki.Unity.Services.Network;
using NUnit.Framework;

namespace Asaki.Tests.Network
{
    /// <summary>
    /// 测试用的请求数据
    /// </summary>
    public class LoginRequestData : IAsakiSavable
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
    /// 测试用的响应数据
    /// </summary>
    public class LoginResponseData : IAsakiSavable
    {
        public string Token { get; set; }
        public long ExpiresAt { get; set; }

        public void Serialize(IAsakiWriter writer)
        {
            writer.WriteString("token", Token);
            writer.WriteLong("expiresAt", ExpiresAt);
        }

        public void Deserialize(IAsakiReader reader)
        {
            Token = reader.ReadString("token");
            ExpiresAt = reader.ReadLong("expiresAt");
        }
    }

    /// <summary>
    /// AsakiRequest 和 AsakiResponse 单元测试
    /// </summary>
    [TestFixture]
    public class AsakiRequestResponseTests
    {
        #region AsakiRequest 测试

        [Test]
        [Category("Unit")]
        [Description("测试AsakiRequest.Create创建请求")]
        public void AsakiRequest_Create_ReturnsNewRequest()
        {
            // Act
            var request = AsakiRequest.Create();

            // Assert
            Assert.IsNotNull(request);
            Assert.IsInstanceOf<AsakiRequest>(request);
            Assert.IsNotNull(request.RequestId);
        }

        [Test]
        [Category("Unit")]
        [Description("测试AsakiRequest继承基类功能")]
        public void AsakiRequest_InheritsBaseFunctionality()
        {
            // Arrange
            var request = AsakiRequest.Create();

            // Assert
            Assert.IsNotNull(request.RequestId);
            Assert.Greater(request.Timestamp, 0);
            Assert.IsTrue(request.Validate().IsValid);
        }

        [Test]
        [Category("Unit")]
        [Description("测试AsakiRequest<T>.Create创建泛型请求")]
        public void AsakiRequestGeneric_Create_ReturnsNewRequest()
        {
            // Act
            var request = AsakiRequest<LoginRequestData>.Create();

            // Assert
            Assert.IsNotNull(request);
            Assert.IsNotNull(request.Data);
            Assert.IsInstanceOf<LoginRequestData>(request.Data);
        }

        [Test]
        [Category("Unit")]
        [Description("测试AsakiRequest<T>.Create带数据创建请求")]
        public void AsakiRequestGeneric_CreateWithData_SetsData()
        {
            // Arrange
            var data = new LoginRequestData { Username = "test", Password = "pass" };

            // Act
            var request = AsakiRequest<LoginRequestData>.Create(data);

            // Assert
            Assert.AreEqual("test", request.Data.Username);
            Assert.AreEqual("pass", request.Data.Password);
        }

        [Test]
        [Category("Unit")]
        [Description("测试AsakiRequest<T>.WithData链式设置数据")]
        public void AsakiRequestGeneric_WithData_ChainsCorrectly()
        {
            // Arrange
            var data = new LoginRequestData { Username = "test", Password = "pass" };

            // Act
            var request = AsakiRequest<LoginRequestData>.Create().WithData(data);

            // Assert
            Assert.AreEqual("test", request.Data.Username);
            Assert.AreEqual("pass", request.Data.Password);
        }

        [Test]
        [Category("Unit")]
        [Description("测试AsakiRequest<T>支持方法链式调用")]
        public void AsakiRequestGeneric_Methods_SupportChaining()
        {
            // Act
            var request = AsakiRequest<LoginRequestData>
                .Create()
                .WithData(new LoginRequestData { Username = "user", Password = "pwd" });

            // Assert
            Assert.IsNotNull(request);
            Assert.AreEqual("user", request.Data.Username);
        }

        #endregion

        #region AsakiResponse 测试

        [Test]
        [Category("Unit")]
        [Description("测试AsakiResponse.Success创建成功响应")]
        public void AsakiResponse_Success_CreatesSuccessResponse()
        {
            // Act
            var response = AsakiResponse.Success();

            // Assert
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual(AsakiResponseCode.Success, response.Code);
        }

        [Test]
        [Category("Unit")]
        [Description("测试AsakiResponse.Success带消息创建成功响应")]
        public void AsakiResponse_SuccessWithMessage_CreatesResponseWithMessage()
        {
            // Act
            var response = AsakiResponse.Success("操作成功");

            // Assert
            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("操作成功", response.Message);
        }

        [Test]
        [Category("Unit")]
        [Description("测试AsakiResponse.Failure创建失败响应")]
        public void AsakiResponse_Failure_CreatesFailureResponse()
        {
            // Act
            var response = AsakiResponse.Failure(AsakiResponseCode.InvalidParameter);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(AsakiResponseCode.InvalidParameter, response.Code);
        }

        [Test]
        [Category("Unit")]
        [Description("测试AsakiResponse.Failure带消息创建失败响应")]
        public void AsakiResponse_FailureWithMessage_CreatesResponseWithMessage()
        {
            // Act
            var response = AsakiResponse.Failure(AsakiResponseCode.ServerError, "服务器错误");

            // Assert
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(AsakiResponseCode.ServerError, response.Code);
            Assert.AreEqual("服务器错误", response.Message);
        }

        [Test]
        [Category("Unit")]
        [Description("测试AsakiResponse<T>.Success创建成功响应")]
        public void AsakiResponseGeneric_Success_CreatesSuccessResponse()
        {
            // Arrange
            var data = new LoginResponseData { Token = "abc123", ExpiresAt = 1234567890 };

            // Act
            var response = AsakiResponse<LoginResponseData>.Success(data);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess);
            Assert.AreSame(data, response.Data);
        }

        [Test]
        [Category("Unit")]
        [Description("测试AsakiResponse<T>.Success带消息创建成功响应")]
        public void AsakiResponseGeneric_SuccessWithMessage_CreatesResponseWithMessage()
        {
            // Arrange
            var data = new LoginResponseData { Token = "abc123" };

            // Act
            var response = AsakiResponse<LoginResponseData>.Success(data, "登录成功");

            // Assert
            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual("登录成功", response.Message);
            Assert.AreEqual("abc123", response.Data.Token);
        }

        [Test]
        [Category("Unit")]
        [Description("测试AsakiResponse<T>.Failure创建失败响应")]
        public void AsakiResponseGeneric_Failure_CreatesFailureResponse()
        {
            // Act
            var response = AsakiResponse<LoginResponseData>.Failure(AsakiResponseCode.Unauthorized);

            // Assert
            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(AsakiResponseCode.Unauthorized, response.Code);
            Assert.IsNotNull(response.Data); // 失败时Data也被初始化
        }

        [Test]
        [Category("Unit")]
        [Description("测试AsakiResponse<T>.Failure带消息创建失败响应")]
        public void AsakiResponseGeneric_FailureWithMessage_CreatesResponseWithMessage()
        {
            // Act
            var response = AsakiResponse<LoginResponseData>.Failure(
                AsakiResponseCode.InvalidToken,
                "Token无效"
            );

            // Assert
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(AsakiResponseCode.InvalidToken, response.Code);
            Assert.AreEqual("Token无效", response.Message);
        }

        #endregion

        #region 接口实现测试

        [Test]
        [Category("Unit")]
        [Description("测试AsakiRequest实现IAsakiRequest")]
        public void AsakiRequest_ImplementsIAsakiRequest()
        {
            // Arrange
            var request = AsakiRequest.Create();

            // Assert
            Assert.IsInstanceOf<IAsakiRequest>(request);
        }

        [Test]
        [Category("Unit")]
        [Description("测试AsakiResponse实现IAsakiResponse")]
        public void AsakiResponse_ImplementsIAsakiResponse()
        {
            // Arrange
            var response = AsakiResponse.Success();

            // Assert
            Assert.IsInstanceOf<IAsakiResponse>(response);
        }

        [Test]
        [Category("Unit")]
        [Description("测试AsakiResponse<T>实现IAsakiResponse<T>")]
        public void AsakiResponseGeneric_ImplementsIAsakiResponseGeneric()
        {
            // Arrange
            var response = AsakiResponse<LoginResponseData>.Success(new LoginResponseData());

            // Assert
            Assert.IsInstanceOf<IAsakiResponse<LoginResponseData>>(response);
            Assert.IsInstanceOf<IAsakiResponse>(response);
        }

        #endregion

        #region 继承关系测试

        [Test]
        [Category("Unit")]
        [Description("测试AsakiRequest继承AsakiRequestBase")]
        public void AsakiRequest_InheritsFromBase()
        {
            // Arrange
            var request = AsakiRequest.Create();

            // Assert
            Assert.IsInstanceOf<AsakiRequestBase>(request);
        }

        [Test]
        [Category("Unit")]
        [Description("测试AsakiResponse继承AsakiResponseBase")]
        public void AsakiResponse_InheritsFromBase()
        {
            // Arrange
            var response = AsakiResponse.Success();

            // Assert
            Assert.IsInstanceOf<AsakiResponseBase>(response);
        }

        [Test]
        [Category("Unit")]
        [Description("测试AsakiRequest<T>继承AsakiRequestBase<T>")]
        public void AsakiRequestGeneric_InheritsFromBase()
        {
            // Arrange
            var request = AsakiRequest<LoginRequestData>.Create();

            // Assert
            Assert.IsInstanceOf<AsakiRequestBase<LoginRequestData>>(request);
            Assert.IsInstanceOf<AsakiRequestBase>(request);
        }

        [Test]
        [Category("Unit")]
        [Description("测试AsakiResponse<T>继承AsakiResponseBase<T>")]
        public void AsakiResponseGeneric_InheritsFromBase()
        {
            // Arrange
            var response = AsakiResponse<LoginResponseData>.Success(new LoginResponseData());

            // Assert
            Assert.IsInstanceOf<AsakiResponseBase<LoginResponseData>>(response);
            Assert.IsInstanceOf<AsakiResponseBase>(response);
        }

        #endregion
    }
}
