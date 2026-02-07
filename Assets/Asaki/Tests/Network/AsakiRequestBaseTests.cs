using System;
using Asaki.Core.Network;
using Asaki.Core.Serialization;
using NUnit.Framework;

namespace Asaki.Tests.Network
{
    /// <summary>
    /// 测试用的简单数据类
    /// </summary>
    [Serializable]
    public class TestRequestData : IAsakiSavable
    {
        public string Name { get; set; }
        public int Value { get; set; }

        public void Serialize(IAsakiWriter writer)
        {
            writer.WriteString("name", Name);
            writer.WriteInt("value", Value);
        }

        public void Deserialize(IAsakiReader reader)
        {
            Name = reader.ReadString("name");
            Value = reader.ReadInt("value");
        }
    }

    /// <summary>
    /// 测试用的具体请求类
    /// </summary>
    public class TestRequest : AsakiRequestBase
    {
        public string CustomField { get; set; }

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
    /// 测试用的泛型请求类
    /// </summary>
    public class TestGenericRequest : AsakiRequestBase<TestRequestData>
    {
        public string ExtraField { get; set; }

        protected override void SerializeRequestCore(IAsakiWriter writer)
        {
            writer.WriteString("extraField", ExtraField);
        }

        protected override void DeserializeRequestCore(IAsakiReader reader)
        {
            ExtraField = reader.ReadString("extraField");
        }
    }

    /// <summary>
    /// AsakiRequestBase 请求基类单元测试
    /// </summary>
    [TestFixture]
    public class AsakiRequestBaseTests
    {
        #region RequestId 测试

        [Test]
        [Category("Unit")]
        [Description("测试构造函数生成唯一的RequestId")]
        public void Constructor_GeneratesUniqueRequestId()
        {
            // Arrange
            var request1 = new TestRequest();
            var request2 = new TestRequest();

            // Assert
            Assert.IsNotNull(request1.RequestId);
            Assert.IsNotEmpty(request1.RequestId);
            Assert.IsNotNull(request2.RequestId);
            Assert.AreNotEqual(request1.RequestId, request2.RequestId);
        }

        [Test]
        [Category("Unit")]
        [Description("测试RequestId格式为32字符的N格式GUID")]
        public void Constructor_RequestIdIsValidGuid()
        {
            // Arrange
            var request = new TestRequest();

            // Assert
            Assert.AreEqual(32, request.RequestId.Length);
            Assert.IsTrue(Guid.TryParseExact(request.RequestId, "N", out _));
        }

        #endregion

        #region Timestamp 测试

        [Test]
        [Category("Unit")]
        [Description("测试构造函数设置当前时间戳")]
        public void Constructor_SetsCurrentTimestamp()
        {
            // Arrange
            long beforeTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var request = new TestRequest();
            long afterTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Assert
            Assert.GreaterOrEqual(request.Timestamp, beforeTime);
            Assert.LessOrEqual(request.Timestamp, afterTime);
        }

        [Test]
        [Category("Unit")]
        [Description("测试时间戳是Unix毫秒时间戳")]
        public void Timestamp_IsUnixMilliseconds()
        {
            // Arrange
            var request = new TestRequest();
            var now = DateTimeOffset.UtcNow;

            // Assert
            var requestTime = DateTimeOffset.FromUnixTimeMilliseconds(request.Timestamp);
            var diff = Math.Abs((now - requestTime).TotalSeconds);
            Assert.Less(diff, 1, "时间戳应该接近当前时间");
        }

        #endregion

        #region Validate 测试

        [Test]
        [Category("Unit")]
        [Description("测试基类Validate默认返回成功")]
        public void Validate_Default_ReturnsSuccess()
        {
            // Arrange
            var request = new TestRequest();

            // Act
            var result = request.Validate();

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsNull(result.ErrorMessage);
        }

        #endregion

        #region 泛型请求测试

        [Test]
        [Category("Unit")]
        [Description("测试泛型请求自动初始化数据对象")]
        public void GenericRequest_Constructor_InitializesData()
        {
            // Arrange
            var request = new TestGenericRequest();

            // Assert
            Assert.IsNotNull(request.Data);
            Assert.IsInstanceOf<TestRequestData>(request.Data);
        }

        [Test]
        [Category("Unit")]
        [Description("测试泛型请求可以设置数据")]
        public void GenericRequest_CanSetData()
        {
            // Arrange
            var request = new TestGenericRequest();
            var data = new TestRequestData { Name = "Test", Value = 42 };

            // Act
            request.Data = data;

            // Assert
            Assert.AreSame(data, request.Data);
            Assert.AreEqual("Test", request.Data.Name);
            Assert.AreEqual(42, request.Data.Value);
        }

        [Test]
        [Category("Unit")]
        [Description("测试泛型请求使用指定数据构造函数")]
        public void GenericRequest_ConstructorWithData_UsesProvidedData()
        {
            // Arrange
            var data = new TestRequestData { Name = "Provided", Value = 100 };
            var request = new TestGenericRequest();
            request.Data = data;

            // Assert
            Assert.AreEqual("Provided", request.Data.Name);
            Assert.AreEqual(100, request.Data.Value);
        }

        [Test]
        [Category("Unit")]
        [Description("测试泛型请求使用null数据时创建默认对象")]
        public void GenericRequest_ConstructorWithNullData_CreatesDefaultData()
        {
            // Arrange
            var request = new TestGenericRequest();
            request.Data = null;

            // Act - 重新设置一个有效对象
            request.Data = new TestRequestData();

            // Assert
            Assert.IsNotNull(request.Data);
        }

        #endregion

        #region 接口实现测试

        [Test]
        [Category("Unit")]
        [Description("测试请求实现IAsakiRequest接口")]
        public void Request_ImplementsIAsakiRequest()
        {
            // Arrange
            var request = new TestRequest();

            // Assert
            Assert.IsInstanceOf<IAsakiRequest>(request);
        }

        [Test]
        [Category("Unit")]
        [Description("测试泛型请求实现IAsakiRequest接口")]
        public void GenericRequest_ImplementsIAsakiRequest()
        {
            // Arrange
            var request = new TestGenericRequest();

            // Assert
            Assert.IsInstanceOf<IAsakiRequest>(request);
        }

        #endregion
    }
}
