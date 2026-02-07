using System;
using System.Collections;
using Asaki.Core.Network;
using Asaki.Core.Serialization;
using Asaki.Unity.Services.Network;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Asaki.Tests.Network
{
    /// <summary>
    /// 测试用的响应实现
    /// </summary>
    public class TestApiResponse : AsakiResponseBase
    {
        public string CustomData { get; set; }

        protected override void SerializeCore(IAsakiWriter writer)
        {
            writer.WriteString("customData", CustomData);
        }

        protected override void DeserializeCore(IAsakiReader reader)
        {
            CustomData = reader.ReadString("customData");
        }

        /// <summary>
        /// 创建成功响应
        /// </summary>
        public static TestApiResponse Success(string message = null)
        {
            var response = new TestApiResponse();
            response.SetSuccess(message);
            return response;
        }

        /// <summary>
        /// 创建失败响应
        /// </summary>
        public static TestApiResponse Failure(int code, string message = null)
        {
            var response = new TestApiResponse();
            response.SetError(code, message);
            return response;
        }
    }

    /// <summary>
    /// AsakiApiResult API结果单元测试
    /// </summary>
    [TestFixture]
    public class AsakiApiResultTests
    {
        #region Ok 成功结果测试

        [Test]
        [Category("Unit")]
        public void Ok_WithSuccessResponse_CreatesSuccessResult()
        {
            // Arrange
            var response = TestApiResponse.Success();

            // Act
            var result = AsakiApiResult<TestApiResponse>.Ok(response);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.IsFalse(result.IsFailure);
            Assert.AreSame(response, result.Response);
            Assert.IsNull(result.Exception);
        }

        [Test]
        [Category("Unit")]
        public void Ok_SuccessResult_HasSuccessCode()
        {
            // Arrange
            var response = TestApiResponse.Success();

            // Act
            var result = AsakiApiResult<TestApiResponse>.Ok(response);

            // Assert
            Assert.AreEqual(AsakiResponseCode.Success, result.Code);
        }

        #endregion

        #region Error 失败结果测试

        [Test]
        [Category("Unit")]
        public void Error_WithException_CreatesErrorResult()
        {
            // Arrange
            var exception = new Exception("网络连接失败");

            // Act
            var result = AsakiApiResult<TestApiResponse>.Error(exception);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.IsFailure);
            Assert.AreSame(exception, result.Exception);
            Assert.AreEqual("网络连接失败", result.Message);
        }

        [Test]
        [Category("Unit")]
        public void Error_WithErrorResponse_CreatesErrorResult()
        {
            // Arrange
            var response = TestApiResponse.Failure(AsakiResponseCode.InvalidParameter, "参数错误");

            // Act
            var result = AsakiApiResult<TestApiResponse>.Error(response);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.IsFailure);
            Assert.AreSame(response, result.Response);
            Assert.AreEqual(AsakiResponseCode.InvalidParameter, result.Code);
            Assert.AreEqual("参数错误", result.Message);
        }

        [Test]
        [Category("Unit")]
        public void Error_WithNetworkException_HasNetworkErrorCode()
        {
            // Arrange
            var exception = new AsakiWebException("连接超时", 0, "https://test.com");

            // Act
            var result = AsakiApiResult<TestApiResponse>.Error(exception);

            // Assert
            Assert.AreEqual(AsakiResponseCode.NetworkError, result.Code);
        }

        #endregion

        #region Match 方法测试

        [Test]
        [Category("Unit")]
        public void Match_WithSuccessResult_CallsOnSuccess()
        {
            // Arrange
            var response = TestApiResponse.Success();
            var result = AsakiApiResult<TestApiResponse>.Ok(response);
            bool successCalled = false;
            bool failureCalled = false;

            // Act
            result.Match(
                onSuccess: r => successCalled = true,
                onFailure: (code, msg) => failureCalled = true
            );

            // Assert
            Assert.IsTrue(successCalled);
            Assert.IsFalse(failureCalled);
        }

        [Test]
        [Category("Unit")]
        public void Match_WithFailureResult_CallsOnFailure()
        {
            // Arrange
            var response = TestApiResponse.Failure(AsakiResponseCode.ServerError, "服务器错误");
            var result = AsakiApiResult<TestApiResponse>.Error(response);
            bool successCalled = false;
            bool failureCalled = false;
            int capturedCode = 0;
            string capturedMessage = null;

            // Act
            result.Match(
                onSuccess: r => successCalled = true,
                onFailure: (code, msg) =>
                {
                    failureCalled = true;
                    capturedCode = code;
                    capturedMessage = msg;
                }
            );

            // Assert
            Assert.IsFalse(successCalled);
            Assert.IsTrue(failureCalled);
            Assert.AreEqual(AsakiResponseCode.ServerError, capturedCode);
            Assert.AreEqual("服务器错误", capturedMessage);
        }

        [Test]
        [Category("Unit")]
        public void Match_WithNullCallbacks_DoesNotThrow()
        {
            // Arrange
            var successResult = AsakiApiResult<TestApiResponse>.Ok(TestApiResponse.Success());
            var failureResult = AsakiApiResult<TestApiResponse>.Error(new Exception("错误"));

            // Act & Assert
            Assert.DoesNotThrow(() => successResult.Match(null, null));
            Assert.DoesNotThrow(() => failureResult.Match(null, null));
        }

        [Test]
        [Category("Unit")]
        public void Match_WithReturnValue_SuccessReturnsCorrectValue()
        {
            // Arrange
            var response = TestApiResponse.Success();
            var result = AsakiApiResult<TestApiResponse>.Ok(response);

            // Act
            string value = result.Match(
                onSuccess: r => "success",
                onFailure: (code, msg) => "failure"
            );

            // Assert
            Assert.AreEqual("success", value);
        }

        [Test]
        [Category("Unit")]
        public void Match_WithReturnValue_FailureReturnsCorrectValue()
        {
            // Arrange
            var result = AsakiApiResult<TestApiResponse>.Error(new Exception("错误"));

            // Act
            string value = result.Match(
                onSuccess: r => "success",
                onFailure: (code, msg) => $"failure:{code}:{msg}"
            );

            // Assert
            Assert.AreEqual($"failure:{AsakiResponseCode.NetworkError}:错误", value);
        }

        #endregion

        #region OnSuccess 方法测试

        [Test]
        [Category("Unit")]
        public void OnSuccess_WithSuccessResult_ExecutesAction()
        {
            // Arrange
            var response = TestApiResponse.Success();
            var result = AsakiApiResult<TestApiResponse>.Ok(response);
            bool actionCalled = false;

            // Act
            result.OnSuccess(r => actionCalled = true);

            // Assert
            Assert.IsTrue(actionCalled);
        }

        [Test]
        [Category("Unit")]
        public void OnSuccess_WithFailureResult_DoesNotExecuteAction()
        {
            // Arrange
            var result = AsakiApiResult<TestApiResponse>.Error(new Exception("错误"));
            bool actionCalled = false;

            // Act
            result.OnSuccess(r => actionCalled = true);

            // Assert
            Assert.IsFalse(actionCalled);
        }

        [Test]
        [Category("Unit")]
        public void OnSuccess_ReturnsSelf_ForChaining()
        {
            // Arrange
            var result = AsakiApiResult<TestApiResponse>.Ok(TestApiResponse.Success());

            // Act
            var returned = result.OnSuccess(r => { });

            // Assert
            Assert.AreEqual(result, returned);
        }

        #endregion

        #region OnFailure 方法测试

        [Test]
        [Category("Unit")]
        public void OnFailure_WithFailureResult_ExecutesAction()
        {
            // Arrange
            var result = AsakiApiResult<TestApiResponse>.Error(new Exception("错误"));
            bool actionCalled = false;
            int capturedCode = 0;
            string capturedMessage = null;

            // Act
            result.OnFailure(
                (code, msg) =>
                {
                    actionCalled = true;
                    capturedCode = code;
                    capturedMessage = msg;
                }
            );

            // Assert
            Assert.IsTrue(actionCalled);
            Assert.AreEqual(AsakiResponseCode.NetworkError, capturedCode);
            Assert.AreEqual("错误", capturedMessage);
        }

        [Test]
        [Category("Unit")]
        public void OnFailure_WithSuccessResult_DoesNotExecuteAction()
        {
            // Arrange
            var result = AsakiApiResult<TestApiResponse>.Ok(TestApiResponse.Success());
            bool actionCalled = false;

            // Act
            result.OnFailure((code, msg) => actionCalled = true);

            // Assert
            Assert.IsFalse(actionCalled);
        }

        [Test]
        [Category("Unit")]
        public void OnFailure_ReturnsSelf_ForChaining()
        {
            // Arrange
            var result = AsakiApiResult<TestApiResponse>.Error(new Exception("错误"));

            // Act
            var returned = result.OnFailure((code, msg) => { });

            // Assert
            Assert.AreEqual(result, returned);
        }

        #endregion

        #region Map 方法测试

        [Test]
        [Category("Unit")]
        public void Map_WithSuccessResult_TransformsResponse()
        {
            // Arrange
            var sourceResponse = TestApiResponse.Success();
            var result = AsakiApiResult<TestApiResponse>.Ok(sourceResponse);

            // Act
            var mapped = result.Map(r =>
            {
                var newResponse = TestApiResponse.Success();
                newResponse.CustomData = "映射成功";
                return newResponse;
            });

            // Assert
            Assert.IsTrue(mapped.IsSuccess);
            Assert.AreEqual("映射成功", mapped.Response.CustomData);
        }

        [Test]
        [Category("Unit")]
        public void Map_WithFailureResult_KeepsException()
        {
            // Arrange
            var exception = new Exception("原始错误");
            var result = AsakiApiResult<TestApiResponse>.Error(exception);

            // Act
            var mapped = result.Map(r => TestApiResponse.Success());

            // Assert
            Assert.IsFalse(mapped.IsSuccess);
            Assert.AreSame(exception, mapped.Exception);
        }

        #endregion

        #region 扩展方法测试 - ToApiResult

        [UnityTest]
        [Category("Unit")]
        public IEnumerator ToApiResult_WithSuccessTask_ReturnsSuccessResult()
        {
            // Arrange
            var expectedResponse = TestApiResponse.Success();
            UniTask<TestApiResponse> task = UniTask.FromResult(expectedResponse);

            // Act
            AsakiApiResult<TestApiResponse> result = new AsakiApiResult<TestApiResponse>();
            yield return task.ToApiResult().ContinueWith(r => result = r).ToCoroutine();

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreSame(expectedResponse, result.Response);
        }

        [UnityTest]
        [Category("Unit")]
        public IEnumerator ToApiResult_WithWebException_ReturnsErrorResult()
        {
            // Arrange
            var exception = new AsakiWebException("连接失败", 0, "https://test.com");
            UniTask<TestApiResponse> task = UniTask.FromException<TestApiResponse>(exception);

            // Act
            AsakiApiResult<TestApiResponse> result = new AsakiApiResult<TestApiResponse>();
            yield return task.ToApiResult().ContinueWith(r => result = r).ToCoroutine();

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreSame(exception, result.Exception);
        }

        [UnityTest]
        [Category("Unit")]
        public IEnumerator ToApiResult_WithCancellation_ReturnsErrorResult()
        {
            // Arrange
            var exception = new OperationCanceledException("操作已取消");
            UniTask<TestApiResponse> task = UniTask.FromException<TestApiResponse>(exception);

            // Act
            AsakiApiResult<TestApiResponse> result = new AsakiApiResult<TestApiResponse>();
            yield return task.ToApiResult().ContinueWith(r => result = r).ToCoroutine();

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsInstanceOf<OperationCanceledException>(result.Exception);
        }

        [UnityTest]
        [Category("Unit")]
        public IEnumerator ToApiResult_WithGenericException_ReturnsErrorResult()
        {
            // Arrange
            var exception = new InvalidOperationException("无效操作");
            UniTask<TestApiResponse> task = UniTask.FromException<TestApiResponse>(exception);

            // Act
            AsakiApiResult<TestApiResponse> result = new AsakiApiResult<TestApiResponse>();
            yield return task.ToApiResult().ContinueWith(r => result = r).ToCoroutine();

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreSame(exception, result.Exception);
        }

        #endregion

        #region 扩展方法测试 - EnsureSuccess

        [Test]
        [Category("Unit")]
        public void EnsureSuccess_WithSuccessResult_ReturnsResponse()
        {
            // Arrange
            var response = TestApiResponse.Success();
            var result = AsakiApiResult<TestApiResponse>.Ok(response);

            // Act
            var returnedResponse = result.EnsureSuccess();

            // Assert
            Assert.AreSame(response, returnedResponse);
        }

        [Test]
        [Category("Unit")]
        public void EnsureSuccess_WithFailureResult_ThrowsAsakiWebException()
        {
            // Arrange
            var result = AsakiApiResult<TestApiResponse>.Error(new Exception("发生错误"));

            // Act & Assert
            var exception = Assert.Throws<AsakiWebException>(() => result.EnsureSuccess());
            Assert.AreEqual("发生错误", exception.Message);
        }

        [Test]
        [Category("Unit")]
        public void EnsureSuccess_WithBusinessFailure_ThrowsAsakiWebException()
        {
            // Arrange
            var response = TestApiResponse.Failure(AsakiResponseCode.InvalidParameter, "参数无效");
            var result = AsakiApiResult<TestApiResponse>.Error(response);

            // Act & Assert
            var exception = Assert.Throws<AsakiWebException>(() => result.EnsureSuccess());
            Assert.AreEqual("参数无效", exception.Message);
        }

        #endregion
    }
}
