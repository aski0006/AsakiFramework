# Asaki Framework 架构指南

## 概述

Asaki Framework 采用模块化架构设计，基于依赖注入（DI）原则构建。框架分为四个主要层次：

```
┌─────────────────────────────────────────┐
│           Editor Layer                  │
│  (编辑器工具、调试窗口、可视化编辑器)       │
├─────────────────────────────────────────┤
│          Unity Layer                    │
│  (Unity 服务实现、MonoBehaviour 桥接)    │
├─────────────────────────────────────────┤
│           Core Layer                    │
│  (核心架构、接口定义、基础服务)            │
├─────────────────────────────────────────┤
│         Plugins Layer                   │
│  (第三方插件集成、扩展功能)               │
└─────────────────────────────────────────┘
```

## 核心概念

### 1. 依赖注入 (DI)

框架使用基于特性的依赖注入系统：

```csharp
// 标记服务接口
public interface IMyService : IAsakiService { }

// 实现服务
public class MyService : IMyService { }

// 自动注入
public class MyController : MonoBehaviour, IAsakiAutoInject
{
    [AsakiInject] private IMyService _myService;
}
```

### 2. 模块系统

功能通过模块进行组织：

```csharp
public class MyModule : IAsakiModule
{
    public void ConfigureServices(AsakiContext context)
    {
        context.AddService<IMyService, MyService>();
    }
}
```

### 3. 命令模式

用于封装操作，支持撤销/重做：

```csharp
public class MoveCommand : AsakiCommand
{
    protected override void OnExecute()
    {
        // 执行操作
    }
    
    protected override void OnUndo()
    {
        // 撤销操作
    }
}
```

### 4. 事件总线

松耦合的事件通信：

```csharp
// 定义事件
public struct PlayerDamagedEvent : IAsakiEvent
{
    public int Damage;
}

// 发布事件
AsakiBroker.Publish(new PlayerDamagedEvent { Damage = 10 });

// 订阅事件
AsakiBroker.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
```

### 5. MVVM 绑定

响应式数据绑定：

```csharp
public class ViewModel
{
    [AsakiBind] public AsakiProperty<int> Score { get; } = new(0);
}

// 在 View 中绑定
viewModel.Score.Bind(value => scoreText.text = value.ToString());
```

## 服务生命周期

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Register  │ --> │   Resolve   │ --> │    Init     │
│  (注册服务)  │     │  (解析依赖)  │     │  (初始化)    │
└─────────────┘     └─────────────┘     └──────┬──────┘
                                               │
                                               v
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Dispose   │ <-- │    Tick     │ <-- │    Start    │
│  (释放资源)  │     │  (每帧更新)  │     │  (开始运行)  │
└─────────────┘     └─────────────┘     └─────────────┘
```

## 最佳实践

1. **服务设计** - 保持服务单一职责，接口与实现分离
2. **模块划分** - 按功能领域划分模块，避免循环依赖
3. **事件使用** - 事件用于跨模块通信，避免过度使用
4. **资源管理** - 使用框架提供的资源服务，避免直接调用 Resources
5. **生命周期** - 注意 MonoBehaviour 和框架服务的生命周期差异

## 性能优化

- 使用对象池避免频繁分配
- 利用 Burst 编译器优化计算密集型代码
- 使用 UniTask 替代协程减少 GC
- 合理使用查询缓存避免重复计算
