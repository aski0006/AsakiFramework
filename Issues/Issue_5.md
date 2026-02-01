# Issue #5: [Feature] Reactive & Strongly-Typed I18n: Zero-String & Hot-Switching (响应式强类型国际化：去字符串化与无缝切换)

**状态**: OPEN  
**作者**: aski0006 (Asaki0019)  
**创建时间**: 2026-01-31  
**更新时间**: 2026-01-31  
**URL**: https://github.com/aski0006/AsakiFramework/issues/5

---

## 1. 背景与痛点 (Problem Statement)

目前的 `AsakiLocalizationService` 是基于传统的"字典查找"模式实现的。虽然借助 `ConfigService` 实现了数据加载 ，但在使用层面上存在明显的"体验断层"，与框架整体的响应式（Reactive）和强类型（Type-Safe）设计理念不符。

* **弱类型风险 (Magic String)**：代码中充斥着 `GetText("KEY_MENU_TITLE")`。Key 是硬编码的字符串，一旦配置表修改了 Key，编译期不会报错，运行时才会发现文本丢失。
* **非响应式 (Non-Reactive)**：切换语言 (`SwitchLanguage`) 是一个"重操作"。UI 组件通常需要监听事件并手动重新调用 `GetText`，或者需要销毁重刷整个界面。这导致语言切换不够流畅，且增加了 UI 层的胶水代码。

**相关代码引用：**
* 当前实现：`Assets/Asaki/Plugins/Localization/AsakiLocalizationService.cs`
* 数据定义：`Assets/Asaki/Plugins/Localization/LocalizationTable.cs`

---

## 2. 解决方案提案 (Proposed Solution)

利用 **Roslyn Source Generator** 和 **AsakiReactive** 重构国际化模块，实现"编译时生成 Key"和"运行时数据流绑定"。

**核心功能点：**

### 2.1 强类型 Key 生成 (Roslyn Key Generator)
* 编写 Source Generator 读取 `LocalizationTable.csv`。
* 自动生成静态类或常量结构：
    ```csharp
    public static class L10n {
        public static class Menu {
            public const int Title = 1001;
            public const int StartBtn = 1002;
        }
    }
    ```
* **收益**：开发者使用 `L10n.Menu.Title`。如果配置表删除了该 Key，代码直接**编译 报错**，彻底消灭拼写错误。

### 2.2 响应式绑定 (Reactive Binding)
* 修改 接口，使其返回 `IAsakiProperty<string>` 而非 `string`。
    ```csharp
    // 接口变化
    public IAsakiProperty<string> GetStream(int keyId);
    ```
* **内部实现**：Service 内部维护一个 `ReactiveProperty` 字典。当 `SwitchLanguage` 发生时，Service 批量更新所有 Property 的 Value。
* **UI 绑定**：
    ```csharp
    // 结合 UI 绑定系统
    _txtTitle.Bind(Localization.GetStream(L10n.Menu.Title));
    ```

### 2.3 去字符串化 (Zero String Alloc in Logic)
* 业务逻辑中只传递 `int` 类型的 ID，只在最终 UI 渲染层才通过 Property 获取 `string`，减少中间环节的 GC。

---

## 3. 预期收益 (Benefits)

* **极致的开发体验**：像写代码一样使用多语言，智能感知（IntelliSense）自动补全 Key。
* **无缝热切换**：切换语言时，全游戏所有绑定的 UI 文本**瞬间自动刷新**，无需编写任何 `OnLanguageChanged` 回调代码。
* **一致性**：将多语言模块提升 至与 Asaki 核心架构一致的工业级标准。

---

## 4. 下一步计划 (Next Steps)

- [ ] 编写 `AsakiLocalizationGenerator`，解析 CSV 生成 Key 代码。
- [ ] 重构 `AsakiLocalizationService`，引入 Reactive 存储机制。
- [ ] 扩展 UI 组件（如 `AsakiText`），支持直接绑定 Localization Property。
