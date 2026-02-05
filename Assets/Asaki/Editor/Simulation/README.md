# Asaki Simulation Debugger

Asaki框架Simulation模块的自定义编辑器窗口，用于可视化管理和调试所有已注册的Tick对象。

## 功能特性

### 1. 整体布局

编辑器采用上下分部布局：
- **上部容器**：包含工具栏和搜索栏
- **下部容器**：左右可变宽度布局
  - **左侧**：显示已注册的Tick对象列表
  - **右侧**：展示当前选中Tick对象的详细信息

### 2. 核心功能

#### Tick对象管理
- 实时显示所有已注册的Tick对象（Tick/FixedTick/LateTick）
- 支持三种Tick类型的可视化区分（不同颜色标识）
- 显示每个对象的优先级信息

#### 搜索与过滤
- 支持按名称、类型名搜索Tick对象
- 支持按Tick类型过滤（All/Tick/FixedTick/LateTick）
- 实时搜索，即时更新结果

#### 排序功能
- 支持按优先级排序
- 支持按名称排序
- 支持按类型排序
- 支持升序/降序切换

#### 动态布局
- 左右面板宽度可拖动调整
- 支持最小/最大宽度限制

#### 详细信息展示
- 显示对象名称和完整类型名
- 显示Unity对象信息（GameObject/Component）
- 显示实现的接口列表
- 支持在层级窗口中定位对象
- 支持复制类型名到剪贴板

### 3. 自动刷新

- 支持自动刷新模式（默认开启）
- 可手动刷新数据
- 实时反映Tick对象的注册/注销变化

## 使用方法

### 打开编辑器

通过菜单栏打开：
```
Asaki → Diagnostics → Simulation Debugger
```

### 界面说明

1. **工具栏**
   - Refresh：手动刷新数据
   - Auto：自动刷新开关
   - 搜索框：输入关键字搜索Tick对象
   - Filter：按类型过滤
   - Sort：选择排序方式
   - ▲/▼：切换排序方向
   - 右上角显示各类Tick对象数量统计

2. **左侧列表**
   - 显示所有Tick对象的摘要信息
   - 颜色标识：青色(Tick)、橙色(FixedTick)、绿色(LateTick)
   - 点击选中对象查看详情
   - 灰色背景表示对象已被销毁

3. **右侧面板**
   - 显示选中对象的详细信息
   - 包含对象状态、类型信息、Unity对象详情
   - 提供操作按钮（选择对象、复制类型名）

## 代码规范

### UNITY_EDITOR 宏保护

所有编辑器相关代码均使用`#if UNITY_EDITOR`宏进行条件编译保护：

```csharp
#if UNITY_EDITOR
// 编辑器代码
#endif
```

这确保在Release构建时，所有编辑器相关代码会被自动剥离。

### 运行时检测

编辑器访问接口在Simulation服务中也使用UNITY_EDITOR宏保护：

```csharp
#if UNITY_EDITOR
public ReadOnlyCollection<TickableWrapper> GetTickables() => _tickables.AsReadOnly();
#endif
```

## API说明

### AsakiSimulationService 编辑器接口

```csharp
#if UNITY_EDITOR
// 获取所有Tick对象（只读）
ReadOnlyCollection<TickableWrapper> GetTickables()

// 获取所有FixedTick对象（只读）
ReadOnlyCollection<IAsakiFixedTickable> GetFixedTickables()

// 获取所有LateTick对象（只读）
ReadOnlyCollection<LateTickableWrapper> GetLateTickables()

// 获取Tick对象总数
int GetTotalTickableCount()

// 获取Tick对象统计信息
(int tickCount, int fixedTickCount, int lateTickCount) GetTickableStats()
#endif
```

## 注意事项

1. **运行时依赖**：编辑器需要Simulation服务已注册到AsakiContext中才能正常工作
2. **性能考虑**：编辑器使用0.2秒的刷新间隔，避免频繁刷新影响性能
3. **空对象处理**：已销毁的对象会显示为灰色，但仍保留在列表中直到下次刷新
4. **线程安全**：编辑器访问接口返回只读集合，确保线程安全

## 扩展建议

如需扩展编辑器功能，可以考虑：

1. 添加Tick对象的启用/禁用控制
2. 实现优先级动态调整
3. 添加性能分析数据（执行时间统计）
4. 支持导出Tick对象列表
5. 添加更多筛选条件（按优先级范围、按Unity对象状态等）
