# Issue #4: [Feature] Runtime Developer Console: Command Binding & Reactive Watcher (运行时控制台：指令绑定与响应式监视)

**状态**: OPEN  
**作者**: aski0006 (Asaki0019)  
**创建时间**: 2026-01-31  
**更新时间**: 2026-01-31  
**URL**: https://github.com/aski0006/AsakiFramework/issues/4

---

## 1. 背景与痛点 (Problem Statement)

目前框架在 **Editor 环境**  下的工具链非常完善（Graph, Inspectors），但在 **真机运行环境 (Runtime)** 下缺乏 有效的调试手段。

* **调试困难**：在移动端或打包版本中，无法直接查看日志、修改数值（如测试"无限金币"或"无敌模式"）或执行特定逻辑。
* **状态盲盒**：无法实时监控核心数据（Model 中的 `AsakiProperty`）的变化曲线，只能靠打 Log。
* **QA 效率** ：缺乏一个统一的入口让 QA 或策划动态调整游戏参数。

**相关代码引用：**
* 指令基类：`Assets/Asaki/Core/Architecture/Command/AsakiCommand.cs`
* 响应式属性：`Assets/Asaki/Core/Reactive/AsakiProperty.cs`
* 日志系统：`Assets/Asaki/Core/Logging/ALog.cs`

---

## 2. 解决方案提案 (Proposed Solution)

构建一个类似于 Quake/Unreal 风格的 **运行时控制台 (In-Game Console)**，深度集成 Asaki 的架构特性。

**核心功能点：**

### 2.1 指令注册与反射执行 (Command Registration)
* 引入 `[AsakiConsoleCommand("cmd_name", Description = "...")]` 特性。
* 支持自动扫描并注册静态方法。
* **架构集成**：支持通过字符串直接查找并执行 `AsakiCommand`，例如输入 `exec KillAllEnemiesCommand`。

### 2.2 参数自动解析 (Parameter Parsing)
* 实现一个简单的词法分析器，支持将控制台输入的字符串 `cmd_add_item 1001 5` 自动转换为 `(int, int)` 并通过反射调用对应方法。

### 2.3 响应式 数据监视 (Reactive Watcher)
* 实现 `watch` 指令。
* 利用 `AsakiProperty<T>` 的 `Subscribe` 机制。
* 输入 `watch PlayerModel.HP`，当 HP 变化时，控制台实时打印 `[Watch] PlayerModel.HP changed to 90`。

### 2.4 日志重定向 (Log Overlay)
* 接管 `ALog` 的输出流，将其渲染在控制台 UI 的 ScrollView 中。
* 支持按 LogLevel (Info/Warning/Error) 进行颜色高亮和过滤。

---

## 3. 预期 收益 (Benefits)

* **调试效率**：在真机上即可完成数值验证和逻辑触发，无需重新打 包。
* **架构复用**：直接复用现有的 Command 和 Property 系统，体现框架的一致性 。
* **扩展性**：开发者可以轻松为自己的游戏逻辑添加作弊码 (Cheats)。

---

## 4. 下一步计划 (Next Steps)

- [ ] 定义 `AsakiConsoleCommandAttribute`。
- [ ] 创建 `ConsoleService` (实现 `IAsakiModule`) 用于管理指令注册。
- [ ] 开发基于 UGUI 的控制台界面 (利用现有的 UI 框架生成)。
- [ ] 实现基础的参数解析器 (String Arg Parser)。
- [ ] 对接 `ALog` 实现日志回显。
