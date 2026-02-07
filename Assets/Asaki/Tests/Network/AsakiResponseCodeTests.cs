using Asaki.Core.Network;
using NUnit.Framework;

namespace Asaki.Tests.Network
{
    /// <summary>
    /// AsakiResponseCode 响应状态码单元测试
    /// </summary>
    [TestFixture]
    public class AsakiResponseCodeTests
    {
        #region 成功状态码测试

        [Test]
        [Category("Unit")]
        [Description("测试成功状态码为0")]
        public void Success_CodeIsZero()
        {
            // Assert
            Assert.AreEqual(0, AsakiResponseCode.Success);
        }

        [Test]
        [Category("Unit")]
        [Description("测试IsSuccess对成功状态码返回true")]
        public void IsSuccess_WithSuccessCode_ReturnsTrue()
        {
            // Act & Assert
            Assert.IsTrue(AsakiResponseCode.IsSuccess(AsakiResponseCode.Success));
        }

        [Test]
        [Category("Unit")]
        [Description("测试IsSuccess对非0状态码返回false")]
        public void IsSuccess_WithNonZeroCode_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(AsakiResponseCode.IsSuccess(AsakiResponseCode.GeneralError));
            Assert.IsFalse(AsakiResponseCode.IsSuccess(AsakiResponseCode.InvalidParameter));
            Assert.IsFalse(AsakiResponseCode.IsSuccess(AsakiResponseCode.ServerError));
            Assert.IsFalse(AsakiResponseCode.IsSuccess(-1));
        }

        #endregion

        #region 错误类型分类测试

        [Test]
        [Category("Unit")]
        [Description("测试IsClientError正确识别客户端错误(1xxx)")]
        public void IsClientError_With1xxxCodes_ReturnsTrue()
        {
            // Act & Assert
            Assert.IsTrue(AsakiResponseCode.IsClientError(AsakiResponseCode.InvalidParameter));
            Assert.IsTrue(AsakiResponseCode.IsClientError(AsakiResponseCode.MissingParameter));
            Assert.IsTrue(AsakiResponseCode.IsClientError(AsakiResponseCode.InvalidParameterFormat));
            Assert.IsTrue(AsakiResponseCode.IsClientError(1000));
            Assert.IsTrue(AsakiResponseCode.IsClientError(1999));
        }

        [Test]
        [Category("Unit")]
        [Description("测试IsClientError对非1xxx返回false")]
        public void IsClientError_WithNon1xxxCodes_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(AsakiResponseCode.IsClientError(AsakiResponseCode.Success));
            Assert.IsFalse(AsakiResponseCode.IsClientError(AsakiResponseCode.Unauthorized));
            Assert.IsFalse(AsakiResponseCode.IsClientError(AsakiResponseCode.NetworkError));
            Assert.IsFalse(AsakiResponseCode.IsClientError(999));
            Assert.IsFalse(AsakiResponseCode.IsClientError(2000));
        }

        [Test]
        [Category("Unit")]
        [Description("测试IsAuthError正确识别授权错误(2xxx)")]
        public void IsAuthError_With2xxxCodes_ReturnsTrue()
        {
            // Act & Assert
            Assert.IsTrue(AsakiResponseCode.IsAuthError(AsakiResponseCode.Unauthorized));
            Assert.IsTrue(AsakiResponseCode.IsAuthError(AsakiResponseCode.TokenExpired));
            Assert.IsTrue(AsakiResponseCode.IsAuthError(AsakiResponseCode.InvalidToken));
            Assert.IsTrue(AsakiResponseCode.IsAuthError(AsakiResponseCode.InsufficientPermission));
            Assert.IsTrue(AsakiResponseCode.IsAuthError(2000));
            Assert.IsTrue(AsakiResponseCode.IsAuthError(2999));
        }

        [Test]
        [Category("Unit")]
        [Description("测试IsAuthError对非2xxx返回false")]
        public void IsAuthError_WithNon2xxxCodes_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(AsakiResponseCode.IsAuthError(AsakiResponseCode.Success));
            Assert.IsFalse(AsakiResponseCode.IsAuthError(AsakiResponseCode.InvalidParameter));
            Assert.IsFalse(AsakiResponseCode.IsAuthError(AsakiResponseCode.ResourceNotFound));
            Assert.IsFalse(AsakiResponseCode.IsAuthError(1999));
            Assert.IsFalse(AsakiResponseCode.IsAuthError(3000));
        }

        [Test]
        [Category("Unit")]
        [Description("测试IsResourceError正确识别资源错误(3xxx)")]
        public void IsResourceError_With3xxxCodes_ReturnsTrue()
        {
            // Act & Assert
            Assert.IsTrue(AsakiResponseCode.IsResourceError(AsakiResponseCode.ResourceNotFound));
            Assert.IsTrue(AsakiResponseCode.IsResourceError(AsakiResponseCode.ResourceAlreadyExists));
            Assert.IsTrue(AsakiResponseCode.IsResourceError(AsakiResponseCode.ResourceBusy));
            Assert.IsTrue(AsakiResponseCode.IsResourceError(3000));
            Assert.IsTrue(AsakiResponseCode.IsResourceError(3999));
        }

        [Test]
        [Category("Unit")]
        [Description("测试IsResourceError对非3xxx返回false")]
        public void IsResourceError_WithNon3xxxCodes_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(AsakiResponseCode.IsResourceError(AsakiResponseCode.Success));
            Assert.IsFalse(AsakiResponseCode.IsResourceError(AsakiResponseCode.Unauthorized));
            Assert.IsFalse(AsakiResponseCode.IsResourceError(AsakiResponseCode.NetworkError));
            Assert.IsFalse(AsakiResponseCode.IsResourceError(2999));
            Assert.IsFalse(AsakiResponseCode.IsResourceError(4000));
        }

        [Test]
        [Category("Unit")]
        [Description("测试IsNetworkError正确识别网络错误(4xxx)")]
        public void IsNetworkError_With4xxxCodes_ReturnsTrue()
        {
            // Act & Assert
            Assert.IsTrue(AsakiResponseCode.IsNetworkError(AsakiResponseCode.NetworkError));
            Assert.IsTrue(AsakiResponseCode.IsNetworkError(AsakiResponseCode.RequestTimeout));
            Assert.IsTrue(AsakiResponseCode.IsNetworkError(4000));
            Assert.IsTrue(AsakiResponseCode.IsNetworkError(4999));
        }

        [Test]
        [Category("Unit")]
        [Description("测试IsNetworkError对非4xxx返回false")]
        public void IsNetworkError_WithNon4xxxCodes_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(AsakiResponseCode.IsNetworkError(AsakiResponseCode.Success));
            Assert.IsFalse(AsakiResponseCode.IsNetworkError(AsakiResponseCode.ResourceNotFound));
            Assert.IsFalse(AsakiResponseCode.IsNetworkError(AsakiResponseCode.ServerError));
            Assert.IsFalse(AsakiResponseCode.IsNetworkError(3999));
            Assert.IsFalse(AsakiResponseCode.IsNetworkError(5000));
        }

        [Test]
        [Category("Unit")]
        [Description("测试IsServerError正确识别服务器错误(5xxx)")]
        public void IsServerError_With5xxxCodes_ReturnsTrue()
        {
            // Act & Assert
            Assert.IsTrue(AsakiResponseCode.IsServerError(AsakiResponseCode.ServerError));
            Assert.IsTrue(AsakiResponseCode.IsServerError(AsakiResponseCode.ServiceUnavailable));
            Assert.IsTrue(AsakiResponseCode.IsServerError(AsakiResponseCode.ServerMaintenance));
            Assert.IsTrue(AsakiResponseCode.IsServerError(5000));
            Assert.IsTrue(AsakiResponseCode.IsServerError(5999));
        }

        [Test]
        [Category("Unit")]
        [Description("测试IsServerError对非5xxx返回false")]
        public void IsServerError_WithNon5xxxCodes_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(AsakiResponseCode.IsServerError(AsakiResponseCode.Success));
            Assert.IsFalse(AsakiResponseCode.IsServerError(AsakiResponseCode.NetworkError));
            Assert.IsFalse(AsakiResponseCode.IsServerError(4999));
            Assert.IsFalse(AsakiResponseCode.IsServerError(6000));
        }

        #endregion

        #region 默认消息测试

        [Test]
        [Category("Unit")]
        [Description("测试GetDefaultMessage返回正确的中文消息")]
        public void GetDefaultMessage_WithKnownCodes_ReturnsCorrectMessages()
        {
            // Act & Assert
            Assert.AreEqual("操作成功", AsakiResponseCode.GetDefaultMessage(AsakiResponseCode.Success));
            Assert.AreEqual("操作失败", AsakiResponseCode.GetDefaultMessage(AsakiResponseCode.GeneralError));
            Assert.AreEqual("参数错误", AsakiResponseCode.GetDefaultMessage(AsakiResponseCode.InvalidParameter));
            Assert.AreEqual("缺少必要参数", AsakiResponseCode.GetDefaultMessage(AsakiResponseCode.MissingParameter));
            Assert.AreEqual("未授权", AsakiResponseCode.GetDefaultMessage(AsakiResponseCode.Unauthorized));
            Assert.AreEqual("登录已过期，请重新登录", AsakiResponseCode.GetDefaultMessage(AsakiResponseCode.TokenExpired));
            Assert.AreEqual("请求的资源不存在", AsakiResponseCode.GetDefaultMessage(AsakiResponseCode.ResourceNotFound));
            Assert.AreEqual("网络连接失败", AsakiResponseCode.GetDefaultMessage(AsakiResponseCode.NetworkError));
            Assert.AreEqual("请求超时，请稍后重试", AsakiResponseCode.GetDefaultMessage(AsakiResponseCode.RequestTimeout));
            Assert.AreEqual("服务器内部错误", AsakiResponseCode.GetDefaultMessage(AsakiResponseCode.ServerError));
        }

        [Test]
        [Category("Unit")]
        [Description("测试GetDefaultMessage对未知状态码返回默认消息")]
        public void GetDefaultMessage_WithUnknownCode_ReturnsDefaultMessage()
        {
            // Act & Assert
            Assert.AreEqual("未知错误", AsakiResponseCode.GetDefaultMessage(9999));
            Assert.AreEqual("未知错误", AsakiResponseCode.GetDefaultMessage(-1));
            Assert.AreEqual("未知错误", AsakiResponseCode.GetDefaultMessage(12345));
        }

        #endregion

        #region 边界值测试

        [Test]
        [Category("Unit")]
        [Description("测试边界值1000被识别为客户端错误")]
        public void IsClientError_BoundaryValue1000_ReturnsTrue()
        {
            Assert.IsTrue(AsakiResponseCode.IsClientError(1000));
        }

        [Test]
        [Category("Unit")]
        [Description("测试边界值1999被识别为客户端错误")]
        public void IsClientError_BoundaryValue1999_ReturnsTrue()
        {
            Assert.IsTrue(AsakiResponseCode.IsClientError(1999));
        }

        [Test]
        [Category("Unit")]
        [Description("测试边界值2000被识别为授权错误")]
        public void IsAuthError_BoundaryValue2000_ReturnsTrue()
        {
            Assert.IsTrue(AsakiResponseCode.IsAuthError(2000));
        }

        [Test]
        [Category("Unit")]
        [Description("测试边界值2999被识别为授权错误")]
        public void IsAuthError_BoundaryValue2999_ReturnsTrue()
        {
            Assert.IsTrue(AsakiResponseCode.IsAuthError(2999));
        }

        [Test]
        [Category("Unit")]
        [Description("测试边界值3000被识别为资源错误")]
        public void IsResourceError_BoundaryValue3000_ReturnsTrue()
        {
            Assert.IsTrue(AsakiResponseCode.IsResourceError(3000));
        }

        [Test]
        [Category("Unit")]
        [Description("测试边界值3999被识别为资源错误")]
        public void IsResourceError_BoundaryValue3999_ReturnsTrue()
        {
            Assert.IsTrue(AsakiResponseCode.IsResourceError(3999));
        }

        [Test]
        [Category("Unit")]
        [Description("测试边界值4000被识别为网络错误")]
        public void IsNetworkError_BoundaryValue4000_ReturnsTrue()
        {
            Assert.IsTrue(AsakiResponseCode.IsNetworkError(4000));
        }

        [Test]
        [Category("Unit")]
        [Description("测试边界值4999被识别为网络错误")]
        public void IsNetworkError_BoundaryValue4999_ReturnsTrue()
        {
            Assert.IsTrue(AsakiResponseCode.IsNetworkError(4999));
        }

        [Test]
        [Category("Unit")]
        [Description("测试边界值5000被识别为服务器错误")]
        public void IsServerError_BoundaryValue5000_ReturnsTrue()
        {
            Assert.IsTrue(AsakiResponseCode.IsServerError(5000));
        }

        [Test]
        [Category("Unit")]
        [Description("测试边界值5999被识别为服务器错误")]
        public void IsServerError_BoundaryValue5999_ReturnsTrue()
        {
            Assert.IsTrue(AsakiResponseCode.IsServerError(5999));
        }

        #endregion
    }
}
