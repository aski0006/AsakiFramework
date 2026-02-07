using Asaki.Core.Network;
using NUnit.Framework;

namespace Asaki.Tests.Network
{
    /// <summary>
    /// AsakiWebException 网络异常单元测试
    /// </summary>
    [TestFixture]
    public class AsakiWebExceptionTests
    {
        #region 构造函数测试

        [Test]
        [Category("Unit")]
        [Description("测试构造函数正确设置所有属性")]
        public void Constructor_WithValidParameters_SetsProperties()
        {
            // Arrange
            string message = "连接超时";
            long code = 404;
            string url = "https://api.example.com/test";

            // Act
            var exception = new AsakiWebException(message, code, url);

            // Assert
            Assert.AreEqual(message, exception.Message);
            Assert.AreEqual(code, exception.ResponseCode);
            Assert.AreEqual(url, exception.Url);
        }

        [Test]
        [Category("Unit")]
        [Description("测试构造函数使用空消息")]
        public void Constructor_WithNullMessage_SetsEmptyMessage()
        {
            // Arrange
            long code = 500;
            string url = "https://api.example.com/test";

            // Act
            var exception = new AsakiWebException(null, code, url);

            // Assert
            Assert.AreEqual(string.Empty, exception.Message);
            Assert.AreEqual(code, exception.ResponseCode);
            Assert.AreEqual(url, exception.Url);
        }

        [Test]
        [Category("Unit")]
        [Description("测试构造函数使用空URL")]
        public void Constructor_WithNullUrl_SetsNullUrl()
        {
            // Arrange
            string message = "网络错误";
            long code = 0;

            // Act
            var exception = new AsakiWebException(message, code, null);

            // Assert
            Assert.AreEqual(message, exception.Message);
            Assert.AreEqual(code, exception.ResponseCode);
            Assert.IsNull(exception.Url);
        }

        [Test]
        [Category("Unit")]
        [Description("测试构造函数使用负数状态码")]
        public void Constructor_WithNegativeCode_SetsNegativeCode()
        {
            // Arrange
            string message = "未知错误";
            long code = -1;
            string url = "https://api.example.com/test";

            // Act
            var exception = new AsakiWebException(message, code, url);

            // Assert
            Assert.AreEqual(code, exception.ResponseCode);
        }

        [Test]
        [Category("Unit")]
        [Description("测试构造函数使用0状态码")]
        public void Constructor_WithZeroCode_SetsZeroCode()
        {
            // Arrange
            string message = "成功";
            long code = 0;
            string url = "https://api.example.com/test";

            // Act
            var exception = new AsakiWebException(message, code, url);

            // Assert
            Assert.AreEqual(0, exception.ResponseCode);
        }

        #endregion

        #region 常见HTTP状态码测试

        [Test]
        [Category("Unit")]
        [Description("测试400 Bad Request异常")]
        public void Constructor_WithBadRequestCode_CreatesCorrectException()
        {
            // Act
            var exception = new AsakiWebException(
                "请求参数错误",
                400,
                "https://api.example.com/users"
            );

            // Assert
            Assert.AreEqual(400, exception.ResponseCode);
            Assert.AreEqual("请求参数错误", exception.Message);
        }

        [Test]
        [Category("Unit")]
        [Description("测试401 Unauthorized异常")]
        public void Constructor_WithUnauthorizedCode_CreatesCorrectException()
        {
            // Act
            var exception = new AsakiWebException(
                "未授权访问",
                401,
                "https://api.example.com/protected"
            );

            // Assert
            Assert.AreEqual(401, exception.ResponseCode);
        }

        [Test]
        [Category("Unit")]
        [Description("测试403 Forbidden异常")]
        public void Constructor_WithForbiddenCode_CreatesCorrectException()
        {
            // Act
            var exception = new AsakiWebException("禁止访问", 403, "https://api.example.com/admin");

            // Assert
            Assert.AreEqual(403, exception.ResponseCode);
        }

        [Test]
        [Category("Unit")]
        [Description("测试404 Not Found异常")]
        public void Constructor_WithNotFoundCode_CreatesCorrectException()
        {
            // Act
            var exception = new AsakiWebException(
                "资源不存在",
                404,
                "https://api.example.com/users/999"
            );

            // Assert
            Assert.AreEqual(404, exception.ResponseCode);
        }

        [Test]
        [Category("Unit")]
        [Description("测试500 Internal Server Error异常")]
        public void Constructor_WithInternalServerErrorCode_CreatesCorrectException()
        {
            // Act
            var exception = new AsakiWebException(
                "服务器内部错误",
                500,
                "https://api.example.com/data"
            );

            // Assert
            Assert.AreEqual(500, exception.ResponseCode);
        }

        [Test]
        [Category("Unit")]
        [Description("测试503 Service Unavailable异常")]
        public void Constructor_WithServiceUnavailableCode_CreatesCorrectException()
        {
            // Act
            var exception = new AsakiWebException(
                "服务不可用",
                503,
                "https://api.example.com/service"
            );

            // Assert
            Assert.AreEqual(503, exception.ResponseCode);
        }

        #endregion

        #region 继承测试

        [Test]
        [Category("Unit")]
        [Description("测试异常继承自System.Exception")]
        public void AsakiWebException_InheritsFromException()
        {
            // Arrange
            var exception = new AsakiWebException("测试", 200, "url");

            // Assert
            Assert.IsInstanceOf<System.Exception>(exception);
        }

        [Test]
        [Category("Unit")]
        [Description("测试异常可以被catch为System.Exception")]
        public void AsakiWebException_CanBeCaughtAsException()
        {
            // Arrange
            System.Exception caughtException = null;

            // Act
            try
            {
                throw new AsakiWebException("测试异常", 500, "https://test.com");
            }
            catch (System.Exception ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException);
            Assert.IsInstanceOf<AsakiWebException>(caughtException);
        }

        #endregion
    }
}
