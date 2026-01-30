---
name: "unity-code-analyzer"
description: "Analyzes Unity C# code for memory leaks, performance issues, and thread safety problems. Invoke when user asks for code quality analysis, Unity code review, or detecting potential issues in Unity scripts."
---

# Unity Code Quality Analyzer

A comprehensive code analysis tool for Unity projects that detects memory management issues, performance bottlenecks, and thread safety violations.

## When to Invoke

- User asks to analyze Unity code quality
- User requests code review for Unity scripts
- User wants to detect memory leaks in Unity code
- User needs performance optimization suggestions
- User is concerned about thread safety in Unity
- User asks to check code for best practices

## Analysis Categories

### 1. Memory Management Issues

#### 1.1 Unreleased Resources
**Detection Patterns:**
- `Texture2D`, `RenderTexture`, `Material`, `Mesh` created but not destroyed
- `AssetBundle` not unloaded after use
- `WWW` or `UnityWebRequest` not disposed
- `AudioClip` resources not released

**Code Patterns to Detect:**
```csharp
// ❌ BAD: Resource not released
Texture2D texture = new Texture2D(1024, 1024);
// Missing: Destroy(texture);

// ❌ BAD: AssetBundle not unloaded
AssetBundle bundle = AssetBundle.LoadFromFile(path);
// Missing: bundle.Unload(false);

// ✅ GOOD: Proper cleanup
Texture2D texture = new Texture2D(1024, 1024);
try {
    // Use texture
} finally {
    Destroy(texture);
}
```

#### 1.2 Object Reference Management
**Detection Patterns:**
- Static references to MonoBehaviour/ScriptableObject
- Event subscriptions not unsubscribed
- Delegate references causing memory leaks
- Circular references between objects

**Code Patterns to Detect:**
```csharp
// ❌ BAD: Static reference to MonoBehaviour
public static MyMonoBehaviour Instance;

// ❌ BAD: Event not unsubscribed
void OnEnable() {
    EventManager.OnEvent += HandleEvent;
}
// Missing: OnDisable() with -=

// ✅ GOOD: Proper lifecycle management
void OnEnable() {
    EventManager.OnEvent += HandleEvent;
}
void OnDisable() {
    EventManager.OnEvent -= HandleEvent;
}
```

#### 1.3 Large Object Lifecycle
**Detection Patterns:**
- Large arrays/lists in MonoBehaviour without pooling
- Texture/Sprite references in static fields
- Scene objects with large data structures

### 2. Performance Optimization Issues

#### 2.1 Unnecessary Loops
**Detection Patterns:**
- `GetComponent()` inside `Update()` or loops
- `Find()` methods in hot paths
- LINQ queries in performance-critical code
- String concatenation in loops

**Code Patterns to Detect:**
```csharp
// ❌ BAD: GetComponent in Update
void Update() {
    GetComponent<Rigidbody>().velocity = Vector3.zero;
}

// ❌ BAD: Find in Update
void Update() {
    GameObject player = GameObject.Find("Player");
}

// ❌ BAD: LINQ in hot path
void Update() {
    var items = allItems.Where(x => x.active).ToList();
}

// ✅ GOOD: Cached reference
private Rigidbody rb;
void Start() {
    rb = GetComponent<Rigidbody>();
}
void Update() {
    rb.velocity = Vector3.zero;
}
```

#### 2.2 Redundant Calculations
**Detection Patterns:**
- `transform.position`/`rotation` repeated access
- `Camera.main` repeated calls
- Mathematical operations that can be cached
- Property getters with heavy computation

**Code Patterns to Detect:**
```csharp
// ❌ BAD: Repeated property access
void Update() {
    transform.position = new Vector3(transform.position.x, 0, transform.position.z);
}

// ❌ BAD: Camera.main in loop
void Update() {
    for (int i = 0; i < 100; i++) {
        Vector3 viewport = Camera.main.WorldToViewportPoint(positions[i]);
    }
}

// ✅ GOOD: Cached values
private Camera mainCamera;
void Start() {
    mainCamera = Camera.main;
}
void Update() {
    Vector3 pos = transform.position;
    transform.position = new Vector3(pos.x, 0, pos.z);
}
```

#### 2.3 GC Allocation Triggers
**Detection Patterns:**
- Boxing operations (struct to object conversion)
- Closure allocations in lambdas
- Array resizing in loops
- String operations creating garbage

**Code Patterns to Detect:**
```csharp
// ❌ BAD: Boxing
object obj = 5; // int boxed

// ❌ BAD: Closure allocation
void Update() {
    int local = 5;
    Action action = () => Debug.Log(local); // Allocates
}

// ❌ BAD: String concatenation
void Update() {
    string result = "Frame: " + Time.frameCount; // Allocates every frame
}

// ✅ GOOD: StringBuilder or interpolation caching
private StringBuilder sb = new StringBuilder();
void Update() {
    sb.Clear();
    sb.Append("Frame: ").Append(Time.frameCount);
}
```

#### 2.4 Rendering Pipeline Issues
**Detection Patterns:**
- `GetComponent<Renderer>()` in Update
- Material property changes every frame
- Dynamic batching breaking operations
- Shader property access in loops

### 3. Thread Safety Issues

#### 3.1 Unity API Off-Main Thread
**Detection Patterns:**
- `transform`, `gameObject` access in async/await
- `Instantiate`/`Destroy` in threads
- `Physics` API in non-main threads
- `GetComponent` in background threads

**Code Patterns to Detect:**
```csharp
// ❌ BAD: Unity API in async
async void LoadData() {
    await Task.Run(() => {
        transform.position = Vector3.zero; // CRASH!
    });
}

// ❌ BAD: Instantiate in thread
void Start() {
    new Thread(() => {
        Instantiate(prefab); // CRASH!
    }).Start();
}

// ✅ GOOD: Unity API on main thread
async void LoadData() {
    var result = await Task.Run(() => ComputeData());
    await UniTask.SwitchToMainThread();
    transform.position = result;
}
```

#### 3.2 Resource Competition
**Detection Patterns:**
- Shared collections accessed from multiple threads
- `Dictionary`/`List` concurrent access
- Static variables modified by threads
- Lock-free data structures needed

#### 3.3 Async/Await Misuse
**Detection Patterns:**
- `async void` methods
- Missing `ConfigureAwait(false)` in libraries
- Deadlock potential with `.Result`
- Unawaited async operations

**Code Patterns to Detect:**
```csharp
// ❌ BAD: async void
async void OnButtonClick() { // Can crash on exception
    await LoadData();
}

// ❌ BAD: .Result blocking
void Start() {
    var data = LoadData().Result; // Potential deadlock
}

// ✅ GOOD: async Task
async Task OnButtonClickAsync() {
    await LoadData();
}
```

## Analysis Execution Workflow

### Step 1: Initialize Analysis Session
```
1. Check for existing analysis state file (.trae/unity-analysis-state.json)
2. If exists, load previous progress and findings
3. If new session, initialize empty state
4. Display analysis plan and modules
```

### Step 2: Dependency Tracing
```
1. Identify entry point (user-specified file or all .cs files)
2. Parse using statements and namespace imports
3. Build dependency graph:
   - Direct class references
   - Interface implementations
   - Inheritance chains
   - Event subscriptions
4. Mark files for analysis based on dependency depth
```

### Step 3: Code Path Analysis
```
For each analyzed file:
   1. Parse method bodies and control flow
   2. Track variable lifecycles
   3. Identify resource allocations and releases
   4. Check thread boundary crossings
   5. Record all findings with severity
```

### Step 4: Severity Classification

| Severity | Description | Examples |
|----------|-------------|----------|
| 🔴 **Critical** | Will cause crashes or severe memory leaks | Unity API off-main thread, unclosed native resources |
| 🟠 **High** | Significant performance impact or memory leaks | GetComponent in Update, static MonoBehaviour refs |
| 🟡 **Medium** | Moderate performance issues | Redundant calculations, unnecessary allocations |
| 🔵 **Low** | Code style or minor optimization | Missing null checks, redundant using statements |

## Session State Management

### State File Format (.trae/unity-analysis-state.json)
```json
{
  "sessionId": "uuid",
  "startTime": "2026-01-30T10:00:00Z",
  "lastUpdate": "2026-01-30T10:30:00Z",
  "progress": {
    "totalFiles": 150,
    "analyzedFiles": 45,
    "currentFile": "Assets/Scripts/PlayerController.cs"
  },
  "findings": [
    {
      "id": "finding-001",
      "severity": "high",
      "category": "memory",
      "file": "Assets/Scripts/AudioManager.cs",
      "line": 45,
      "message": "Static reference to MonoBehaviour detected",
      "suggestion": "Use singleton pattern with proper cleanup"
    }
  ],
  "pendingModules": ["thread-safety", "rendering"],
  "completedModules": ["memory-basic", "performance-basic"]
}
```

### State Persistence Rules
```
1. Save state after each file analysis completes
2. Compress findings to essential info only
3. Store file positions for resume capability
4. Clear state when analysis completes or user requests reset
```

## Output Report Format

### Structured Analysis Report

```markdown
# Unity Code Quality Analysis Report
**Project**: [Project Name]  
**Analysis Date**: [Date]  
**Files Analyzed**: [Count]  
**Session Duration**: [Time]

## Executive Summary
- 🔴 Critical Issues: [N]
- 🟠 High Severity: [N]
- 🟡 Medium Severity: [N]
- 🔵 Low Severity: [N]

## Detailed Findings

### 🔴 Critical Issues

#### [Issue Title]
- **File**: `path/to/file.cs:line`
- **Category**: Memory/Performance/ThreadSafety
- **Description**: Detailed explanation
- **Impact**: What could go wrong
- **Solution**: 
  ```csharp
  // Before (problematic)
  [problem code]
  
  // After (fixed)
  [fixed code]
  ```
- **References**: [Unity Docs links, Best Practice articles]

### 🟠 High Severity Issues
[Same format...]

## Recommendations by Category

### Memory Management
1. [Priority recommendation]
2. [Secondary recommendation]

### Performance Optimization
1. [Priority recommendation]
2. [Secondary recommendation]

### Thread Safety
1. [Priority recommendation]
2. [Secondary recommendation]

## Action Items
- [ ] Fix critical issue #1
- [ ] Review high severity issues in AudioManager
- [ ] Implement object pooling for particle systems
```

## Analysis Commands

### Start New Analysis
```
Analyze Unity code quality for project
Focus on: [memory|performance|thread-safety|all]
Entry point: [specific file or "all"]
```

### Continue Previous Analysis
```
Continue Unity code analysis from last session
```

### Analyze Specific File
```
Analyze file: Assets/Scripts/PlayerController.cs
Focus on memory management issues
```

### Export Report
```
Export analysis report to: analysis-report.md
```

## Best Practices Reference

### Memory Management
- Use object pooling for frequently instantiated objects
- Always unsubscribe from events in OnDisable
- Avoid static references to scene objects
- Use `using` statements for IDisposable objects
- Call `UnloadUnusedAssets()` after large resource releases

### Performance Optimization
- Cache component references in Start/Awake
- Use `CompareTag()` instead of `tag ==`
- Avoid LINQ in hot paths
- Use `StringBuilder` for string operations in loops
- Prefer `transform.localPosition` over `transform.position` when possible

### Thread Safety
- Only use Unity API on main thread
- Use `MainThreadDispatcher` or `UniTask.SwitchToMainThread()`
- Use concurrent collections for thread-shared data
- Prefer `async Task` over `async void`
- Use `CancellationToken` for cancellable operations
