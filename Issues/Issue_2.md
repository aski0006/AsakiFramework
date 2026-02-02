# Issue #2: [Feature] Data Versioning & Migration Pipeline (数据版本控制与自动迁移系统)

**状态**: OPEN  
**作者**: aski0006 (Asaki0019)  
**创建时间**: 2026-01-31  
**更新时间**: 2026-01-31  
**URL**: https://github.com/aski0006/AsakiFramework/issues/2

---

## 1. 背景与痛点 (Problem Statement)

目前框架采用了高性能 的二进制序列化 (`AsakiBinarySerialization`) 作为存档方案。虽然性能优异，但二进制格式对数据结构变化极其敏感（Brittle）。

* **开发痛点**：在开发过程中，一旦修 改 `SaveModel`（如新增字段），旧的二进制存档文件将无法读取，导致报错或数据错乱，必须删档重来。
* **运营风险**：在游戏上线后，如果版本更新修改了存档结构，无法兼容旧版本存档，将导致玩家进度丢失，这是无法接受的。

---

## 2. 解决方案提案 (Proposed Solution)

建议在底层序列化系统中引入**数据演进（Schema Evolution）**支持，无需开发者编写复杂的转换脚本，框架自动处理版本兼容。

**核心功能点：**

### 2.1 版本标记 (Attribute-based Versioning)
* 引入 `[AsakiSchema(Version = X)]` 特性，标记当前数据结构的版本号。
* 在二进制文件头部自动写入该版本号。

### 2.2 迁移管线 (Migration Pipeline)
* 在 `IAsakiReader` 读取时，对比 **文件版 本** 与 **代码版本**。
* 如果 `FileVer < CodeVer`，触发迁移流程，而不是直接报错。
* 提供 `IMigrationStep` 接口或 `OnMigrate(int oldVer, int newVer)` 回调，允 许开发者在此处手动修补数据（如填充默认值）。

### 2.3 代码生成集成 (Roslyn Integration)
* 升级 `AsakiSaveGenerator`，使其生成的 `Deserialize` 代码具备版本感知能力。
* 支持生成类似 `Read_V1`, `Read_V2` 的分支逻辑，或在读取新字段时检测版本号，如果旧版本缺失则自动跳过读取并赋默认值。

---

## 3. 预期收益 (Benefits)

* **稳定性**：彻底解决二进制存档"一改就坏"的问题。
* **兼容性**：保障 游戏长线运营中的存档兼容，支持从 v1.0 平滑升级到 v2.0。
* **自动化**：通过 Roslyn 减少手动写版本兼容代码的工作量。

---

## 4. 参考实现 (References)

* 参考 Entity Framework 的 Migration 思路。
* 结合现有的 `AsakiBinaryWriter/Reader` 和 `Source Generator` 架构。
