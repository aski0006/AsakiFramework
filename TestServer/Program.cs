using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// 模拟用户数据库
var users = new Dictionary<string, User>
{
    ["admin"] = new User { UserID = "1", UserNickname = "Admin", Password = "admin123" }
};

// 健康检查
app.MapGet("/", () =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] GET / - Health check");
    return Results.Ok(new { Status = "Server is running", Time = DateTime.Now });
});

// 登录接口
app.MapPost("/api/auth/login", async (HttpRequest request) =>
{
    Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] POST /api/auth/login");
    
    try
    {
        // 读取原始JSON
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body);
        var jsonString = await reader.ReadToEndAsync();
        Console.WriteLine($"  Raw JSON: {jsonString}");
        
        // 手动解析JSON
        using var doc = JsonDocument.Parse(jsonString);
        var root = doc.RootElement;
        
        var requestId = root.GetProperty("requestId").GetString();
        var timestamp = root.GetProperty("timestamp").GetInt64();
        var dataElement = root.GetProperty("data");
        var username = dataElement.GetProperty("Username").GetString();
        var password = dataElement.GetProperty("Password").GetString();
        
        Console.WriteLine($"  Parsed - RequestId: {requestId}, Username: {username}");
        
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            var errorResponse = new { IsSuccess = false, Message = "Username and password are required" };
            Console.WriteLine($"  Response: {JsonSerializer.Serialize(errorResponse)}");
            return Results.BadRequest(errorResponse);
        }

        if (!users.TryGetValue(username, out var user))
        {
            var errorResponse = new { IsSuccess = false, Message = "User not found" };
            Console.WriteLine($"  Response: {JsonSerializer.Serialize(errorResponse)}");
            return Results.Ok(errorResponse);
        }

        if (user.Password != password)
        {
            var errorResponse = new { IsSuccess = false, Message = "Invalid password" };
            Console.WriteLine($"  Response: {JsonSerializer.Serialize(errorResponse)}");
            return Results.Ok(errorResponse);
        }

        var successResponse = new
        {
            IsSuccess = true,
            Message = "Login successful",
            Data = new { user.UserID, user.UserNickname }
        };
        Console.WriteLine($"  Response: {JsonSerializer.Serialize(successResponse)}");
        return Results.Ok(successResponse);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Error: {ex.Message}");
        Console.WriteLine($"  StackTrace: {ex.StackTrace}");
        return Results.BadRequest(new { IsSuccess = false, Message = $"Invalid request: {ex.Message}" });
    }
});

// 注册接口
app.MapPost("/api/auth/register", async (HttpRequest request) =>
{
    Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] POST /api/auth/register");
    
    try
    {
        // 读取原始JSON
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body);
        var jsonString = await reader.ReadToEndAsync();
        Console.WriteLine($"  Raw JSON: {jsonString}");
        
        // 手动解析JSON
        using var doc = JsonDocument.Parse(jsonString);
        var root = doc.RootElement;
        
        var requestId = root.GetProperty("requestId").GetString();
        var timestamp = root.GetProperty("timestamp").GetInt64();
        var dataElement = root.GetProperty("data");
        var username = dataElement.GetProperty("Username").GetString();
        var password = dataElement.GetProperty("Password").GetString();
        
        Console.WriteLine($"  Parsed - RequestId: {requestId}, Username: {username}");
        
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            var errorResponse = new { IsSuccess = false, Message = "Username and password are required" };
            Console.WriteLine($"  Response: {JsonSerializer.Serialize(errorResponse)}");
            return Results.BadRequest(errorResponse);
        }

        if (users.ContainsKey(username))
        {
            var errorResponse = new { IsSuccess = false, Message = "Username already exists" };
            Console.WriteLine($"  Response: {JsonSerializer.Serialize(errorResponse)}");
            return Results.Ok(errorResponse);
        }

        var newUser = new User
        {
            UserID = Guid.NewGuid().ToString("N")[..8],
            UserNickname = username,
            Password = password
        };

        users[username] = newUser;

        var successResponse = new { IsSuccess = true, Message = "Registration successful" };
        Console.WriteLine($"  Response: {JsonSerializer.Serialize(successResponse)}");
        return Results.Ok(successResponse);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Error: {ex.Message}");
        Console.WriteLine($"  StackTrace: {ex.StackTrace}");
        return Results.BadRequest(new { IsSuccess = false, Message = $"Invalid request: {ex.Message}" });
    }
});

// 获取所有用户（测试用）
app.MapGet("/api/users", () =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] GET /api/users");
    var response = new
    {
        IsSuccess = true,
        Data = users.Values.Select(u => new { u.UserID, u.UserNickname })
    };
    return Results.Ok(response);
});

Console.WriteLine("\n========================================");
Console.WriteLine("  Asaki 测试服务器已启动");
Console.WriteLine("========================================");
Console.WriteLine("API 端点:");
Console.WriteLine("  GET  /                 - 健康检查");
Console.WriteLine("  POST /api/auth/login   - 登录");
Console.WriteLine("  POST /api/auth/register - 注册");
Console.WriteLine("  GET  /api/users        - 获取所有用户");
Console.WriteLine("========================================\n");

app.Run();

// 数据模型
public class User
{
    public string UserID { get; set; } = string.Empty;
    public string UserNickname { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
