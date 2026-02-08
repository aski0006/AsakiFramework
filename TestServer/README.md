# Asaki 测试服务器

这是一个简单的 ASP.NET Core 测试服务器，用于测试 Unity 客户端的登录和注册功能。

## 启动服务器

```bash
cd TestServer
dotnet run
```

服务器将在以下地址运行：
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001

## API 接口

### 1. 健康检查
```
GET /
```

### 2. 登录
```
POST /api/auth/login
Content-Type: application/json

{
  "data": {
    "username": "admin",
    "password": "admin123"
  }
}
```

### 3. 注册
```
POST /api/auth/register
Content-Type: application/json

{
  "data": {
    "username": "newuser",
    "password": "password123"
  }
}
```

### 4. 获取所有用户（测试用）
```
GET /api/users
```

## 默认测试账号

- 用户名: `admin`
- 密码: `admin123`

## Unity 配置

在 Unity 中，将 `AsakiWebConfig` 的 BaseUrl 设置为：
```
http://localhost:5000
```
