# Asaki Entities 编辑器工具

本目录包含 Asaki Framework 实体系统的一整套自定义编辑器工具，提供可视化管理和调试功能。

## 编辑器窗口

### 1. Entity World (实体世界)
**路径**: `Asaki/Entities/Entity World`

实体世界的总览窗口，显示所有实体及其状态：
- 查看所有实体的列表
- 显示实体ID、组件数量、激活状态
- 搜索和筛选（按组件类型、激活状态）
- 查看实体详细信息
- 实时统计（实体总数、激活数、组件总数）

### 2. Entity Debugger (实体调试器)
**路径**: `Asaki/Entities/Entity Debugger`

强大的实体调试和编辑工具：
- 实时查看和编辑实体数据
- 添加/移除组件
- 修改组件字段值（支持int/float/bool/string/Vector2/Vector3/enum）
- 创建新实体并选择组件
- 一键销毁实体
- 暂停自动刷新功能

### 3. Entity Templates (实体模板)
**路径**: `Asaki/Entities/Entity Templates`

可视化模板编辑器：
- 创建新的实体模板
- 配置模板组件和默认值
- 管理所有已注册的模板
- 测试创建模板实体（Play Mode）
- 删除模板

### 4. Component Graph (组件图)
**路径**: `Asaki/Entities/Component Graph`

可视化实体-组件关系图：
- 节点-边图显示实体和组件的关系
- 实体节点（蓝色）、组件节点（绿色）、标签节点（黄色）
- 支持缩放和拖拽
- 点击节点查看详细信息
- 实时更新（Play Mode）

### 5. Query Builder (查询构建器)
**路径**: `Asaki/Entities/Query Builder`

高级查询和批量操作工具：
- 可视化构建查询条件
- 支持多种条件类型：
  - HasComponent / NotHasComponent
  - IsActive / IsInactive
  - HasTag
- 批量操作查询结果：
  - 批量添加/移除组件
  - 批量激活/禁用
  - 批量销毁

## 使用示例

### 创建实体模板
```csharp
// 在代码中注册模板
EntityTemplateRegistry.Register("Enemy", new EntityTemplate()
    .With<HealthComponent>(h => {
        h.MaxHealth = 100;
        h.CurrentHealth = 100;
    })
    .WithTag<EnemyTag>()
    .With<AIComponent>());
```

### 使用实体构建器
```csharp
var player = world.Create()
    .With<HealthComponent>(h => {
        h.MaxHealth = 200;
        h.CurrentHealth = 200;
    })
    .WithTag<PlayerTag>()
    .With<LifecycleComponent>();
```

### 批量查询和操作
```csharp
// 批量修改生命值
world.BatchModify<HealthComponent>(health => {
    if (health.CurrentHealth < health.MaxHealth)
    {
        health.CurrentHealth = health.MaxHealth;
        return true;
    }
    return false;
});

// 批量销毁死亡实体
world.BatchDestroy(entity => {
    if (entity.TryGetComponent<HealthComponent>(out var health))
    {
        return health.CurrentHealth <= 0;
    }
    return false;
});
```

## 菜单结构

```
Asaki/Entities/
├── Entity World          # 实体世界查看器
├── Entity Debugger       # 实体调试器
├── Entity Templates      # 模板编辑器
├── Component Graph       # 组件关系图
├── Query Builder         # 查询构建器
└── Open Documentation    # 打开文档
```

## 技术说明

- 所有编辑器在 Play Mode 下自动刷新数据
- 使用反射获取 EntityWorld 内部数据
- 支持缩放、拖拽、筛选等交互
- 与现有 Entities API 完全兼容

## 注意事项

1. 大多数功能需要在 Play Mode 下使用
2. 实体修改会立即生效（慎用批量销毁）
3. 模板修改会立即保存到注册表
