# 实体系统单元测试

本目录包含 Asaki 实体系统的完整单元测试，基于 Unity Test Framework (NUnit)。

## 测试文件列表

| 文件 | 说明 | 测试类别 |
|------|------|----------|
| `MagicContainerTests.cs` | 魔法容器核心功能测试 | Unit |
| `MagicContainerPerformanceTests.cs` | 魔法容器性能对比测试 | Performance |
| `EntityIdTests.cs` | 实体标识符测试 | Unit |
| `EntityTests.cs` | 实体组件管理测试 | Unit |
| `EntityWorldTests.cs` | 实体世界管理测试 | Unit |
| `EntityModelTests.cs` | 实体模型（Architecture集成）测试 | Unit |
| `EntityCommandsTests.cs` | 实体相关Command测试 | Unit |

## 运行测试

### 在 Unity Editor 中运行

1. 打开 **Window > General > Test Runner**
2. 选择 **Edit Mode** 标签页
3. 展开 **Asaki.Tests.Entities** 命名空间
4. 点击 **Run All** 或选择特定测试运行

### 命令行运行

```bash
# 运行所有 Edit Mode 测试
Unity.exe -runTests -projectPath "PATH_TO_PROJECT" -testResults "results.xml" -testPlatform editmode

# 仅运行实体系统测试
Unity.exe -runTests -projectPath "PATH_TO_PROJECT" -testCategory "Entities" -testResults "results.xml" -testPlatform editmode

# 运行性能测试
Unity.exe -runTests -projectPath "PATH_TO_PROJECT" -testCategory "Performance" -testResults "results.xml" -testPlatform editmode
```

## 测试类别

### [Category("Unit")]

单元测试，验证各组件的基本功能：
- 添加/获取/移除组件
- 实体生命周期管理
- Command 执行与撤销

### [Category("Performance")]

性能测试，对比 MagicContainer 与 Dictionary：
- 添加性能
- 遍历性能（连续内存 vs 随机访问）
- 随机访问性能
- 删除性能

## 测试覆盖

### MagicContainer

- ✅ 基本操作：Add, Get, Remove
- ✅ 句柄复用
- ✅ 批量操作：ForEach, Find, FindAll, Exists
- ✅ 边界条件：无效句柄、空容器
- ✅ 枚举支持
- ✅ 性能对比

### EntityId

- ✅ 构造与属性
- ✅ IsValid 验证
- ✅ 相等性比较（== 和 Equals）
- ✅ 哈希码生成
- ✅ ToString 格式化
- ✅ 作为字典键使用

### Entity

- ✅ 基本属性（Id, World, IsActive, ComponentCount）
- ✅ 组件添加（泛型 + 实例）
- ✅ 组件获取与 TryGet
- ✅ 组件移除
- ✅ 组件存在性检查（HasComponent）
- ✅ 生命周期回调（OnAttach, OnDetach, OnEnable, OnDisable）
- ✅ 实体激活/禁用状态切换
- ✅ 获取所有组件
- ✅ 释放处理

### EntityWorld

- ✅ 实体创建
- ✅ 实体销毁
- ✅ 实体检索（Get/TryGet）
- ✅ 获取所有实体
- ✅ 组件查询（Query 1/2/3 个组件类型）
- ✅ 批量遍历（ForEach）
- ✅ 事件通知（OnEntityCreated, OnEntityDestroyed）
- ✅ 代际验证（防止 ABA 问题）

### EntityModel

- ✅ 创建与初始化
- ✅ World 访问
- ✅ 释放处理
- ✅ IAsakiModel 接口实现

### Commands

- ✅ CreateEntityCommand
- ✅ DestroyEntityCommand
- ✅ AddComponentCommand（支持 Undo）
- ✅ RemoveComponentCommand（支持 Undo）
- ✅ CanUndo 属性

## 编写新测试

遵循以下规范：

```csharp
[TestFixture]
[Category("Unit")]
public class MyEntityTests
{
    [SetUp]
    public void Setup()
    {
        // 初始化测试环境
    }

    [TearDown]
    public void Teardown()
    {
        // 清理资源
    }

    [Test]
    [Category("Unit")]
    public void MethodName_Scenario_ExpectedResult()
    {
        // Arrange
        // 准备测试数据

        // Act
        // 执行被测方法

        // Assert
        // 验证结果
    }
}
```

### 命名规范

- 测试类名：`{被测类名}Tests`
- 测试方法：`{被测方法}_{场景}_{预期结果}`
- 例如：`AddComponent_WhenEntityIsActive_CallsOnEnable`

### 断言规范

```csharp
// 基本断言
Assert.AreEqual(expected, actual);
Assert.IsTrue(condition);
Assert.IsNull(obj);

// 异常断言
Assert.Throws<InvalidOperationException>(() => method());
Assert.DoesNotThrow(() => method());

// 集合断言
Assert.IsEmpty(collection);
Assert.Contains(expectedItem, collection);
```

## 调试技巧

1. **使用 Debug.Log**：在测试中输出调试信息
2. **设置断点**：在 Visual Studio 中附加调试器
3. **Test Runner 调试**：右键测试选择 "Debug Selected"
4. **性能分析**：使用 `[Category("Performance")]` 标记性能测试

## 常见问题

### 测试无法发现

- 确保测试类标记了 `[TestFixture]`
- 确保测试方法标记了 `[Test]` 或 `[UnityTest]`
- 检查文件是否在 `Assets/Tests` 目录下
- 确认 Assembly Definition 配置正确

### 测试相互影响

- 使用 `[SetUp]` 和 `[TearDown]` 确保每个测试独立
- 避免使用静态状态
- 在 Teardown 中清理所有创建的资源

### 性能测试不稳定

- 使用 `[Repeat]` 运行多次取平均
- 排除 JIT 编译影响（先运行一次预热）
- 关闭其他应用程序减少干扰
