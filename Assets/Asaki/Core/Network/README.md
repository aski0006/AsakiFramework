# Asaki Core Network 模块

Asaki Core Network 模块提供了游戏开发中网络通信的基础抽象和核心实现，采用分层架构设计，支持请求/响应模式、拦截器机制、文件下载等功能。

## 目录结构

```
Assets/Asaki/Core/Network/
├── IAsakiRequest.cs              # 请求接口定义
├── IAsakiResponse.cs             # 响应接口定义
├── IAsakiWebService.cs           # Web服务接口定义
├── IAsakiDownloadService.cs      # 下载服务接口定义
├── AsakiRequestBase.cs           # 请求基类实现
├── AsakiResponseBase.cs          # 响应基类实现
├── AsakiResponseCode.cs          # 标准响应状态码
├── AsakiWebException.cs          # 网络异常类
└── Examples/
    └── NetworkRequestResponseExample.cs  # 使用示例
```

## 核心接口

### IAsakiRequest - 请求接口

定义所有 HTTP 请求的基础契约，继承自 `IAsakiSavable` 支持序列化。

```csharp
public interface IAsakiRequest : IAsakiSavable
{
    string RequestId { get; }           // 请求唯一标识符
    long Timestamp { get; }             // 请求时间戳（Unix毫秒）
    AsakiRequestValidationResult Validate();  // 请求数据验证
}
```

### IAsakiResponse - 响应接口

定义所有 HTTP 响应的基础契约。

```csharp
public interface IAsakiResponse : IAsakiSavable
{
    int Code { get; }                   // 业务状态码
    string Message { get; }             // 响应消息
    bool IsSuccess { get; }             // 是否成功
    string RequestId { get; }           // 关联请求ID
}

public interface IAsakiResponse<out TData> : IAsakiResponse
{
    TData Data { get; }                 // 响应数据载荷
}
```

### IAsakiWebService - Web服务接口

提供 HTTP 请求发送与拦截功能。

```csharp
public interface IAsakiWebService : IAsakiService, IDisposable
{
    void Setup(AsakiWebConfig config);
    void AddInterceptor(IAsakiWebInterceptor interceptor);
    void RemoveInterceptor(IAsakiWebInterceptor interceptor);
    
    UniTask<TResponse> GetAsync<TResponse>(string apiPath, CancellationToken token = default);
    UniTask<TResponse> PostAsync<TRequest, TResponse>(string apiPath, TRequest body, CancellationToken token = default);
    UniTask<TResponse> PostFormAsync<TResponse>(string apiPath, WWWForm form, CancellationToken token = default);
}
```

### IAsakiDownloadService - 下载服务接口

提供文件下载及进度监控功能。

```csharp
public interface IAsakiDownloadService : IAsakiService
{
    UniTask DownloadAsync(string url, string localPath, 
        IProgress<AsakiDownloadProgress> progress = null, 
        CancellationToken token = default);
    
    UniTask<long> GetFileSizeAsync(string url, CancellationToken token = default);
}
```

## 基类实现

### AsakiRequestBase - 请求基类

提供请求的统一实现，包含请求ID和时间戳管理。

```csharp
public abstract class AsakiRequestBase : IAsakiRequest
{
    public string RequestId { get; }    // 自动生成的GUID
    public long Timestamp { get; }      // 自动生成的Unix时间戳
    
    public virtual AsakiRequestValidationResult Validate() => AsakiRequestValidationResult.Success;
    
    protected abstract void SerializeCore(IAsakiWriter writer);
    protected abstract void DeserializeCore(IAsakiReader reader);
}

// 带数据的请求基类
public abstract class AsakiRequestBase<TData> : AsakiRequestBase
    where TData : IAsakiSavable, new()
{
    public TData Data { get; set; }
}
```

### AsakiResponseBase - 响应基类

提供响应的统一实现，包含状态码和消息管理。

```csharp
public abstract class AsakiResponseBase : IAsakiResponse
{
    public int Code { get; }
    public string Message { get; }
    public bool IsSuccess { get; }      // Code == 0
    public string RequestId { get; }
    
    protected void SetSuccess(string message = null);
    protected void SetError(int code, string message = null);
    
    protected abstract void SerializeCore(IAsakiWriter writer);
    protected abstract void DeserializeCore(IAsakiReader reader);
}

// 带数据的响应基类
public abstract class AsakiResponseBase<TData> : AsakiResponseBase, IAsakiResponse<TData>
    where TData : IAsakiSavable, new()
{
    public TData Data { get; }
    
    protected void SetSuccess(TData data, string message = null);
}
```

## 响应状态码

`AsakiResponseCode` 定义了一套标准的业务状态码规范：

| 状态码 | 名称 | 说明 |
|--------|------|------|
| 0 | Success | 操作成功 |
| 1 | GeneralError | 通用错误 |
| 1001 | InvalidParameter | 参数错误 |
| 1002 | MissingParameter | 缺少必要参数 |
| 1003 | InvalidParameterFormat | 参数格式错误 |
| 2001 | Unauthorized | 未授权 |
| 2002 | TokenExpired | Token过期 |
| 2003 | InvalidToken | Token无效 |
| 2004 | InsufficientPermission | 权限不足 |
| 3001 | ResourceNotFound | 资源不存在 |
| 3002 | ResourceAlreadyExists | 资源已存在 |
| 3003 | ResourceBusy | 资源被占用 |
| 4001 | NetworkError | 网络错误 |
| 4002 | RequestTimeout | 请求超时 |
| 5001 | ServerError | 服务器内部错误 |
| 5002 | ServiceUnavailable | 服务不可用 |
| 5003 | ServerMaintenance | 服务器维护中 |

### 状态码工具方法

```csharp
AsakiResponseCode.IsSuccess(code);           // 是否成功
AsakiResponseCode.IsClientError(code);       // 是否客户端错误 (1000-1999)
AsakiResponseCode.IsAuthError(code);         // 是否授权错误 (2000-2999)
AsakiResponseCode.IsResourceError(code);     // 是否资源错误 (3000-3999)
AsakiResponseCode.IsNetworkError(code);      // 是否网络错误 (4000-4999)
AsakiResponseCode.IsServerError(code);       // 是否服务器错误 (5000-5999)
AsakiResponseCode.GetDefaultMessage(code);   // 获取默认消息
```

## 拦截器机制

通过 `IAsakiWebInterceptor` 接口实现请求/响应拦截：

```csharp
public interface IAsakiWebInterceptor
{
    void OnRequest(UnityWebRequest uwr);                    // 请求发送前
    bool OnResponse(UnityWebRequest uwr);                   // 收到响应后
    void OnError(UnityWebRequest uwr, Exception ex);        // 发生异常时
}
```

典型应用场景：
- 自动添加 Token 认证头
- 统一错误处理
- 请求日志记录
- 请求重试机制

## 使用示例

### 1. 定义数据模型

```csharp
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

public class LoginResultData : IAsakiSavable
{
    public string Token { get; set; }
    public long ExpiresAt { get; set; }
    public string UserId { get; set; }
    
    // 实现 Serialize/Deserialize...
}
```

### 2. 创建请求/响应类

```csharp
public class LoginRequest : AsakiRequestBase<LoginData>
{
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
        return AsakiRequestValidationResult.Success;
    }
}

public class LoginResponse : AsakiResponseBase<LoginResultData>
{
    public static LoginResponse Success(LoginResultData data, string message = null)
    {
        var response = new LoginResponse();
        response.SetSuccess(data, message);
        return response;
    }

    public static LoginResponse Failure(int code, string message = null)
    {
        var response = new LoginResponse();
        response.SetError(code, message);
        return response;
    }
}
```

### 3. 验证和发送请求

```csharp
// 创建请求
var loginRequest = new LoginRequest("player123", "password123");

// 验证请求
var validation = loginRequest.Validate();
if (!validation.IsValid)
{
    Debug.LogError($"请求验证失败: {validation.ErrorMessage}");
    return;
}

// 通过 WebService 发送请求（需要 Unity 层的实现）
// var response = await webService.PostAsync<LoginRequest, LoginResponse>("/api/login", loginRequest);
```

### 4. 处理响应

```csharp
// 创建成功响应
var successResponse = LoginResponse.Success(
    new LoginResultData
    {
        Token = "eyJhbGciOiJIUzI1NiIs...",
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeMilliseconds(),
        UserId = "user_12345",
    },
    "登录成功"
);

if (successResponse.IsSuccess)
{
    Debug.Log($"登录成功，Token: {successResponse.Data.Token}");
}

// 创建失败响应
var errorResponse = LoginResponse.Failure(
    AsakiResponseCode.InvalidParameter,
    "用户名或密码错误"
);

Debug.Log($"错误码: {errorResponse.Code}");
Debug.Log($"错误消息: {errorResponse.Message}");
```

## 异常处理

```csharp
try
{
    var response = await webService.GetAsync<PlayerDataResponse>("/api/player");
}
catch (AsakiWebException ex)
{
    Debug.LogError($"网络请求失败: {ex.Message}");
    Debug.LogError($"HTTP状态码: {ex.ResponseCode}");
    Debug.LogError($"请求URL: {ex.Url}");
}
```

## 架构说明

### 分层设计

```
┌─────────────────────────────────────┐
│         Unity 实现层                 │  ← Assets/Asaki/Unity/Services/Network
│   (AsakiWebService, AsakiRequest)   │
├─────────────────────────────────────┤
│           Core 抽象层                │  ← Assets/Asaki/Core/Network (本模块)
│  (IAsakiWebService, IAsakiRequest)  │
├─────────────────────────────────────┤
│         序列化抽象层                 │  ← Assets/Asaki/Core/Serialization
│      (IAsakiSavable, IAsakiWriter)  │
└─────────────────────────────────────┘
```

### 设计特点

1. **接口与实现分离**：Core 层定义抽象接口，Unity 层提供具体实现
2. **零GC分配**：使用 `struct` 和 `UniTask` 减少内存分配
3. **类型安全**：泛型接口确保请求/响应类型正确
4. **可扩展性**：拦截器机制支持横切关注点
5. **标准化**：统一的状态码和消息规范

## 依赖关系

- `Asaki.Core.Serialization` - 序列化接口
- `Asaki.Core.Context` - 服务上下文
- `Asaki.Core.Configs` - 配置系统
- `Cysharp.Threading.Tasks` - 异步任务支持

## 相关模块

- [Unity Services Network](../Unity/Services/Network/) - Unity 层的具体实现
- [Core Serialization](../Serialization/) - 序列化系统
- [Core Context](../Context/) - 服务上下文管理
