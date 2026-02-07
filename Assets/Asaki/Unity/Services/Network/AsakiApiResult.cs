using System;
using System.Threading;
using System.Threading.Tasks;
using Asaki.Core.Network;
using Cysharp.Threading.Tasks;

namespace Asaki.Unity.Services.Network
{
    /// <summary>
    /// API调用结果包装类，统一处理响应状态和错误
    /// </summary>
    /// <typeparam name="TResponse">响应类型</typeparam>
    /// <remarks>
    /// 提供了一种函数式编程风格来处理API响应，支持链式调用和模式匹配
    /// </remarks>
    public readonly struct AsakiApiResult<TResponse>
        where TResponse : IAsakiResponse
    {
        private readonly TResponse _response;
        private readonly Exception _exception;

        /// <summary>
        /// 获取响应对象
        /// </summary>
        public TResponse Response => _response;

        /// <summary>
        /// 获取异常对象（如果有）
        /// </summary>
        public Exception Exception => _exception;

        /// <summary>
        /// 是否成功（业务成功且无异常）
        /// </summary>
        public bool IsSuccess => _exception == null && _response != null && _response.IsSuccess;

        /// <summary>
        /// 是否失败（业务失败或有异常）
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// 获取业务状态码
        /// </summary>
        public int Code => _response?.Code ?? AsakiResponseCode.NetworkError;

        /// <summary>
        /// 获取响应消息
        /// </summary>
        public string Message => _exception?.Message ?? _response?.Message;

        /// <summary>
        /// 初始化API结果
        /// </summary>
        private AsakiApiResult(TResponse response, Exception exception)
        {
            _response = response;
            _exception = exception;
        }

        /// <summary>
        /// 创建成功的结果
        /// </summary>
        public static AsakiApiResult<TResponse> Ok(TResponse response)
        {
            return new AsakiApiResult<TResponse>(response, null);
        }

        /// <summary>
        /// 创建失败的结果
        /// </summary>
        public static AsakiApiResult<TResponse> Error(Exception exception)
        {
            return new AsakiApiResult<TResponse>(default, exception);
        }

        /// <summary>
        /// 创建失败的结果（带响应）
        /// </summary>
        public static AsakiApiResult<TResponse> Error(TResponse response)
        {
            return new AsakiApiResult<TResponse>(response, null);
        }

        /// <summary>
        /// 匹配处理结果
        /// </summary>
        /// <param name="onSuccess">成功时的处理函数</param>
        /// <param name="onFailure">失败时的处理函数</param>
        public void Match(Action<TResponse> onSuccess, Action<int, string> onFailure)
        {
            if (IsSuccess)
            {
                onSuccess?.Invoke(_response);
            }
            else
            {
                onFailure?.Invoke(Code, Message);
            }
        }

        /// <summary>
        /// 匹配处理结果（带返回值）
        /// </summary>
        public TResult Match<TResult>(Func<TResponse, TResult> onSuccess, Func<int, string, TResult> onFailure)
        {
            if (IsSuccess)
            {
                return onSuccess != null ? onSuccess(_response) : default;
            }
            else
            {
                return onFailure != null ? onFailure(Code, Message) : default;
            }
        }

        /// <summary>
        /// 成功时执行操作
        /// </summary>
        public AsakiApiResult<TResponse> OnSuccess(Action<TResponse> action)
        {
            if (IsSuccess)
            {
                action?.Invoke(_response);
            }
            return this;
        }

        /// <summary>
        /// 失败时执行操作
        /// </summary>
        public AsakiApiResult<TResponse> OnFailure(Action<int, string> action)
        {
            if (IsFailure)
            {
                action?.Invoke(Code, Message);
            }
            return this;
        }

        /// <summary>
        /// 映射响应数据到另一种类型
        /// </summary>
        public AsakiApiResult<TNewResponse> Map<TNewResponse>(Func<TResponse, TNewResponse> mapper)
            where TNewResponse : IAsakiResponse
        {
            if (IsSuccess)
            {
                return AsakiApiResult<TNewResponse>.Ok(mapper(_response));
            }
            return AsakiApiResult<TNewResponse>.Error(_exception);
        }
    }

    /// <summary>
    /// API调用结果扩展方法
    /// </summary>
    public static class AsakiApiResultExtensions
    {
        /// <summary>
        /// 将UniTask转换为API结果
        /// </summary>
        public static async UniTask<AsakiApiResult<TResponse>> ToApiResult<TResponse>(
            this UniTask<TResponse> task)
            where TResponse : IAsakiResponse
        {
            try
            {
                var response = await task;
                return AsakiApiResult<TResponse>.Ok(response);
            }
            catch (AsakiWebException ex)
            {
                return AsakiApiResult<TResponse>.Error(ex);
            }
            catch (OperationCanceledException ex)
            {
                return AsakiApiResult<TResponse>.Error(ex);
            }
            catch (Exception ex)
            {
                return AsakiApiResult<TResponse>.Error(ex);
            }
        }

        /// <summary>
        /// 确保响应成功，否则抛出异常
        /// </summary>
        public static TResponse EnsureSuccess<TResponse>(this AsakiApiResult<TResponse> result)
            where TResponse : IAsakiResponse
        {
            if (result.IsFailure)
            {
                throw new AsakiWebException(
                    result.Message,
                    result.Code,
                    result.Response?.RequestId ?? "unknown"
                );
            }
            return result.Response;
        }
    }
}
