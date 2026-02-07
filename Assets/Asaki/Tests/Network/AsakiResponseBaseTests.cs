using System;
using Asaki.Core.Network;
using Asaki.Core.Serialization;
using NUnit.Framework;

namespace Asaki.Tests.Network
{
    /// <summary>
    /// 测试用的简单响应数据类
    /// </summary>
    [Serializable]
    public class TestResponseData : IAsakiSavable
    {
        public string Result { get; set; }
        public int Count { get; set; }

        public void Serialize(IAsakiWriter writer)
        {
            writer.WriteString("result", Result);
            writer.WriteInt("count", Count);
        }

        public void Deserialize(IAsakiReader reader)
        {
            Result = reader.ReadString("result");
            Count = reader.ReadInt("count");
        }
    }

    /// <summary>
    /// 测试用的具体响应类
    /// </summary>
    public class TestResponse : AsakiResponseBase
    {
        public string CustomField { get; set; }

        public new void SetSuccess(string message = null)
        {
            base.SetSuccess(message);
        }

        public new void SetError(int code, string message = null)
        {
            base.SetError(code, message);
        }

        public new void SetResponse(int code, string message = null)
        {
            base.SetResponse(code, message);
        }

        protected override void SerializeCore(IAsakiWriter writer)
        {
            writer.WriteString("customField", CustomField);
        }

        protected override void DeserializeCore(IAsakiReader reader)
        {
            CustomField = reader.ReadString("customField");
        }
    }

    /// <summary>
    /// 测试用的泛型响应类
    /// </summary>
    public class TestGenericResponse : AsakiResponseBase<TestResponseData>
    {
        public new void SetSuccess(TestResponseData data, string message = null)
        {
            base.SetSuccess(data, message);
        }

        public new void SetError(int code, string message = null)
        {
            base.SetError(code, message);
        }

        public string ExtraField { get; set; }

        protected override void SerializeResponseCore(IAsakiWriter writer)
        {
            writer.WriteString("extraField", ExtraField);
        }

        protected override void DeserializeResponseCore(IAsakiReader reader)
        {
            ExtraField = reader.ReadString("extraField");
        }
    }

    /// <summary>
    /// AsakiResponseBase 响应基类单元测试
    /// </summary>
    [TestFixture]
    public class AsakiResponseBaseTests
    {
        #region 默认构造函数测试

        [Test]
        [Category("Unit")]
        [Description("测试默认构造函数设置成功状态码")]
        public void DefaultConstructor_SetsSuccessCode()
        {
            // Arrange
            var response = new TestResponse();

            // Assert
            Assert.AreEqual(AsakiResponseCode.Success, response.Code);
            Assert.IsTrue(response.IsSuccess);
        }

        [Test]
        [Category("Unit")]
        [Description("测试默认构造函数设置空消息")]
        public void DefaultConstructor_SetsEmptyMessage()
        {
            // Arrange
            var response = new TestResponse();

            // Assert
            Assert.AreEqual(string.Empty, response.Message);
        }

        [Test]
        [Category("Unit")]
        [Description("测试默认构造函数RequestId为空")]
        public void DefaultConstructor_SetsEmptyRequestId()
        {
            // Arrange
            var response = new TestResponse();

            // Assert
            Assert.IsNull(response.RequestId);
        }

        #endregion

        #region 参数化构造函数测试

        [Test]
        [Category("Unit")]
        [Description("测试参数化构造函数设置指定状态码")]
        public void ParameterizedConstructor_SetsSpecifiedCode()
        {
            // Arrange
            var response = new TestResponse();
            response.SetError(AsakiResponseCode.InvalidParameter);

            // Assert
            Assert.AreEqual(AsakiResponseCode.InvalidParameter, response.Code);
            Assert.IsFalse(response.IsSuccess);
        }

        [Test]
        [Category("Unit")]
        [Description("测试参数化构造函数使用自定义消息")]
        public void SetResponse_WithCustomMessage_SetsMessage()
        {
            // Arrange
            var response = new TestResponse();
            string customMessage = "自定义错误消息";

            // Act
            response.SetError(AsakiResponseCode.GeneralError, customMessage);

            // Assert
            Assert.AreEqual(customMessage, response.Message);
        }

        [Test]
        [Category("Unit")]
        [Description("测试使用null消息时获取默认消息")]
        public void SetResponse_WithNullMessage_UsesDefaultMessage()
        {
            // Arrange
            var response = new TestResponse();

            // Act
            response.SetError(AsakiResponseCode.InvalidParameter, null);

            // Assert
            Assert.AreEqual("参数错误", response.Message);
        }

        #endregion

        #region IsSuccess 测试

        [Test]
        [Category("Unit")]
        [Description("测试IsSuccess对0状态码返回true")]
        public void IsSuccess_WithZeroCode_ReturnsTrue()
        {
            // Arrange
            var response = new TestResponse();
            response.SetSuccess();

            // Assert
            Assert.IsTrue(response.IsSuccess);
        }

        [Test]
        [Category("Unit")]
        [Description("测试IsSuccess对非0状态码返回false")]
        public void IsSuccess_WithNonZeroCode_ReturnsFalse()
        {
            // Arrange
            var response = new TestResponse();
            response.SetError(AsakiResponseCode.GeneralError);

            // Assert
            Assert.IsFalse(response.IsSuccess);
        }

        [Test]
        [Category("Unit")]
        [Description("测试IsSuccess对负数状态码返回false")]
        public void IsSuccess_WithNegativeCode_ReturnsFalse()
        {
            // Arrange
            var response = new TestResponse();
            response.SetResponse(-1);

            // Assert
            Assert.IsFalse(response.IsSuccess);
        }

        #endregion

        #region SetSuccess 测试

        [Test]
        [Category("Unit")]
        [Description("测试SetSuccess设置成功状态")]
        public void SetSuccess_SetsSuccessState()
        {
            // Arrange
            var response = new TestResponse();
            response.SetError(AsakiResponseCode.GeneralError);

            // Act
            response.SetSuccess();

            // Assert
            Assert.AreEqual(AsakiResponseCode.Success, response.Code);
            Assert.IsTrue(response.IsSuccess);
        }

        [Test]
        [Category("Unit")]
        [Description("测试SetSuccess可以设置自定义成功消息")]
        public void SetSuccess_WithMessage_SetsMessage()
        {
            // Arrange
            var response = new TestResponse();
            string successMessage = "操作成功完成";

            // Act
            response.SetSuccess(successMessage);

            // Assert
            Assert.AreEqual(successMessage, response.Message);
        }

        #endregion

        #region SetError 测试

        [Test]
        [Category("Unit")]
        [Description("测试SetError设置错误状态")]
        public void SetError_SetsErrorState()
        {
            // Arrange
            var response = new TestResponse();

            // Act
            response.SetError(AsakiResponseCode.ServerError);

            // Assert
            Assert.AreEqual(AsakiResponseCode.ServerError, response.Code);
            Assert.IsFalse(response.IsSuccess);
        }

        [Test]
        [Category("Unit")]
        [Description("测试SetError可以设置自定义错误消息")]
        public void SetError_WithMessage_SetsMessage()
        {
            // Arrange
            var response = new TestResponse();
            string errorMessage = "数据库连接失败";

            // Act
            response.SetError(AsakiResponseCode.ServerError, errorMessage);

            // Assert
            Assert.AreEqual(errorMessage, response.Message);
        }

        #endregion

        #region 泛型响应测试

        [Test]
        [Category("Unit")]
        [Description("测试泛型响应自动初始化数据对象")]
        public void GenericResponse_Constructor_InitializesData()
        {
            // Arrange
            var response = new TestGenericResponse();

            // Assert
            Assert.IsNotNull(response.Data);
            Assert.IsInstanceOf<TestResponseData>(response.Data);
        }

        [Test]
        [Category("Unit")]
        [Description("测试泛型响应SetSuccess设置数据")]
        public void GenericResponse_SetSuccess_SetsData()
        {
            // Arrange
            var response = new TestGenericResponse();
            var data = new TestResponseData { Result = "OK", Count = 10 };

            // Act
            response.SetSuccess(data);

            // Assert
            Assert.AreSame(data, response.Data);
            Assert.AreEqual("OK", response.Data.Result);
            Assert.AreEqual(10, response.Data.Count);
        }

        [Test]
        [Category("Unit")]
        [Description("测试泛型响应SetSuccess设置数据和消息")]
        public void GenericResponse_SetSuccessWithMessage_SetsDataAndMessage()
        {
            // Arrange
            var response = new TestGenericResponse();
            var data = new TestResponseData { Result = "OK" };
            string message = "查询成功";

            // Act
            response.SetSuccess(data, message);

            // Assert
            Assert.AreEqual(message, response.Message);
            Assert.AreSame(data, response.Data);
        }

        [Test]
        [Category("Unit")]
        [Description("测试泛型响应SetError时不修改数据")]
        public void GenericResponse_SetError_KeepsData()
        {
            // Arrange
            var response = new TestGenericResponse();
            var originalData = response.Data;

            // Act
            response.SetError(AsakiResponseCode.ResourceNotFound);

            // Assert
            Assert.IsNotNull(response.Data);
            Assert.AreSame(originalData, response.Data);
            Assert.IsFalse(response.IsSuccess);
        }

        #endregion

        #region 接口实现测试

        [Test]
        [Category("Unit")]
        [Description("测试响应实现IAsakiResponse接口")]
        public void Response_ImplementsIAsakiResponse()
        {
            // Arrange
            var response = new TestResponse();

            // Assert
            Assert.IsInstanceOf<IAsakiResponse>(response);
        }

        [Test]
        [Category("Unit")]
        [Description("测试泛型响应实现IAsakiResponse接口")]
        public void GenericResponse_ImplementsIAsakiResponse()
        {
            // Arrange
            var response = new TestGenericResponse();

            // Assert
            Assert.IsInstanceOf<IAsakiResponse>(response);
            Assert.IsInstanceOf<IAsakiResponse<TestResponseData>>(response);
        }

        #endregion
    }
}
