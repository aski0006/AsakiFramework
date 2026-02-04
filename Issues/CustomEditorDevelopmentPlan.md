# Asaki Framework 自定义编辑器开发计划

## 项目概述

Asaki Framework 是一个高性能、模块化、可扩展的 Unity 游戏开发框架。目前已拥有 **38+ 个自定义编辑器**，覆盖了实体系统、图系统、调试工具、配置管理、模块管理等核心领域。

本文档分析现有编辑器状况，并规划未来需要开发的自定义编辑器。

---

## 一、现有自定义编辑器清单

### 1.1 实体系统编辑器 (Entity System)
位于 `Assets/Asaki/Editor/Entities/`

| 编辑器 | 类型 | 功能 | 菜单路径 |
|--------|------|------|----------|
| EntityWorldWindow | EditorWindow | 实体世界总览 | Asaki/Entities/Entity World |
| EntityDebuggerWindow | EditorWindow | 实体调试和编辑 | Asaki/Entities/Entity Debugger |
| EntityTemplateWindow | EditorWindow | 可视化模板编辑器 | Asaki/Entities/Entity Templates |
| EntityComponentGraphWindow | EditorWindow | 实体-组件关系图 | Asaki/Entities/Component Graph |
| EntityQueryWindow | EditorWindow | 高级查询工具 | Asaki/Entities/Query Builder |

**状态**: ✅ 完整

### 1.2 图编辑器系统 (Graph Editors)
位于 `Assets/Asaki/Editor/GraphEditors/`

| 编辑器 | 类型 | 功能 |
|--------|------|------|
| AsakiGraphWindow | EditorWindow | 通用图编辑器窗口 |
| AsakiGraphView | GraphView | 基于UI Toolkit的图视图 |
| AsakiNodeView | Node | 节点视图基类 |
| AsakiNodeSearchWindow | ScriptableObject | 节点搜索窗口 |
| AsakiBlackboardProvider | Provider | 黑板变量提供器 |
| GlobalBlackboardWindow | EditorWindow | 全局黑板变量管理 |
| AsakiGraphDebugger | Debugger | 图运行时调试器 |

**状态**: ✅ 完整，可扩展

### 1.3 调试窗口 (Debugging)
位于 `Assets/Asaki/Editor/Debugging/`

| 编辑器 | 类型 | 功能 | 菜单路径 |
|--------|------|------|----------|
| AsakiContextDebuggerWindow | EditorWindow | 服务容器调试器 | Asaki/Diagnostics/Context Debugger |
| AsakiPoolDebuggerWindow | EditorWindow | 对象池调试窗口 | Asaki/Diagnostics/Pool Debugger |
| AsakiEventDebuggerWindow | EditorWindow | 事件总线调试器 | Asaki/Diagnostics/Event Debugger |
| AsakiResDebuggerWindow | EditorWindow | 资源管理调试器 | Asaki/Diagnostics/Resources Debugger |
| AsakiSaveInspector | EditorWindow | 存档数据检查器 | Asaki/Diagnostics/Save Inspector |
| AsakiTimerDebuggerWindow | EditorWindow | 定时器调试窗口 | Asaki/Diagnostics/Timer Debugger |
| AsakiLogDashboard | EditorWindow | 日志仪表板 | Asaki/Window/Log Dashboard |

**状态**: ✅ 完整

### 1.4 配置系统编辑器 (Configuration)
位于 `Assets/Asaki/Editor/Configuration/`

| 编辑器 | 类型 | 功能 | 菜单路径 |
|--------|------|------|----------|
| AsakiConfigDashboardWindow | EditorWindow | CSV配置仪表板 | Asaki/Configuration/Config Dashboard |
| AsakiConfigDebugger | EditorWindow | 配置运行时编辑器 | Asaki/Configuration/Runtime Editor |
| AsakiConfigBaker | MenuItem | 配置烘焙工具 | Asaki/Configuration/Bake Configs |

**状态**: ✅ 完整

### 1.5 模块系统编辑器 (Module System)
位于 `Assets/Asaki/Editor/ModuleSystem/`

| 编辑器 | 类型 | 功能 | 菜单路径 |
|--------|------|------|----------|
| AsakiModuleDashboard | EditorWindow | 模块依赖管理仪表板 | Asaki/Window/Module Dashboard |

**状态**: ✅ 完整

### 1.6 UI系统编辑器 (UI System)
位于 `Assets/Asaki/Editor/UI/`

| 编辑器 | 类型 | 功能 | 菜单路径 |
|--------|------|------|----------|
| AsakiUIGeneratorWindow | EditorWindow | UI资源生成器 | Asaki/Window/UI Asset Generator |
| AsakiUIScriptGeneratorWindow | EditorWindow | UI脚本生成器 | Asaki/UI/Generate UI Script |
| AsakiUIScaffolder | EditorWindow | UI脚手架工具 | Asaki/UI/Scaffold UI |
| AsakiUIPanel | EditorWindow | UI布局面板 | Asaki/UI/UI Layout Panel |
| AsakiUIWindowEditor | CustomEditor | UI窗口自定义编辑器 | - |
| AsakiUITools | MenuItem | UI快捷工具 | Asaki/UI/Snap Anchors |

**状态**: ✅ 完整

### 1.7 Property Drawers
位于 `Assets/Asaki/Editor/PropertyDrawers/`

| 编辑器 | 类型 | 功能 |
|--------|------|------|
| AsakiInterfaceDrawer | PropertyDrawer | 接口类型选择器 |
| AsakiPropertyDrawer | PropertyDrawer | 响应式属性绘制器 |
| AsakiSceneServiceDrawer | PropertyDrawer | 场景服务引用绘制器 |
| AsakiServiceComponentEditor | Editor | 服务组件编辑器 |

**状态**: ✅ 完整

### 1.8 实用工具 (Utilities)
位于 `Assets/Asaki/Editor/Utilities/Tools/`

| 编辑器 | 类型 | 功能 | 菜单路径 |
|--------|------|------|----------|
| AssetExplorerWindow | EditorWindow | 资源浏览器 | Asaki/Tools/Asset Explorer |
| BatchRenameEditorWindow | EditorWindow | 批量重命名工具 | Asaki/Tools/Batch Rename |
| AsakiDuplicateFinderWindow | EditorWindow | 重复资源查找器 | Asaki/Tools/Duplicate Finder |
| AsakiGroundAlignerWindow | EditorWindow | 地面对齐工具 | Asaki/Tools/Ground Aligner |
| AsakiMissingFinderWindow | EditorWindow | 缺失引用查找器 | Asaki/Tools/Missing Finder |
| AsakiAssetReplacerWindow | EditorWindow | 资源替换工具 | Asaki/Tools/Asset Replacer |
| AsakiQuickLayoutWindow | EditorWindow | 快速布局工具 | Asaki/Tools/Quick Layout |
| AsakiFileTreeGenerator | EditorWindow | 文件树生成器 | Asaki/Tools/File Tree Generator |

**状态**: ✅ 完整

### 1.9 性能分析器 (Profilers)
位于 `Assets/Asaki/Editor/Profiler/`

| 编辑器 | 类型 | 功能 | 菜单路径 |
|--------|------|------|----------|
| BlackboardProfilerWindow | EditorWindow | 黑板性能分析器 | Asaki/Diagnostics/Blackboard Profiler |
| AsakiTypeBridgeValidator | EditorWindow | 类型桥接验证器 | Asaki/Diagnostics/Validate Type Registry |

**状态**: ✅ 完整

### 1.10 其他编辑器

| 编辑器 | 类型 | 功能 | 菜单路径 |
|--------|------|------|----------|
| AsakiAudioDashboard | EditorWindow | 音频管理仪表板 | Asaki/Window/Audio Dashboard |
| AsakiSceneContextEditor | CustomEditor | 场景上下文自定义编辑器 | - |

**状态**: ✅ 完整

---

## 二、需要开发的自定义编辑器

### 2.1 高优先级 (High Priority)

#### 1. Command Debugger Window - 命令调试器
**功能描述**: 调试 CQRS 架构中的 Command 执行流程

**核心功能**:
- 实时显示命令执行历史
- 查看命令执行时间轴
- 撤销/重做栈可视化
- 命令参数和结果查看
- 支持命令断点调试

**技术要点**:
```csharp
// 路径: Assets/Asaki/Editor/Debugging/AsakiCommandDebuggerWindow.cs
// 菜单: Asaki/Diagnostics/Command Debugger
// 依赖: Core/Architecture/Commands/
```

**开发估算**: 3-4 天

---

#### 2. MVVM Binding Debugger - 绑定调试器
**功能描述**: 调试 UI 系统的 MVVM 数据绑定

**核心功能**:
- 查看所有活跃的绑定关系
- 监控绑定更新频率
- 检测绑定性能瓶颈
- 显示 ViewModel 属性变化
- 绑定错误诊断

**技术要点**:
```csharp
// 路径: Assets/Asaki/Editor/UI/AsakiBindingDebuggerWindow.cs
// 菜单: Asaki/UI/Binding Debugger
// 依赖: Unity/Services/UI/MVVM/
```

**开发估算**: 2-3 天

---

#### 3. FSM State Machine Editor - 状态机编辑器
**功能描述**: 可视化有限状态机编辑

**核心功能**:
- 可视化状态图编辑
- 状态转换条件配置
- 子状态机支持
- 运行时状态监控
- 代码生成

**技术要点**:
```csharp
// 路径: Assets/Asaki/Editor/FSM/
// 菜单: Asaki/FSM/State Machine Editor
// 依赖: Core/FSM/ (需新建)
```

**开发估算**: 5-7 天

---

### 2.2 中优先级 (Medium Priority)

#### 4. DI Graph Visualizer - 依赖注入可视化
**功能描述**: 可视化服务依赖关系图

**核心功能**:
- 显示服务依赖关系图
- 检测循环依赖
- 服务生命周期可视化
- 依赖注入路径追踪

**技术要点**:
```csharp
// 路径: Assets/Asaki/Editor/Diagnostics/AsakiDIGraphWindow.cs
// 菜单: Asaki/Diagnostics/DI Graph Visualizer
// 依赖: Core/Context/
```

**开发估算**: 2-3 天

---

#### 5. Network Monitor Window - 网络监控器
**功能描述**: 监控网络请求和响应

**核心功能**:
- HTTP 请求/响应列表
- 请求耗时分析
- 请求重放功能
- 网络错误统计
- API 调用频率监控

**技术要点**:
```csharp
// 路径: Assets/Asaki/Editor/Debugging/AsakiNetworkMonitorWindow.cs
// 菜单: Asaki/Diagnostics/Network Monitor
// 依赖: Unity/Services/Web/
```

**开发估算**: 3-4 天

---

#### 6. Migration Manager Window - 迁移脚本管理器
**功能描述**: 管理存档数据迁移脚本

**核心功能**:
- 迁移脚本列表
- 版本历史查看
- 迁移测试工具
- 批量迁移执行
- 迁移日志查看

**技术要点**:
```csharp
// 路径: Assets/Asaki/Editor/Configuration/AsakiMigrationManagerWindow.cs
// 菜单: Asaki/Configuration/Migration Manager
// 依赖: Core/Serialization/Migration/
```

**开发估算**: 2-3 天

---

#### 7. Query Profiler Window - 查询分析器
**功能描述**: 分析实体查询性能

**核心功能**:
- 查询执行时间统计
- 缓存命中率监控
- 查询优化建议
- 慢查询检测

**技术要点**:
```csharp
// 路径: Assets/Asaki/Editor/Entities/EntityQueryProfilerWindow.cs
// 菜单: Asaki/Entities/Query Profiler
// 依赖: Core/Architecture/Entities/
```

**开发估算**: 2-3 天

---

### 2.3 低优先级 (Low Priority)

#### 8. Audio State Visualizer - 音频状态可视化
**功能描述**: 增强版音频监控

**核心功能**:
- 实时音频频谱显示
- 混音器状态可视化
- 音频源位置3D显示
- 音频事件时间轴

**技术要点**:
```csharp
// 路径: Assets/Asaki/Editor/Utilities/Tools/AsakiAudioVisualizerWindow.cs
// 菜单: Asaki/Window/Audio Visualizer
// 依赖: Unity/Services/Audio/
```

**开发估算**: 3-4 天

---

#### 9. Scene Transition Editor - 场景过渡编辑器
**功能描述**: 可视化场景过渡配置

**核心功能**:
- 场景过渡流程图
- 过渡效果预览
- 加载进度配置
- 过渡动画编辑

**技术要点**:
```csharp
// 路径: Assets/Asaki/Editor/SceneManagement/AsakiSceneTransitionEditor.cs
// 菜单: Asaki/Scene/Transition Editor
// 依赖: Unity/Services/SceneManagement/
```

**开发估算**: 2-3 天

---

#### 10. Unified Profiler Window - 统一性能分析器
**功能描述**: 整合所有性能数据

**核心功能**:
- 实体系统性能
- 对象池性能
- 事件总线性能
- 内存使用统计
- CPU/GPU 时间线

**技术要点**:
```csharp
// 路径: Assets/Asaki/Editor/Profiler/AsakiUnifiedProfilerWindow.cs
// 菜单: Asaki/Diagnostics/Unified Profiler
// 依赖: 所有核心系统
```

**开发估算**: 4-5 天

---

#### 11. Entity Relationship Graph - 实体关系图
**功能描述**: 显示实体间引用关系

**核心功能**:
- 实体引用关系可视化
- 父子关系显示
- 组件共享关系
- 引用循环检测

**技术要点**:
```csharp
// 路径: Assets/Asaki/Editor/Entities/EntityRelationshipGraphWindow.cs
// 菜单: Asaki/Entities/Relationship Graph
// 依赖: Core/Architecture/Entities/
```

**开发估算**: 2-3 天

---

#### 12. Config Diff Tool - 配置对比工具
**功能描述**: 对比不同版本的配置文件

**核心功能**:
- 多配置文件对比
- 差异高亮显示
- 合并冲突解决
- 版本历史对比

**技术要点**:
```csharp
// 路径: Assets/Asaki/Editor/Configuration/AsakiConfigDiffWindow.cs
// 菜单: Asaki/Configuration/Config Diff
// 依赖: Core/Configuration/
```

**开发估算**: 2-3 天

---

## 三、开发计划时间表

### 第一阶段 (第1-2周)
- [ ] Command Debugger Window
- [ ] MVVM Binding Debugger

### 第二阶段 (第3-4周)
- [ ] FSM State Machine Editor
- [ ] DI Graph Visualizer

### 第三阶段 (第5-6周)
- [ ] Network Monitor Window
- [ ] Migration Manager Window
- [ ] Query Profiler Window

### 第四阶段 (第7-8周)
- [ ] Audio State Visualizer
- [ ] Scene Transition Editor
- [ ] Unified Profiler Window

### 第五阶段 (第9-10周)
- [ ] Entity Relationship Graph
- [ ] Config Diff Tool
- [ ] 现有编辑器优化

---

## 四、技术规范

### 4.1 命名规范
- EditorWindow: `Asaki[功能]Window.cs`
- CustomEditor: `Asaki[类型]Editor.cs`
- PropertyDrawer: `Asaki[类型]Drawer.cs`

### 4.2 菜单路径规范
```
Asaki/
├── Entities/          # 实体系统
├── Graph/             # 图编辑器
├── Diagnostics/       # 调试工具
├── Configuration/     # 配置系统
├── UI/                # UI系统
├── Scene/             # 场景管理
├── FSM/               # 状态机
├── Window/            # 通用窗口
└── Tools/             # 实用工具
```

### 4.3 UI 风格规范
- 使用 UI Toolkit (UXML/USS)
- 遵循 Unity Editor 风格
- 支持暗黑/明亮主题
- 响应式布局

---

## 五、验收标准

每个编辑器开发完成后需要满足：

1. **功能完整性**: 实现所有规划功能
2. **代码质量**: 通过代码审查，符合项目规范
3. **性能要求**: 不影响编辑器性能
4. **文档**: 包含使用说明和 API 文档
5. **测试**: 关键功能有单元测试

---

## 六、附录

### 6.1 现有编辑器文件清单

```
Assets/Asaki/Editor/
├── Entities/
│   ├── EntityWorldWindow.cs
│   ├── EntityDebuggerWindow.cs
│   ├── EntityTemplateWindow.cs
│   ├── EntityComponentGraphWindow.cs
│   ├── EntityQueryWindow.cs
│   └── EntityMenu.cs
├── GraphEditors/
│   ├── AsakiGraphWindow.cs
│   ├── AsakiGraphView.cs
│   ├── AsakiNodeView.cs
│   ├── AsakiNodeSearchWindow.cs
│   ├── AsakiBlackboardProvider.cs
│   ├── GlobalBlackboardWindow.cs
│   └── AsakiGraphDebugger.cs
├── Debugging/
│   ├── AsakiContextDebuggerWindow.cs
│   ├── AsakiPoolDebuggerWindow.cs
│   ├── AsakiEventDebuggerWindow.cs
│   ├── AsakiResDebuggerWindow.cs
│   ├── AsakiSaveInspector.cs
│   ├── AsakiTimerDebuggerWindow.cs
│   ├── AsakiLogDashboard.cs
│   └── ALogUnityBridgeToggle.cs
├── Configuration/
│   ├── AsakiConfigDashboardWindow.cs
│   ├── AsakiConfigDebugger.cs
│   └── AsakiConfigBaker.cs
├── ModuleSystem/
│   ├── AsakiModuleDashboard.cs
│   ├── Graph/
│   │   ├── AsakiModuleGraphBuilder.cs
│   │   ├── AsakiModuleGraphController.cs
│   │   └── AsakiModuleNode.cs
├── UI/
│   ├── AsakiUIGeneratorWindow.cs
│   ├── AsakiUIScriptGeneratorWindow.cs
│   ├── AsakiUIScaffolder.cs
│   ├── AsakiUIPanel.cs
│   ├── AsakiUIWindowEditor.cs
│   └── AsakiUITools.cs
├── PropertyDrawers/
│   ├── AsakiInterfaceDrawer.cs
│   ├── AsakiPropertyDrawer.cs
│   ├── AsakiSceneServiceDrawer.cs
│   └── AsakiServiceComponentEditor.cs
├── Utilities/Tools/
│   ├── AssetExplorerWindow.cs
│   ├── BatchRenameEditorWindow.cs
│   ├── AsakiDuplicateFinderWindow.cs
│   ├── AsakiGroundAlignerWindow.cs
│   ├── AsakiMissingFinderWindow.cs
│   ├── AsakiAssetReplacerWindow.cs
│   ├── AsakiQuickLayoutWindow.cs
│   ├── AsakiFileTreeGenerator.cs
│   ├── AsakiScriptAggregation.cs
│   ├── AsakiSceneContextCreator.cs
│   └── AssetsExplore/
├── Profiler/
│   └── BlackboardProfilerWindow.cs
├── Diagnostics/
│   └── AsakiTypeBridgeValidator.cs
├── Context/
│   └── AsakiSceneContextEditor.cs
└── AsakiAudioDashboard.cs
```

### 6.2 参考资源

- [Unity Editor Scripting Guide](https://docs.unity3d.com/Manual/EditorScripting.html)
- [UI Toolkit Documentation](https://docs.unity3d.com/Manual/UIElements.html)
- [GraphView API](https://docs.unity3d.com/ScriptReference/Experimental.GraphView.GraphView.html)

---

*文档版本: 1.0*
*创建日期: 2026-02-04*
*作者: Asaki Framework Team*
