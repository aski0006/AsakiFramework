# 测试结果

**测试时间**: 2026-02-01 14:55:41  
**测试套件**: Asaki.Tests.Pooling.AsakiPoolGovernanceTests  
**总计**: 1 个测试  
**通过**: 1 个  
**失败**: 0 个

---

## 测试详情

### 1. PerformGovernance_WhenIntervalPassed_PerformsShrink
- **描述**: 测试治理检查在间隔到达时执行
- **结果**: Passed
- **耗时**: 1.129282s
- **日志**:
  - TestPool Prewarm completed, created 10 objects
  - TestPool LRU Shrink removed 5 objects (idle > 0.05s), remaining: 5
  - TestPool Disposed - Total: 10, Active: 0, Inactive: 0, Destroyed: 10, Gets: 10, Returns: 10