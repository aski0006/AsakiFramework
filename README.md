# Asaki Framework

<p align="center">
  <img src="https://img.shields.io/badge/Unity-2022.3+-blue.svg?style=flat-square" alt="Unity Version">
  <img src="https://img.shields.io/badge/.NET-4.7.1+-blue.svg?style=flat-square" alt=".NET Version">
  <img src="https://img.shields.io/badge/License-MIT-green.svg?style=flat-square" alt="License">
</p>

<p align="center">
  <b>高性能、模块化、可扩展的 Unity 游戏开发框架</b>
</p>

---

## 项目初衷

Asaki Framework 诞生于对 Unity 游戏开发中常见痛点的深度思考。在多年的游戏开发实践中，我们发现：

- **代码耦合严重**：业务逻辑与框架代码纠缠不清，难以维护和测试
- **性能瓶颈频发**：GC Alloc 过高、对象创建销毁开销大、异步操作混乱
- **架构难以扩展**：新功能接入困难，旧代码改动牵一发而动全身
- **团队协作低效**：缺乏统一的开发规范和架构约束

Asaki Framework 旨在解决这些问题，提供一个**高性能、高内聚、低耦合**的游戏开发基础设施，让开发者能够专注于游戏逻辑本身，而非底层架构的繁琐细节。

---

## 设计思想

### 1. 极速微内核架构 (Ultra-Lightweight Microkernel)

Asaki 的核心是一个极简的服务容器，采用 **Copy-On-Write + Snapshot Swap** 策略：

- **读操作 O(1)**：无锁访问，性能等同于原生 Dictionary
- **写操作隔离**：仅在启动期发生，运行时零分配
- **冻结机制**：初始化完成后冻结容器，防止运行时架构腐化

```csharp
// 极速服务获取 - 无锁、零分配
var audioService = AsakiContext.Get<IAsakiAudioService>();
```

### 2. 模块化生命周期管理 (Module Lifecycle Management)

采用 DAG（有向无环图）依赖解析，确保模块按正确顺序初始化：

```csharp
public interface IAsakiModule : IAsakiService
{
    void OnInit();           // 同步初始化
    UniTask OnInitAsync();   // 异步初始化
    void OnDispose();        // 销毁清理
}
```

### 3. 响应式架构 (CQRS + Event-Driven)

基于命令查询职责分离和事件总线，实现清晰的业务逻辑分层：

```csharp
// Command - 写操作
public class IncrementCounterCommand : AsakiCommand { }

// Query - 读操作
public class GetCounterValueQuery : AsakiQuery<int> { }

// Event - 通知
public class AchievementUnlockedEvent : IAsakiEvent { }
```

### 4. 零分配对象池 (Zero-Allocation Pooling)

智能对象池系统，支持自动治理和内存压力响应：

- **LRU 淘汰策略**：自动回收长期未使用对象
- **低内存自动收缩**：响应系统内存压力事件
- **预加载与异步扩容**：平滑处理突发请求

### 5. 编译时代码生成 (Compile-Time Code Generation)

通过 Roslyn Source Generator 减少运行时反射开销：

- 依赖注入代码自动生成
- 配置注册表自动生成
- 类型桥接自动生成

---

## 核心特性

| 特性 | 描述 | 状态 |
|------|------|------|
| **服务容器** | 极速无锁服务定位器 | ✅ 稳定 |
| **依赖注入** | 属性和构造函数注入 | ✅ 稳定 |
| **模块化系统** | DAG 依赖解析，支持异步初始化 | ✅ 稳定 |
| **对象池** | 智能治理，自动内存管理 | ✅ 稳定 |
| **事件总线** | 类型安全的事件发布订阅 | ✅ 稳定 |
| **响应式架构** | CQRS + 命令/查询/事件 | ✅ 稳定 |
| **配置系统** | CSV/Binary 热重载 | ✅ 稳定 |
| **资源管理** | Addressables/Resources 统一抽象 | ✅ 稳定 |
| **音频系统** | 状态机驱动的音频管理 | ✅ 稳定 |
| **UI 框架** | MVVM 数据绑定，层级管理 | ✅ 稳定 |
| **存档系统** | 版本化存档，自动迁移 | ✅ 稳定 |
| **日志系统** | 异步文件写入，分级过滤 | ✅ 稳定 |
| **网络服务** | HTTP 请求，下载管理 | ✅ 稳定 |
| **安全协程** | 异常隔离，生命周期管理 | ✅ 稳定 |
| **可视化编辑器** | 节点编辑器，模块依赖图 | ✅ 稳定 |

---

## 快速开始

### 环境要求

- Unity 2022.3 LTS 或更高版本
- .NET Framework 4.7.1 或 .NET Standard 2.1
- UniTask 2.x（异步操作支持）
- Optional: Addressables（资源管理）

### 安装

#### 1. 手动安装

1. 将 `Assets/Asaki` 文件夹复制到你的项目中
2. 确保项目已导入 UniTask 包
3. 在首场景中创建 `AsakiBootstrapper`

#### 2. 通过 Package Manager 安装

1. 打开 Unity Package Manager
2. 点击 "+" -> "Add package from git URL"
3. 输入 `https://github.com/yourusername/Asaki.git?path=Assets/Asaki/#{Version}`
   替换 `#{Version}` 为你要安装的版本号，例如 `v1.0.0`
4. 点击 "Add"

### 基础配置

创建 `AsakiConfig` 配置文件：

```csharp
[CreateAssetMenu(fileName = "AsakiConfig", menuName = "Asaki/Config")]
public class AsakiConfig : ScriptableObject
{
    public int TickRate = 60;
    public AsakiLogConfig LogConfig;
    public AsakiAudioConfig AudioConfig;
    // ... 其他配置
}
```

### 引导程序设置

在场景中创建 `AsakiBootstrapper`：

```csharp
public class AsakiBootstrapper : MonoBehaviour
{
    [SerializeField] private AsakiConfig _config;
    [SerializeField] private MonoBehaviour[] _globalBehaviourServices;
    
    private async void Start()
    {
        // 自动发现模块并按 DAG 顺序初始化
        var discovery = new AsakiStaticModuleDiscovery();
        await AsakiModuleLoader.Startup(discovery);
        
        // 冻结容器，防止运行时修改
        AsakiContext.Freeze();
        
        // 广播框架就绪事件
        AsakiBroker.Publish(new OnAsakiFrameworkReadyEvent());
    }
}
```

### 创建你的第一个模块

```csharp
[AsakiModule(
    id: "MyGameModule",
    dependencies: new[] { "AudioModule", "UIModule" }
)]
public class MyGameModule : IAsakiModule
{
    private IAsakiAudioService _audioService;
    
    public void OnInit()
    {
        // 获取依赖服务
        _audioService = AsakiContext.Get<IAsakiAudioService>();
        
        // 注册本模块提供的服务
        AsakiContext.Register<IMyGameService>(new MyGameService());
    }
    
    public async UniTask OnInitAsync()
    {
        // 异步加载资源
        await LoadGameAssetsAsync();
    }
    
    public void OnDispose()
    {
        // 清理资源
    }
}
```

### 使用响应式架构

```csharp
// 1. 定义架构
public class GameArchitecture : AsakiArchitecture
{
    protected override void OnSetup()
    {
        RegisterModel(new GameModel());
        RegisterSystem(new GameSystem());
    }
}

// 2. 定义命令
public class StartGameCommand : AsakiCommand
{
    public override void Execute()
    {
        var model = Architecture.GetModel<GameModel>();
        model.GameState.Value = GameState.Playing;
    }
}

// 3. 在 View 中使用
public class GameView : MonoBehaviour, IAsakiAutoInject
{
    [AsakiInject] private GameArchitecture _architecture;
    
    private void Start()
    {
        // 执行命令
        _architecture.Execute(new StartGameCommand());
        
        // 订阅事件
        AsakiBroker.Subscribe(new GameStateChangedHandler());
    }
}
```

### 对象池使用

```csharp
// 创建对象池
var pool = await AsakiContext.Get<IAsakiPoolService>().CreatePoolAsync(
    key: "Projectiles",
    factory: new ProjectileFactory(projectilePrefab),
    config: new AsakiPoolConfig 
    { 
        InitialSize = 10, 
        MaxSize = 100,
        AutoShrink = true
    }
);

// 获取对象
var projectile = await pool.GetAsync();

// 归还对象
pool.Return(projectile);
```

---

## 项目文档 (DeepWiki)
- **[项目](https://deepwiki.com/aski0006/AsakiFramework)**
---

## 示例项目

项目包含完整的示例场景：

- **CounterExample** - 响应式架构基础示例
- **MVVMExample** - 数据绑定与 UI 响应式更新
- **PoolExample** - 对象池使用演示
- **AudioExample** - 音频系统使用演示
- **SaveExample** - 存档系统与版本迁移

---

## 性能基准

在 Unity 2022.3 LTS 上的测试数据：

| 操作 | 耗时 | GC Alloc |
|------|------|----------|
| 服务获取 (Get<T>) | ~15ns | 0 B |
| 事件发布 | ~50ns | 0 B |
| 对象池获取 | ~20ns | 0 B |
| 命令执行 | ~30ns | 0 B |

*测试环境：Intel i7-12700K, 32GB RAM*

---

## 贡献指南

我们欢迎社区贡献！请遵循以下规范：

1. **代码风格**：遵循 C# 编码规范，使用 CSharpier 格式化
2. **提交信息**：使用语义化提交信息格式
3. **单元测试**：新功能必须包含单元测试
4. **文档更新**：API 变更需同步更新文档

---

## 许可证

本项目采用 [MIT License](LICENSE) 开源协议。

---

## 作者

**Asaki0019** - [GitHub](https://github.com/aski0006)

如有问题或建议，欢迎提交 Issue 或 Pull Request。

---

<p align="center">
  <i>Built with passion for game development</i>
</p>
