# Unity 控制台报错信息提取

**提取时间:** 2026-02-07  
**来源:** Unity Editor Console

---

## 概览

本次提取的日志包含以下类型的错误：
- **编译错误 (Error):** 35 个
- **警告 (Warning):** 3 个
- **异常 (Exception):** 1 个
- **普通日志 (Log):** 1 条

---

## 编译错误 (Compilation Errors)

### 1. AsakiApiResultTests.cs 错误

**文件:** `Assets/Asaki/Tests/Network/AsakiApiResultTests.cs`

| 行号 | 错误代码 | 错误描述 |
|------|----------|----------|
| 16 | CS0246 | 找不到类型或命名空间名 'IAsakiWriter' |
| 17 | CS0246 | 找不到类型或命名空间名 'IAsakiReader' |
| 14 | CS0534 | 'TestApiResponse' 未实现继承的抽象成员 'AsakiResponseBase.DeserializeCore(IAsakiReader)' |
| 14 | CS0534 | 'TestApiResponse' 未实现继承的抽象成员 'AsakiResponseBase.SerializeCore(IAsakiWriter)' |

**问题分析:**
- 缺少 `IAsakiWriter` 和 `IAsakiReader` 接口的定义或 using 指令
- `TestApiResponse` 类继承自 `AsakiResponseBase` 但未实现必需的抽象方法

---

### 2. AsakiDownloadServiceTests.cs 错误

**文件:** `Assets/Asaki/Tests/Network/AsakiDownloadServiceTests.cs`

**MockAsyncService 类未实现的接口成员 (IAsakiAsyncService):**

| 序号 | 未实现成员 |
|------|-----------|
| 1 | `WaitSeconds(float, CancellationToken)` |
| 2 | `WaitSecondsUnscaled(float, CancellationToken)` |
| 3 | `WaitFrames(int, CancellationToken)` |
| 4 | `WaitFixedFrame(CancellationToken)` |
| 5 | `WaitFixedFrames(int, CancellationToken)` |
| 6 | `WaitUntil(Func<bool>, CancellationToken)` |
| 7 | `WaitWhile(Func<bool>, CancellationToken)` |
| 8 | `WaitUntil(Func<bool>, float, CancellationToken)` |
| 9 | `WaitWhile(Func<bool>, float, CancellationToken)` |
| 10 | `RunTask(Func<UniTask>, CancellationToken)` |
| 11 | `RunTask<T>(Func<UniTask<T>>, CancellationToken)` |
| 12 | `DelayedCall(float, Action, CancellationToken, bool)` |
| 13 | `NextFrameCall(Action, CancellationToken)` |
| 14 | `When(Func<bool>, Action, CancellationToken)` |
| 15 | `WaitAll(IEnumerable<UniTask>, CancellationToken)` |
| 16 | `WaitAny(IEnumerable<UniTask>, CancellationToken)` |
| 17 | `Sequence(IEnumerable<Func<UniTask>>, CancellationToken)` |
| 18 | `Parallel(IEnumerable<Func<UniTask>>, CancellationToken)` |
| 19 | `Retry(Func<UniTask>, int, float, CancellationToken)` |
| 20 | `WaitCustom(IAsakiWaitSource, CancellationToken)` |
| 21 | `CreateWaitBuilder()` |
| 22 | `CancelAllTasks()` |
| 23 | `CreateLinkedToken(CancellationToken)` |
| 24 | `RunningTaskCount` (属性) |

**MockEventService 类未实现的接口成员 (IAsakiEventService):**

| 序号 | 未实现成员 |
|------|-----------|
| 1 | `Subscribe<T>(IAsakiHandler<T>)` |
| 2 | `Unsubscribe<T>(IAsakiHandler<T>)` |
| 3 | `IDisposable.Dispose()` |

**其他错误:**
- 第 43 行: `MockEventService.Publish<T>` 的类型参数 'T' 约束与接口方法不匹配 (CS0425)

**问题分析:**
- `MockAsyncService` 和 `MockEventService` 是测试用的模拟类，但它们没有完整实现对应的接口
- 可能是接口定义更新后，测试类没有及时同步更新

---

### 3. AsakiWebServiceTests.cs 错误

**文件:** `Assets/Asaki/Tests/Network/AsakiWebServiceTests.cs`

| 行号 | 错误代码 | 错误描述 |
|------|----------|----------|
| 85 | CS0246 | 找不到类型或命名空间名 'IAsakiSavable' |
| 63 | CS0246 | 找不到类型或命名空间名 'IAsakiSavable' |
| 89 | CS0246 | 找不到类型或命名空间名 'IAsakiWriter' |
| 94 | CS0246 | 找不到类型或命名空间名 'IAsakiReader' |
| 68 | CS0246 | 找不到类型或命名空间名 'IAsakiWriter' |
| 74 | CS0246 | 找不到类型或命名空间名 'IAsakiReader' |

**问题分析:**
- 缺少 `IAsakiSavable`、`IAsakiWriter` 和 `IAsakiReader` 接口的定义或 using 指令
- 这些接口可能在重构过程中被移动、重命名或删除

---

## 警告 (Warnings)

### 1. ComboInputTypeRegistry 警告

**时间:** 2026-02-07T19:16:01  
**文件:** `Assets/Asaki/Plungin/ComboSystem/Editor/ComboInputTypeRegistry.cs:279`

```
[ComboInputTypeRegistry] Failed to load user defined types: JSON must represent an object type.
```

**堆栈跟踪:**
```
UnityEngine.Debug:LogWarning (object)
Asaki.Plungin.ComboSystem.Editor.ComboInputTypeRegistry:LoadUserDefinedTypes ()
Asaki.Plungin.ComboSystem.Editor.ComboInputTypeRegistry:Initialize ()
```

**问题分析:**
- 用户自定义类型的 JSON 配置文件格式不正确
- JSON 需要是一个对象类型，但可能配置成了数组或其他格式

---

### 2. Lighting Data 警告

**时间:** 2026-02-07T17:35:41

```
Lighting data asset 'LightingData' is incompatible with the current Unity version 
because the scene it was baked for was not serialized. 
Please use Generate Lighting to rebuild the lighting data, 
or assign the target scene to the Lighting Data Asset in the inspector.
```

**解决方案:**
- 使用 `Generate Lighting` 重新生成光照数据
- 或在 Inspector 中将目标场景分配给 Lighting Data Asset

---

## 异常 (Exceptions)

### ArgumentNullException

**时间:** 2026-02-07T17:46:26  
**类型:** `ArgumentNullException`

```
ArgumentNullException: No GameObject reference provided.
Parameter name: gameObjectRef
```

**堆栈跟踪:**
```
com.IvanMurzak.Unity.MCP.Editor.API.Tool_GameObject.AddComponent
  at ./Library/PackageCache/com.ivanmurzak.unity.mcp/Editor/Scripts/API/Tool/GameObject.Component.Add.cs:42
```

**问题分析:**
- Unity MCP 插件在调用 `AddComponent` 时没有提供 GameObject 引用
- 这是 MCP 工具调用时的参数验证错误

---

## 普通日志 (Logs)

### Scene Context 创建成功

**时间:** 2026-02-07T17:43:39

```
[Asaki] Scene Context created successfully.
```

**来源:** `Asaki.Editor.Utilities.Tools.AsakiSceneContextCreator:CreateSceneContext()`

---

## 建议修复方案

### 高优先级

1. **修复接口缺失问题**
   - 检查 `IAsakiWriter`、`IAsakiReader`、`IAsakiSavable` 接口是否存在于项目中
   - 如果接口被重命名，更新测试文件中的引用
   - 如果接口被删除，需要重构测试代码

2. **更新测试模拟类**
   - 更新 `MockAsyncService` 类，实现所有 `IAsakiAsyncService` 接口成员
   - 更新 `MockEventService` 类，实现所有 `IAsakiEventService` 接口成员

3. **修复 ComboSystem JSON 配置**
   - 检查用户自定义类型的 JSON 配置文件
   - 确保 JSON 格式为对象类型 `{}` 而非数组 `[]`

### 中优先级

4. **重新生成光照数据**
   - 打开 Lighting 窗口
   - 点击 `Generate Lighting` 按钮重新烘焙光照

---

## 相关文件路径

```
Assets/Asaki/Tests/Network/AsakiApiResultTests.cs
Assets/Asaki/Tests/Network/AsakiDownloadServiceTests.cs
Assets/Asaki/Tests/Network/AsakiWebServiceTests.cs
Assets/Asaki/Plungin/ComboSystem/Editor/ComboInputTypeRegistry.cs
Assets/Asaki/Editor/Utilities/Tools/AsakiSceneContextCreator.cs
```

---

*此文档由 AI 自动生成，用于记录 Unity 控制台的报错信息*
