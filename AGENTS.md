# Asaki Framework - 开发者指南

> **仓库**: [aski0006/AsakiFramework](https://github.com/aski0006/AsakiFramework)
> **最后更新**: 2026-02-01
> **Unity 版本**: 6000.0.23f1 (Unity 6)

---

## 项目概述

**Asaki Framework** 是一个面向 Unity 的模块化游戏开发框架，专注于提供高性能、可扩展的核心系统组件。框架采用平台无关的核心架构（Asaki.Core）与 Unity 特定实现（Asaki.Unity）分离的设计模式。

### 核心特性

- **模块化架构**: 基于优先级的模块系统，支持自动依赖注入和初始化
- **异步优先**: 全面使用 UniTask 实现异步编程模型
- **对象池**: 智能对象池系统，支持 LRU 淘汰和自动收缩
- **音频系统**: 状态机驱动的音频管理系统
- **事件总线**: 基于 Broker 的事件发布订阅系统
- **依赖注入**: 基于 Context 的服务容器
- **配置管理**: 二进制配置序列化系统

---

## 技术栈

### 核心依赖

| 包 | 版本 | 用途 |
|---|---|---|
| **UniTask** | 最新 | 异步编程，高性能协程替代方案 |
| **UniTask.Addressables** | - | Addressable 资源管理 |
| **Unity Addressables** | 2.2.2 | 异步资源加载 |
| **Universal RP** | 17.0.3 | 通用渲染管线 |
| **Input System** | 1.11.1 | 新版输入系统 |
| **NUnit** | - | 单元测试框架 |

### Unity 模块

- 2D Feature Set (2.0.1)
- Timeline (1.8.7)
- UGUI (2.0.0)
- Visual Scripting (1.9.4)
- Test Framework (1.4.5)

---

## 项目结构

```
Asaki/
├── Assets/
│   ├── Asaki/                    # 框架核心代码
│   │   ├── Core/                 # 平台无关的核心模块
│   │   │   ├── Architecture/     # 架构模式 (MVVM, Counter)
│   │   │   ├── Async/            # 异步操作工具
│   │   │   ├── Audio/            # 音频系统
│   │   │   ├── Blackboard/       # 黑板数据共享系统
│   │   │   ├── Broker/           # 事件总线
│   │   │   ├── Configs/          # 配置管理
│   │   │   ├── Context/          # 依赖注入容器
│   │   │   ├── FSM/              # 有限状态机
│   │   │   ├── Graphs/           # 图算法
│   │   │   ├── Logging/          # 日志系统
│   │   │   ├── Network/          # 网络通信
│   │   │   ├── Pooling/          # 对象池系统
│   │   │   ├── Reactive/         # 响应式编程
│   │   │   ├── Resources/        # 资源管理
│   │   │   ├── Scene/            # 场景管理
│   │   │   ├── Serialization/    # 序列化
│   │   │   ├── Simulation/       # 模拟服务
│   │   │   ├── Time/             # 时间管理
│   │   │   └── UI/               # UI 系统
│   │   │
│   │   ├── Unity/                # Unity 特定实现
│   │   │   ├── Bootstrapper/     # 启动引导
│   │   │   ├── Bridge/           # Core 到 Unity 的桥接
│   │   │   ├── Extensions/       # Unity 扩展方法
│   │   │   ├── Modules/          # Unity 模块实现
│   │   │   ├── Services/         # Unity 服务实现
│   │   │   └── Utils/            # 工具类
│   │   │
│   │   ├── Editor/               # 编辑器工具
│   │   ├── Plugins/              # 第三方插件集成
│   │   ├── Generated/            # 自动生成的代码
│   │   └── CodeGen/              # 代码生成工具
│   │
│   ├── Game/                     # 游戏项目代码
│   │   ├── Scripts/
│   │   │   ├── Examples/         # 框架使用示例
│   │   │   └── View/             # 视图层
│   │   └── Editor/               # 游戏编辑器工具
│   │
│   ├── Tests/                    # 单元测试
│   │   └── Pooling/              # 对象池测试
│   │
│   ├── StreamingAssets/          # 流式资源
│   │   ├── Configs/              # 配置文件 (JSON)
│   │   └── ConfigBin/            # 配置文件 (Binary)
│   │
│   └── Scenes/                   # 场景文件
│
├── Issues/                       # GitHub Issues 本地同步
├── ProjectSettings/              # Unity 项目设置
└── Packages/                     # 包管理配置
```

---

## 程序集依赖关系

| 程序集 | 命名空间 | 用途 | 依赖 |
|---|---|---|---|
| **Asaki.Core** | `Asaki.Core.*` | 核心框架（平台无关） | UniTask, UniTask.Addressables |
| **Asaki.Unity** | `Asaki.Unity.*` | Unity 特定实现 | Asaki.Core |
| **Asaki.Editor** | `Asaki.Editor.*` | 编辑器工具 | Asaki.Core, Asaki.Unity |
| **Asaki.Plugins** | `Asaki.Plugins.*` | 第三方插件集成 | - |
| **Asaki.Generated** | - | 自动生成代码 | - |
| **Game** | `Game.Scripts.*` | 游戏逻辑 | Asaki.Core, Asaki.Unity |
| **Tests** | - | 单元测试 | Asaki.*, NUnit |

---

## 核心系统详解

### 1. 模块系统 (AsakiModule)

框架使用基于优先级的模块系统，通过 `[AsakiModule]` 特性标记模块类。

```csharp
[AsakiModule(优先级, 依赖类型列表)]
public class ExampleModule : IAsakiModule
{
    public void OnInit() { /* 同步初始化 */ }
    public UniTask OnInitAsync() { /* 异步初始化 */ }
    public void OnDispose() { /* 清理资源 */ }
}
```

**核心模块**:
- **AsakiPoolModule** (优先级 150): 对象池服务
- **AsakiResourcesModule**: 资源管理
- **AsakiSimulationModule**: 模拟服务

### 2. 对象池系统 (Pooling)

高性能对象池实现，支持：
- 异步/同步对象创建
- 对象预热（Prewarm）
- 对象验证
- 重复归还检测
- LRU（最近最少使用）淘汰策略
- 自动收缩治理

**核心接口**:
- `IAsakiPool<T>`: 对象池接口
- `IAsakiPoolObjectFactory<T>`: 对象工厂接口
- `IAsakiPoolService`: 池服务接口

**使用示例**:
```csharp
// 获取池服务
var poolService = AsakiContext.Get<IAsakiPoolService>();

// 获取对象
var obj = await poolService.Get<MyClass>("poolKey");

// 归还对象
poolService.Return("poolKey", obj);
```

### 3. 音频系统 (Audio)

基于状态机的音频管理系统：
- `IAsakiAudioService`: 音频服务接口
- `AudioStateMachine`: 音频状态机
- `AudioPlaybackState`: 播放状态管理
- `AudioStateStatistics`: 统计数据

### 4. 依赖注入 (Context)

基于服务容器的依赖注入系统：
```csharp
// 注册服务
AsakiContext.Register<IExampleService>(new ExampleService());

// 获取服务
var service = AsakiContext.Get<IExampleService>();

// 自动注入
public class MyComponent : MonoBehaviour, IAsakiAutoInject
{
    [AsakiInject]
    public void Init(IExampleService service) { }
}
```

### 5. 事件系统 (Broker)

发布订阅模式的事件总线：
```csharp
// 发布事件
AsakiBroker.Publish<MyEvent>(new MyEvent());

// 订阅事件
AsakiBroker.Subscribe<MyEvent>(OnMyEvent);
```

### 6. 黑板系统 (Blackboard)

数据共享和状态管理系统。

---

## 构建与运行

### 构建项目

```bash
# 使用 Unity Editor 打开项目
# Unity 会自动构建所有程序集
```

### 运行测试

```bash
# 在 Unity Editor 中运行测试
# Window > General > Test Runner
```

测试结果保存在: `TestResults/TestResults_Pooling.xml`

### 编码规范

1. **命名约定**:
   - 接口使用 `I` 前缀（如 `IAsakiPool`）
   - Asaki 框架类使用 `Asaki` 前缀
   - 私有字段使用 `_camelCase`

2. **异步编程**:
   - 优先使用 `UniTask` 而非 `Task`
   - 避免阻塞主线程
   - 使用 `CancellationToken` 支持取消

3. **错误处理**:
   - 使用 `ALog` 日志系统记录错误
   - 合理使用 try-catch 捕获异常
   - 避免静默失败

4. **线程安全**:
   - 对象池使用 `lock` 保证线程安全
   - 共享状态需要考虑并发访问

---

## 开发工作流

### 1. 创建新模块

```csharp
[AsakiModule(100)]
public class MyModule : IAsakiModule
{
    public void OnInit()
    {
        // 注册服务
        AsakiContext.Register<IMyService>(new MyService());
    }

    public UniTask OnInitAsync()
    {
        return UniTask.CompletedTask;
    }

    public void OnDispose()
    {
        // 清理资源
    }
}
```

### 2. 编写单元测试

在 `Assets/Tests/` 目录下创建测试类：

```csharp
using NUnit.Framework;
using Asaki.Core.Pooling;

[TestFixture]
public class PoolTests
{
    [Test]
    public void TestPoolCreation()
    {
        // 测试代码
    }
}
```

### 3. 配置管理

配置文件位于 `Assets/StreamingAssets/Configs/` (JSON) 或 `ConfigBin/` (Binary)

---

## 已知问题与待办事项

### 当前 Open Issues (4 个)

| # | 标题 | 类型 |
|---|------|------|
| #5 | Reactive & Strongly-Typed I18n: Zero-String & Hot-Switching | Feature |
| #4 | Runtime Developer Console: Command Binding & Reactive Watcher | Feature |
| #3 | Smart Pool Governance: Auto-Shrink & LRU Eviction | Feature |
| #2 | Data Versioning & Migration Pipeline | Feature |

详见 `Issues/` 目录。

---

## Git 工作流

### 分支信息
- **远程仓库**: https://github.com/aski0006/AsakiFramework.git
- **当前 HEAD**: 31b0dfa03a5d8be6d7ab2d958bad2b9c7651fe55

### 提交规范
提交信息应遵循"为什么"而非"是什么"的原则。

### 常用命令

```bash
# 查看状态
git status

# 查看差异
git diff HEAD

# 查看历史
git log -n 3

# 同步 Issues（手动）
# 将 GitHub Issues 下载到 Issues/ 目录
```

---

## 工具与环境

### IDE
- **Rider**: 推荐的 C# IDE (已集成)
- **Visual Studio**: 备选 IDE

### 命令行工具
- **Git**: 2.51.0.windows.1
- **PowerShell**: 默认命令行环境
- **Python**: 3.13.5 (用于辅助脚本)

### 注意事项
- Windows 环境下使用 `dir` 而非 `ls`
- PowerShell 不支持 `&&` 链式命令，使用 `;` 分隔
- 路径分隔符使用 `\` 但 `/` 在多数工具中也可接受

---

## 示例代码位置

框架提供完整的使用示例，位于 `Assets/Game/Scripts/Examples/`：

- `AsakiAudioExample.cs`: 音频系统示例
- `AsakiBlackboardValueSchameTest.cs`: 黑板系统测试
- `AsakiDownloadTest.cs`: 下载功能测试
- `AsakiJsonTest.cs`: JSON 序列化测试
- `AsakiLogTest.cs`: 日志系统测试
- `AsakiSceneTest.cs`: 场景管理测试
- `AsakiWebTest.cs`: Web 请求测试
- `BrokerExample.cs`: 事件总线示例
- `ConfigExample.cs`: 配置管理示例
- `MVVMExample.cs`: MVVM 模式示例
- `SaveExample.cs`: 存档系统示例

---

## 贡献指南

1. **代码风格**: 遵循现有代码的命名和结构约定
2. **测试**: 为新功能编写单元测试
3. **文档**: 更新相关文档和示例
4. **提交**: 提交前确保项目可正常构建

---

## 联系方式

- **项目主页**: https://github.com/aski0006/AsakiFramework
- **问题反馈**: GitHub Issues

---

*最后更新: 2026-02-01*