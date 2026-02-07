using Asaki.Core.Network;
using NUnit.Framework;

namespace Asaki.Tests.Network
{
    /// <summary>
    /// AsakiRequestValidationResult 请求验证结果单元测试
    /// </summary>
    [TestFixture]
    public class AsakiRequestValidationResultTests
    {
        #region 成功验证测试

        [Test]
        [Category("Unit")]
        [Description("测试Success静态属性创建有效验证结果")]
        public void Success_CreatesValidResult()
        {
            // Act
            var result = AsakiRequestValidationResult.Success;

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsNull(result.ErrorMessage);
        }

        [Test]
        [Category("Unit")]
        [Description("测试构造函数创建成功验证结果")]
        public void Constructor_WithTrueIsValid_CreatesSuccessResult()
        {
            // Act
            var result = new AsakiRequestValidationResult(true);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsNull(result.ErrorMessage);
        }

        #endregion

        #region 失败验证测试

        [Test]
        [Category("Unit")]
        [Description("测试Failure静态方法创建无效验证结果")]
        public void Failure_CreatesInvalidResult()
        {
            // Arrange
            string errorMessage = "参数不能为空";

            // Act
            var result = AsakiRequestValidationResult.Failure(errorMessage);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(errorMessage, result.ErrorMessage);
        }

        [Test]
        [Category("Unit")]
        [Description("测试Failure可以接受空消息")]
        public void Failure_WithNullMessage_CreatesInvalidResult()
        {
            // Act
            var result = AsakiRequestValidationResult.Failure(null);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsNull(result.ErrorMessage);
        }

        [Test]
        [Category("Unit")]
        [Description("测试Failure可以接受空字符串消息")]
        public void Failure_WithEmptyMessage_CreatesInvalidResult()
        {
            // Act
            var result = AsakiRequestValidationResult.Failure("");

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("", result.ErrorMessage);
        }

        [Test]
        [Category("Unit")]
        [Description("测试构造函数创建失败验证结果")]
        public void Constructor_WithFalseIsValid_CreatesFailureResult()
        {
            // Arrange
            string errorMessage = "验证失败";

            // Act
            var result = new AsakiRequestValidationResult(false, errorMessage);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(errorMessage, result.ErrorMessage);
        }

        #endregion

        #region 不可变性测试

        [Test]
        [Category("Unit")]
        [Description("测试验证结果是值类型且不可变")]
        public void Result_IsValueType()
        {
            // Arrange
            var result = AsakiRequestValidationResult.Success;

            // Assert
            Assert.IsTrue(typeof(AsakiRequestValidationResult).IsValueType);
        }

        #endregion

        #region 相等性测试

        [Test]
        [Category("Unit")]
        [Description("测试两个成功验证结果相等")]
        public void Equality_TwoSuccessResults_AreEqual()
        {
            // Arrange
            var result1 = AsakiRequestValidationResult.Success;
            var result2 = AsakiRequestValidationResult.Success;

            // Assert
            Assert.AreEqual(result1.IsValid, result2.IsValid);
        }

        [Test]
        [Category("Unit")]
        [Description("测试相同错误消息的两个失败结果")]
        public void Equality_TwoFailureResultsWithSameMessage_AreEqual()
        {
            // Arrange
            var result1 = AsakiRequestValidationResult.Failure("相同错误");
            var result2 = AsakiRequestValidationResult.Failure("相同错误");

            // Assert
            Assert.AreEqual(result1.IsValid, result2.IsValid);
            Assert.AreEqual(result1.ErrorMessage, result2.ErrorMessage);
        }

        #endregion
    }
}
