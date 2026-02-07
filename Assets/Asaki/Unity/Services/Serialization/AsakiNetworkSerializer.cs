using System;
using System.Text;
using Asaki.Core.Network;
using Asaki.Core.Serialization;
using Asaki.Unity.Utils;

namespace Asaki.Unity.Services.Serialization
{
    /// <summary>
    /// 网络请求/响应序列化器
    /// </summary>
    /// <remarks>
    /// 提供将请求/响应对象与JSON字符串之间的双向转换功能，
    /// 集成AsakiJsonWriter和AsakiJsonReader实现高效的序列化/反序列化。
    /// </remarks>
    public static class AsakiNetworkSerializer
    {
        /// <summary>
        /// 将请求对象序列化为JSON字符串
        /// </summary>
        /// <typeparam name="TRequest">请求类型</typeparam>
        /// <param name="request">请求对象</param>
        /// <returns>JSON字符串</returns>
        public static string SerializeRequest<TRequest>(TRequest request)
            where TRequest : IAsakiRequest
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            StringBuilder sb = AsakiStringBuilderPool.Rent();
            try
            {
                AsakiJsonWriter writer = new AsakiJsonWriter(sb, false);
                request.Serialize(writer);
                return sb.ToString();
            }
            finally
            {
                AsakiStringBuilderPool.Return(sb);
            }
        }

        /// <summary>
        /// 将响应对象序列化为JSON字符串
        /// </summary>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="response">响应对象</param>
        /// <returns>JSON字符串</returns>
        public static string SerializeResponse<TResponse>(TResponse response)
            where TResponse : IAsakiResponse
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response));

            StringBuilder sb = AsakiStringBuilderPool.Rent();
            try
            {
                AsakiJsonWriter writer = new AsakiJsonWriter(sb, false);
                response.Serialize(writer);
                return sb.ToString();
            }
            finally
            {
                AsakiStringBuilderPool.Return(sb);
            }
        }

        /// <summary>
        /// 从JSON字符串反序列化请求对象
        /// </summary>
        /// <typeparam name="TRequest">请求类型</typeparam>
        /// <param name="json">JSON字符串</param>
        /// <returns>请求对象</returns>
        public static TRequest DeserializeRequest<TRequest>(string json)
            where TRequest : IAsakiRequest, new()
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("JSON string cannot be null or empty", nameof(json));

            AsakiJsonReader reader = AsakiJsonReader.FromJson(json);
            TRequest request = new TRequest();
            request.Deserialize(reader);
            return request;
        }

        /// <summary>
        /// 从JSON字符串反序列化响应对象
        /// </summary>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="json">JSON字符串</param>
        /// <returns>响应对象</returns>
        public static TResponse DeserializeResponse<TResponse>(string json)
            where TResponse : IAsakiResponse, new()
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("JSON string cannot be null or empty", nameof(json));

            AsakiJsonReader reader = AsakiJsonReader.FromJson(json);
            TResponse response = new TResponse();
            response.Deserialize(reader);
            return response;
        }

        /// <summary>
        /// 尝试从JSON字符串反序列化请求对象
        /// </summary>
        /// <typeparam name="TRequest">请求类型</typeparam>
        /// <param name="json">JSON字符串</param>
        /// <param name="request">输出请求对象</param>
        /// <returns>是否成功</returns>
        public static bool TryDeserializeRequest<TRequest>(string json, out TRequest request)
            where TRequest : IAsakiRequest, new()
        {
            request = default;

            if (string.IsNullOrEmpty(json))
                return false;

            try
            {
                request = DeserializeRequest<TRequest>(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 尝试从JSON字符串反序列化响应对象
        /// </summary>
        /// <typeparam name="TResponse">响应类型</typeparam>
        /// <param name="json">JSON字符串</param>
        /// <param name="response">输出响应对象</param>
        /// <returns>是否成功</returns>
        public static bool TryDeserializeResponse<TResponse>(string json, out TResponse response)
            where TResponse : IAsakiResponse, new()
        {
            response = default;

            if (string.IsNullOrEmpty(json))
                return false;

            try
            {
                response = DeserializeResponse<TResponse>(json);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 网络序列化扩展方法
    /// </summary>
    public static class AsakiNetworkSerializerExtensions
    {
        /// <summary>
        /// 将请求对象转换为JSON字符串
        /// </summary>
        public static string ToJsonRequest<TRequest>(this TRequest request)
            where TRequest : IAsakiRequest
        {
            return AsakiNetworkSerializer.SerializeRequest(request);
        }

        /// <summary>
        /// 将响应对象转换为JSON字符串
        /// </summary>
        public static string ToJsonResponse<TResponse>(this TResponse response)
            where TResponse : IAsakiResponse
        {
            return AsakiNetworkSerializer.SerializeResponse(response);
        }
    }
}
