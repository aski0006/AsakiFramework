# Asaki Framework

[![Unity Version](https://img.shields.io/badge/Unity-2022.3%2B-blue.svg)](https://unity3d.com)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

一个全面的 Unity 游戏开发框架，提供架构模式、服务管理、UI 系统和开发工具。

## 功能特性

### 核心架构 (Core)
- **依赖注入 (DI)** - 基于注解的服务定位和自动注入
- **命令模式 (Command)** - 支持撤销/重做的命令系统
- **查询模式 (Query)** - 类型安全的查询缓存系统
- **事件总线 (Event Bus)** - 松耦合的事件通信机制
- **MVVM 绑定** - 响应式数据绑定系统
- **状态机 (FSM)** - 灵活的状态机实现

### 服务系统
- **音频管理** - 分层音频系统，支持背景音乐、音效、语音
- **UI 管理** - 窗口管理系统，支持层级和动画
- **资源管理** - 基于 Addressables 的资源加载策略
- **场景管理** - 场景加载和过渡效果
- **对象池** - 高性能对象池系统
- **序列化** - JSON 和二进制存档系统
- **网络服务** - HTTP 请求和下载管理
- **时间管理** - 计时器和调度服务

### 编辑器工具
- **配置烘焙器** - 将 CSV/JSON 配置转换为二进制
- **调试窗口** - 运行时调试和监控工具
- **图编辑器** - 可视化节点图编辑器
- **资源工具** - 批量重命名、查找重复资源等

## 安装方法

### 通过 Package Manager

1. 打开 Unity 编辑器
2. 进入 `Window > Package Manager`
3. 点击 `+` 按钮，选择 `Add package from git URL`
4. 输入：`https://github.com/aski0006/AsakiFramework.git#v1.0.0`

### 通过 manifest.json

在 `Packages/manifest.json` 中添加：

```json
{
  "dependencies": {
    "com.asaki.framework": "https://github.com/aski0006/AsakiFramework.git#v1.0.0"
  }
}
```

## 快速开始

### 1. 初始化框架

```csharp
public class GameBootstrapper : AsakiBootstrapper
{
    protected override void ConfigureModules(AsakiContext context)
    {
        context.AddModule<AsakiLogModule>();
        context.AddModule<AsakiEventBusModule>();
        context.AddModule<AsakiPoolModule>();
        context.AddModule<AsakiUIModule>();
        context.AddModule<AsakiAudioModule>();
    }
}
```

### 2. 创建服务

```csharp
public interface IGameService : IAsakiService
{
    void DoSomething();
}

public class GameService : IGameService
{
    public void DoSomething()
    {
        // 实现
    }
}
```

### 3. 使用依赖注入

```csharp
public class GameController : MonoBehaviour, IAsakiAutoInject
{
    [AsakiInject] private IGameService _gameService;
    [AsakiInject] private IAsakiUIService _uiService;

    void Start()
    {
        _gameService.DoSomething();
    }
}
```

### 4. 使用 MVVM 绑定

```csharp
public class PlayerViewModel
{
    [AsakiBind] public AsakiProperty<int> Health { get; } = new(100);
    [AsakiBind] public AsakiProperty<string> Name { get; } = new("Player");
}

public class PlayerView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI nameText;

    void Start()
    {
        var vm = new PlayerViewModel();
        vm.Health.Bind(v => healthText.text = $"Health: {v}");
        vm.Name.Bind(v => nameText.text = v);
    }
}
```

## 系统要求

- **Unity**: 2022.3 LTS 或更高版本
- **.NET**: .NET Standard 2.1
- **依赖包**:
  - Addressables 1.21.0+
  - Burst 1.8.0+
  - Collections 2.1.0+

## 文档

- [架构指南](Documentation~/Architecture.md)
- [API 参考](Documentation~/API.md)
- [示例项目](Samples~)

## 许可证

本项目采用 [MIT 许可证](LICENSE) 开源。

## 贡献

欢迎提交 Issue 和 Pull Request！

## 联系方式

- Email: aski0006@gmail.com
- GitHub: [@aski0006](https://github.com/aski0006)
