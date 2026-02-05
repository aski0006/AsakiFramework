# Asaki Framework v1.0.0 Release Notes

**发布日期**: 2026年1月31日  
**版本**: v1.0.0  
**状态**: 正式版 (Stable)

---

## 版本信息

- **语义化版本**: 1.0.0
- **Unity 版本要求**: 2022.3 LTS 或更高版本
- **.NET 版本**: .NET Standard 2.1
- **包名**: `com.asaki.framework`
- **发布年份**: 2026

---

## 依赖项

### 必须安装

| 包名 | 版本 | 说明 |
|------|------|------|
| UniTask | 2.9.0+ | 高性能异步任务库，框架核心依赖 |
| TextMeshPro (TMP) | 3.0.0+ | Unity 官方文本渲染解决方案 |

### 推荐安装

| 包名 | 版本 | 说明 |
|------|------|------|
| Addressables | 1.21.0+ | Unity 资源管理系统（非强制要求，但推荐用于资源加载） |

### 自动安装依赖

安装本包时，以下依赖会自动安装：

| 包名 | 版本 | 说明 |
|------|------|------|
| com.unity.addressables | 1.21.0+ | 资源管理系统 |
| com.unity.burst | 1.8.0+ | 高性能编译 |
| com.unity.collections | 2.1.0+ | 高性能集合 |
| com.unity.mathematics | 1.2.0+ | 数学库 |

---

## 架构更新说明

### Reactive 架构（原 MVVM 架构升级）

从 v1.0.0 开始，框架将原 MVVM 架构升级为更灵活的 **Reactive 响应式架构**。这一变更带来了更好的性能、更清晰的代码结构和更强大的功能。

#### 变更详情

**1. 命名空间调整**
- 原命名空间：`Asaki.Core.MVVM`
- 新命名空间：`Asaki.Core.Reactive`

**2. 核心组件升级**

| 原组件 | 新组件 | 说明 |
|--------|--------|------|
| `AsakiProperty<T>` | `AsakiProperty<T>` | 保留类名，内部实现优化 |
| `IAsakiObserver<T>` | `IAsakiObserver<T>` | 观察者接口，支持双向绑定 |
| `AsakiBindTracker` | `AsakiBindTracker` | 绑定追踪器，增强生命周期管理 |

**3. 实现方式改进**

**Reactive 属性系统** (`AsakiProperty<T>`):
```csharp
// 创建可观察属性
var health = new AsakiProperty<int>(100);

// 订阅值变化（支持 MonoBehaviour 生命周期自动管理）
health.Subscribe(this, value => healthBar.value = value);

// 使用接口绑定
public class HealthObserver : IAsakiObserver<int>
{
    public void OnValueChange(int value) 
    {
        Debug.Log($"Health: {value}");
    }
}
health.Bind(new HealthObserver());
```

**特性绑定** (`[AsakiBind]`):
```csharp
public class PlayerViewModel
{
    [AsakiBind] public AsakiProperty<int> Score { get; } = new(0);
    [AsakiBind] public AsakiProperty<string> PlayerName { get; } = new("Player");
}
```

**UI 观察者组件**:
框架提供了一系列 UI 观察者组件，自动将 Reactive 属性绑定到 UI 元素：
- `AsakiTMPTextFloatObserver` - 绑定到 TMP Text (float)
- `AsakiTMPTextIntObserver` - 绑定到 TMP Text (int)
- `AsakiSliderObserver` - 绑定到 Slider
- `AsakiToggleObserver` - 绑定到 Toggle
- `AsakiInputFieldObserver` - 绑定到 InputField
- `AsakiActiveObserver` - 绑定到 GameObject 激活状态

**4. 影响范围**

- **代码兼容性**: 现有使用 `AsakiProperty<T>` 的代码基本无需修改
- **命名空间**: 需要更新 using 语句从 `Asaki.Core.MVVM` 改为 `Asaki.Core.Reactive`
- **功能增强**: 
  - 更高效的内存管理
  - 支持 MonoBehaviour 自动生命周期管理
  - 增强的线程安全性
  - 更丰富的运算符重载支持

---

## 功能清单

### 核心架构 (Asaki.Core)

#### 依赖注入系统
- ✅ 基于特性的服务注册 (`[AsakiModule]`)
- ✅ 构造函数注入和字段注入 (`[AsakiInject]`)
- ✅ 生命周期管理 (Singleton/Transient/Scoped)
- ✅ 自动服务发现

#### 命令模式
- ✅ 命令基类 (`AsakiCommand`)
- ✅ 撤销/重做支持
- ✅ 命令池管理 (`AsakiCommandPoolManager`)
- ✅ 异步命令支持

#### 查询模式
- ✅ 类型安全查询 (`AsakiQuery<T>`)
- ✅ 查询缓存 (`QueryCacheManager`)
- ✅ 查询池化 (`QueryPoolManager`)

#### 事件总线
- ✅ Broker 模式实现 (`AsakiBroker`)
- ✅ 类型安全事件 (`IAsakiEvent`)
- ✅ 自动订阅/取消订阅
- ✅ 事件调试工具

#### Reactive 响应式架构
- ✅ 响应式属性 (`AsakiProperty<T>`)
- ✅ 自动绑定特性 (`[AsakiBind]`)
- ✅ 绑定追踪器 (`AsakiBindTracker`)
- ✅ UI 观察者组件系统
- ✅ MonoBehaviour 生命周期自动管理

#### 状态机
- ✅ 分层状态机 (`AsakiStateMachine`)
- ✅ 状态过渡管理
- ✅ 状态事件回调

#### 黑板系统
- ✅ 运行时数据存储 (`AsakiBlackboard`)
- ✅ 类型桥接 (`AsakiTypeBridge`)
- ✅ 变量约束系统

### 服务系统 (Asaki.Unity)

#### 音频服务
- ✅ 分层音频管理 (BGM/SFX/Voice)
- ✅ 音频状态机
- ✅ 音量控制和混音
- ✅ 3D 音频支持

#### UI 服务
- ✅ 窗口管理系统
- ✅ UI 层级管理
- ✅ 窗口动画支持
- ✅ 资源句柄适配
- ✅ Reactive 属性绑定支持

#### 资源服务
- ✅ Addressables 集成（可选）
- ✅ 多种加载策略
- ✅ 依赖查找系统
- ✅ 资源预加载

#### 场景服务
- ✅ 异步场景加载
- ✅ 加载进度事件
- ✅ 场景过渡效果
- ✅ 加载模式支持

#### 对象池服务
- ✅ 高性能对象池
- ✅ 多种工厂模式
- ✅ 池统计信息
- ✅ 自动扩容

#### 存档服务
- ✅ JSON 序列化
- ✅ 二进制序列化
- ✅ 多存档槽支持
- ✅ 存档元数据

#### 网络服务
- ✅ HTTP 请求管理
- ✅ 下载服务
- ✅ 请求拦截器
- ✅ 超时和重试机制

#### 计时器服务
- ✅ 计时器管理
- ✅ 调度系统
- ✅ 暂停/恢复支持

### 编辑器工具 (Asaki.Editor)

#### 配置工具
- ✅ 配置烘焙器 (CSV/JSON → Binary)
- ✅ 配置仪表板
- ✅ 配置调试器

#### 调试工具
- ✅ 上下文调试器
- ✅ 事件调试器
- ✅ 日志仪表板
- ✅ 资源调试器
- ✅ 存档检查器

#### 图编辑器
- ✅ 可视化节点编辑器
- ✅ 黑板编辑器
- ✅ 模块图编辑器

#### 资源工具
- ✅ 批量重命名
- ✅ 重复资源查找
- ✅ 资源替换工具
- ✅ 资源浏览器

#### UI 工具
- ✅ UI 生成器
- ✅ UI 代码生成
- ✅ 预制体生成

### 插件集成 (Asaki.Plugins)

- ✅ 本地化系统
- ✅ 本地化编辑器

---

## 安装指南

### 前置要求

在安装 Asaki Framework 之前，请确保已安装以下依赖：

1. **UniTask** (必须)
   - 通过 Package Manager 添加：`https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
   
2. **TextMeshPro** (必须)
   - 通常已包含在 Unity 2022.3+ 中
   - 如未安装，通过 Package Manager 安装 TextMeshPro

3. **Addressables** (推荐)
   - 通过 Package Manager 安装 Addressables 包
   - 用于资源管理系统（可选，但推荐）

### 方法 1: 通过 Package Manager (推荐)

1. 打开 Unity 编辑器
2. 进入 `Window > Package Manager`
3. 点击 `+` 按钮，选择 `Add package from git URL`
4. 输入: `https://github.com/aski0006/AsakiFramework.git?path=Assets/Asaki#v1.0.0`
5. 点击 `Add`

### 方法 2: 通过 manifest.json

在 `Packages/manifest.json` 中添加：

```json
{
  "dependencies": {
    "com.asaki.framework": "https://github.com/aski0006/AsakiFramework.git?path=Assets/Asaki#v1.0.0"
  }
}
```

---

## 快速开始

详细的使用文档、API 参考和示例代码请访问：

📚 **[DeepWiki 文档](https://deepwiki.com/aski0006/AsakiFramework)**

文档包含：
- 完整的功能介绍和架构说明
- 详细的使用教程和最佳实践
- API 参考手册
- 示例项目和代码片段
- 常见问题解答

---

## 注意事项

### 升级指南
- 从早期版本升级时，请备份项目
- 更新命名空间：`Asaki.Core.MVVM` → `Asaki.Core.Reactive`
- 检查 API 变更日志
- 重新生成代码（如果有使用代码生成器）

### 已知限制
- 部分编辑器工具需要 Unity 2022.3+ 的特定功能
- Burst 编译器在某些平台上可能需要额外配置
- 存档系统的二进制格式在不同版本间可能不兼容

### 性能建议
- 使用对象池管理频繁创建/销毁的对象
- 利用 Burst 编译器优化计算密集型代码
- 合理使用查询缓存避免重复计算
- 使用 UniTask 替代协程减少 GC

### 调试建议
- 使用框架提供的调试窗口监控运行时状态
- 开启详细日志级别进行问题排查
- 使用事件调试器追踪事件流

---

## 文件清单

```
Assets/Asaki/
├── package.json              # 包配置
├── package.json.meta         # 包配置 meta 文件
├── Documentation~/           # 详细文档（Unity 忽略此文件夹）
│   ├── README.md             # 说明文档
│   ├── CHANGELOG.md          # 变更日志
│   ├── LICENSE               # MIT 许可证
│   └── Architecture.md       # 架构指南
├── Core/                     # 核心架构
│   ├── Architecture/         # 命令/查询模式
│   ├── Attributes/           # 特性定义
│   ├── Blackboard/           # 黑板系统
│   ├── Broker/               # 事件总线
│   ├── Context/              # DI 容器
│   ├── FSM/                  # 状态机
│   ├── Graphs/               # 图系统
│   ├── Pooling/              # 对象池
│   ├── Reactive/             # Reactive 响应式架构
│   └── ...
├── Unity/                    # Unity 实现
│   ├── Bootstrapper/         # 引导程序
│   ├── Modules/              # 服务模块
│   ├── Services/             # 服务实现
│   └── ...
├── Editor/                   # 编辑器工具
│   ├── Debugging/            # 调试工具
│   ├── GraphEditors/         # 图编辑器
│   ├── Utilities/            # 工具集
│   └── ...
└── Plugins/                  # 插件集成
    └── Localization/         # 本地化
```

**注意**: `Documentation~` 文件夹名称以 `~` 结尾，Unity 会自动忽略此文件夹及其内容，不会导入到项目中。这样可以避免根目录文档文件的 meta 文件问题。

---

## 兼容性

### 支持的 Unity 版本
- ✅ Unity 2022.3 LTS
- ✅ Unity 2023.x
- ✅ Unity 6 (6000.x)

### 支持的平台
- ✅ Windows (Standalone)
- ✅ macOS (Standalone)
- ✅ Linux (Standalone)
- ✅ iOS
- ✅ Android
- ✅ WebGL (部分功能受限)

---

## 反馈和支持

- **GitHub Issues**: https://github.com/aski0006/AsakiFramework/issues
- **Email**: aski0006@gmail.com
- **文档**: 参见 `Documentation~` 目录

---

## 许可证

本项目采用 MIT 许可证开源。详见 [LICENSE](../Assets/Asaki/LICENSE) 文件。

---

## 致谢

感谢所有为 Asaki Framework 做出贡献的开发者！

---

**© 2026 Asaki. All rights reserved.**
