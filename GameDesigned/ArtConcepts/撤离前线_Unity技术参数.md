# 《撤离前线》Unity技术参数配置

## 1. 精灵图导入配置 (Sprite Import Settings)

### 1.1 角色精灵配置

```yaml
# 玩家角色 / 敌人
Texture Settings:
  Max Size: 2048x2048 (图集)
  Format: RGBA 32 bit
  Compression: 
    - Desktop: None (像素完美)
    - Mobile: ASTC 6x6
  Filter Mode: Point (无滤镜)
  Pixels Per Unit: 64
  Mesh Type: Full Rect
  Extrude Edges: 2 pixels
  Pivot: Center (0.5, 0.5)

Sprite Editor:
  Cell Size: 128x128 pixels
  Padding: 2 pixels between sprites
  Outline Detail: 128 (高精度轮廓)
  Tolerance: 0.1
```

### 1.2 防御塔精灵配置

```yaml
# 防御塔
Texture Settings:
  Max Size: 1024x1024 (每塔)
  Format: RGBA 32 bit
  Compression: ASTC 6x6 (Mobile) / DXT5 (Desktop)
  Filter Mode: Point
  Pixels Per Unit: 64
  Mesh Type: Tight (紧密网格)
  Pivot: Bottom Center (0.5, 0)

Tower Components:
  Base Layer: 单独精灵 (静态)
  Turret Layer: 8-16帧旋转动画
  Effect Layer: 发光/开火效果叠加
```

### 1.3 环境瓦片配置

```yaml
# 环境瓦片
Texture Settings:
  Max Size: 1024x1024 (每瓦片集)
  Format: RGBA 32 bit
  Compression: ASTC 6x6 / DXT5
  Filter Mode: Point
  Pixels Per Unit: 64
  Mesh Type: Full Rect

Tilemap Settings:
  Cell Size: 64x64 pixels
  Tile Mode: Continuous
  Chunk Culling: Enabled
  Auto Chunk Size: 32x32 tiles
```

### 1.4 UI精灵配置

```yaml
# UI元素
Texture Settings:
  Max Size: 2048x2048 (图集)
  Format: RGBA 32 bit
  Compression: None
  Filter Mode: Bilinear (平滑UI)
  Pixels Per Unit: 100 (Unity UI默认)
  Mesh Type: Full Rect
  Generate Mip Maps: Disabled

UI Atlas Packing:
  Padding: 4 pixels
  Allow Rotation: False
  Tight Packing: True
```

---

## 2. 精灵图集规划 (Sprite Atlas Strategy)

### 2.1 图集分组策略

```
Assets/
├── Sprites/
│   ├── Atlas/
│   │   ├── Characters/
│   │   │   ├── Player_Atlas.spriteatlas        # 玩家角色
│   │   │   ├── Enemies_Common_Atlas.spriteatlas # 普通敌人
│   │   │   └── Enemies_Boss_Atlas.spriteatlas   # Boss敌人
│   │   ├── Towers/
│   │   │   ├── Tower_MachineGun_Atlas.spriteatlas
│   │   │   ├── Tower_Flame_Atlas.spriteatlas
│   │   │   ├── Tower_Cryo_Atlas.spriteatlas
│   │   │   ├── Tower_EMP_Atlas.spriteatlas
│   │   │   └── Tower_Missile_Atlas.spriteatlas
│   │   ├── Environment/
│   │   │   ├── Env_Desert_Atlas.spriteatlas
│   │   │   ├── Env_Industrial_Atlas.spriteatlas
│   │   │   └── Env_Military_Atlas.spriteatlas
│   │   ├── Props/
│   │   │   └── Props_Common_Atlas.spriteatlas
│   │   └── UI/
│   │       ├── UI_HUD_Atlas.spriteatlas
│   │       ├── UI_Icons_Atlas.spriteatlas
│   │       └── UI_Menus_Atlas.spriteatlas
```

### 2.2 图集配置示例

```csharp
// SpriteAtlas 配置参数
public class AtlasConfig
{
    // 玩家角色图集
    public static readonly AtlasSettings PlayerAtlas = new()
    {
        MaxTextureSize = 2048,
        Padding = 4,
        AllowRotation = false,
        TightPacking = true,
        IncludeInBuild = true
    };
    
    // 环境图集
    public static readonly AtlasSettings EnvironmentAtlas = new()
    {
        MaxTextureSize = 4096,
        Padding = 2,
        AllowRotation = false,
        TightPacking = true,
        IncludeInBuild = true
    };
    
    // UI图集
    public static readonly AtlasSettings UIAtlas = new()
    {
        MaxTextureSize = 2048,
        Padding = 4,
        AllowRotation = false,
        TightPacking = true,
        IncludeInBuild = true
    };
}
```

---

## 3. 动画配置 (Animation Settings)

### 3.1 角色动画帧规格

| 动画状态 | 帧数 | FPS | 循环 | 总时长(ms) |
|----------|------|-----|------|------------|
| Idle | 4-8 | 6 | Yes | 667-1333 |
| Walk | 6-8 | 10 | Yes | 600-800 |
| Run | 6-8 | 12 | Yes | 500-667 |
| Attack | 4-8 | 15 | No | 267-533 |
| Hit | 2-4 | 20 | No | 100-200 |
| Death | 8-12 | 10 | No | 800-1200 |

### 3.2 防御塔动画规格

| 动画状态 | 帧数 | FPS | 说明 |
|----------|------|-----|------|
| Idle | 1 | - | 静态 |
| Rotate | 16 | - | 360度旋转帧 |
| Fire | 4-6 | 20 | 开火效果 |
| Reload | 4 | 10 | 装弹动画 |

### 3.3 动画控制器结构

```
AnimatorController: PlayerCharacter
├── Base Layer
│   ├── States:
│   │   ├── Idle (default)
│   │   ├── Walk
│   │   ├── Run
│   │   ├── Attack
│   │   ├── Hit
│   │   └── Death
│   ├── Parameters:
│   │   ├── Speed (Float)
│   │   ├── IsMoving (Bool)
│   │   ├── Attack (Trigger)
│   │   ├── Hit (Trigger)
│   │   └── Death (Trigger)
│   └── Transitions:
│       ├── Idle -> Walk: Speed > 0.1
│       ├── Walk -> Run: Speed > 0.8
│       ├── Any -> Attack: Attack trigger
│       ├── Any -> Hit: Hit trigger
│       └── Any -> Death: Death trigger
```

---

## 4. 分辨率与像素密度 (Resolution & Pixel Density)

### 4.1 目标分辨率

| 平台 | 分辨率 | 宽高比 | PPU | 备注 |
|------|--------|--------|-----|------|
| PC/Mac | 1920x1080 | 16:9 | 64 | 基准 |
| PC/Mac 4K | 3840x2160 | 16:9 | 128 | 高DPI |
| Mobile HD | 1280x720 | 16:9 | 64 | 高端手机 |
| Mobile SD | 960x540 | 16:9 | 48 | 低端手机 |
| Tablet | 2048x1536 | 4:3 | 64 | iPad |

### 4.2 屏幕空间分配

```
屏幕布局 (1920x1080 基准):
┌────────────────────────────────────────────────────────────┐
│ [状态栏 80px]                                              │
├────────────────────────────────────────────────────────────┤
│                                                            │
│                                                            │
│                     游戏主区域                              │
│                   (有效视野区域)                            │
│                                                            │
│                                                            │
│                                                            │
├────────────────────────────────────────────────────────────┤
│ [底部UI 120px]  [小地图 200x200]                           │
└────────────────────────────────────────────────────────────┘

有效游戏区域: 1920 x 880 pixels
```

### 4.3 实体尺寸规格

| 实体类型 | 像素尺寸 | 屏幕占比 | Unity Units |
|----------|----------|----------|-------------|
| 玩家角色 | 128x128 | ~6% | 2x2 units |
| 普通敌人 | 64-96 | 3-5% | 1-1.5 units |
| 精英敌人 | 96-128 | 5-6% | 1.5-2 units |
| Boss | 256-512 | 12-24% | 4-8 units |
| 防御塔 | 96x96 | ~4.5% | 1.5x1.5 units |
| 子弹/特效 | 16-32 | <1% | 0.25-0.5 units |

---

## 5. 渲染管线配置 (Render Pipeline)

### 5.1 URP资产配置

```yaml
# URP Asset Settings
UniversalRenderPipelineAsset:
  # 渲染设置
  Depth Texture: Enabled
  Opaque Texture: Disabled
  MSAA: 4x (PC) / 2x (Mobile)
  
  # 光照
  Main Light: Per Pixel
  Additional Lights: Per Vertex (Mobile) / Per Pixel (PC)
  Additional Lights Per Object: 4
  Shadow Distance: 50 units
  Shadow Cascade Count: 1 (2D游戏)
  
  # 后处理
  Post Processing: Enabled
  Color Grading: Enabled
  Bloom: Enabled (低强度)
  
  # 2D设置
  HDR: Disabled
  SRGB: Enabled
```

### 5.2 2D渲染器配置

```yaml
# 2D Renderer Data
Renderer2DData:
  # 精灵排序
  Transparency Sort Mode: Custom Axis
  Transparency Sort Axis: (0, 1, 0)
  
  # 光照
  Light Blending: Enabled
  Light Layers: Default
  
  # 后处理
  Post Process Data: Default
```

### 5.3 图层排序

```
Sorting Layers (从后到前):
├── Background (Order: -100)
├── Environment (Order: -50)
├── Ground (Order: -10)
├── Props_Back (Order: 0)
├── Shadows (Order: 10)
├── Characters (Order: 20)
├── Enemies (Order: 30)
├── Towers (Order: 40)
├── Projectiles (Order: 50)
├── Effects (Order: 60)
├── Props_Front (Order: 70)
└── UI_World (Order: 100)
```

---

## 6. Shader配置 (Shader Settings)

### 6.1 标准角色Shader

```hlsl
// Custom Sprite Shader for Characters
Shader "Custom/CharacterSprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _RimColor ("Rim Light Color", Color) = (0.5,0.5,0.5,1)
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 3.0
        _HitColor ("Hit Flash Color", Color) = (1,0,0,1)
        _HitAmount ("Hit Flash Amount", Range(0, 1)) = 0
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _DissolveColor ("Dissolve Edge Color", Color) = (1,0.5,0,1)
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        
        Pass
        {
            // 基础精灵渲染
            // + 边缘光效果
            // + 受伤闪烁
            // + 死亡消融
        }
    }
}
```

### 6.2 防御塔Shader

```hlsl
// Tower Shader with Glow Effect
Shader "Custom/TowerSprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _GlowIntensity ("Glow Intensity", Range(0, 2)) = 0.5
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 1.0
    }
    
    // 实现发光脉冲效果
    // 用于塔的能量核心/指示灯
}
```

### 6.3 环境Shader

```hlsl
// Environment Tile Shader
Shader "Custom/EnvironmentTile"
{
    Properties
    {
        _MainTex ("Tile Texture", 2D) = "white" {}
        _ScrollSpeed ("Scroll Speed", Vector) = (0, 0, 0, 0)
        _Tint ("Weather Tint", Color) = (1,1,1,1)
    }
    
    // 实现流动效果 (如熔岩/水)
    // 天气变色 (沙尘暴/夜间)
}
```

---

## 7. 性能优化配置 (Performance Optimization)

### 7.1 内存预算

| 资源类型 | PC预算 | Mobile预算 | 说明 |
|----------|--------|------------|------|
| 角色图集 | 16 MB | 8 MB | 所有角色+敌人 |
| 防御塔图集 | 8 MB | 4 MB | 5种塔+升级 |
| 环境图集 | 32 MB | 16 MB | 3种环境+变体 |
| UI图集 | 8 MB | 4 MB | HUD+菜单+图标 |
| 特效图集 | 16 MB | 8 MB | 粒子+特效 |
| 音频 | 32 MB | 16 MB | 音效+音乐 |
| **总计** | **112 MB** | **56 MB** | - |

### 7.2 Draw Call优化

```csharp
// 批处理配置
public class BatchingConfig
{
    // 静态批处理
    public const bool StaticBatching = true;
    
    // 动态批处理
    public const bool DynamicBatching = true;
    public const int BatchBufferSize = 1024; // vertices
    
    // GPU Instancing (用于相同敌人)
    public const bool GPUInstancing = true;
    public const int MaxInstancesPerBatch = 256;
    
    // Sprite Atlas批处理
    public const bool SpriteAtlasBatching = true;
}
```

### 7.3 对象池配置

```csharp
// 对象池预设
public class PoolConfig
{
    // 子弹池
    public const int BulletPoolSize = 100;
    public const int BulletPoolExpand = 20;
    
    // 特效池
    public const int EffectPoolSize = 50;
    public const int EffectPoolExpand = 10;
    
    // 敌人池
    public const int EnemyPoolSize = 30;
    public const int EnemyPoolExpand = 10;
    
    // UI元素池
    public const int UIPoolSize = 20;
    public const int UIPoolExpand = 5;
}
```

### 7.4 帧率目标

| 平台 | 目标帧率 | 最低帧率 | VSync |
|------|----------|----------|-------|
| PC 高端 | 144 FPS | 60 FPS | Off |
| PC 中端 | 60 FPS | 45 FPS | On |
| Mobile 高端 | 60 FPS | 30 FPS | On |
| Mobile 低端 | 30 FPS | 20 FPS | On |

---

## 8. 平台特定配置 (Platform-Specific Settings)

### 8.1 PC/Mac配置

```yaml
PC Settings:
  Resolution:
    Default: 1920x1080
    Fullscreen Mode: Fullscreen Window
    Resizable Window: True
  
  Graphics:
    Quality Level: High
    Anti Aliasing: 4x MSAA
    Anisotropic Filtering: Enabled
    Texture Quality: Full Resolution
  
  Input:
    Keyboard + Mouse: Primary
    Gamepad: Supported
```

### 8.2 iOS配置

```yaml
iOS Settings:
  Resolution:
    Default: Device Native
    Orientation: Landscape
    
  Graphics:
    Quality Level: Medium
    Anti Aliasing: 2x MSAA
    Texture Quality: Half Resolution (older devices)
    Target Frame Rate: 60
    
  Memory:
    Texture Compression: ASTC 6x6
    Audio Compression: AAC
    
  Features:
    Metal API: Required
    Multithreaded Rendering: Enabled
```

### 8.3 Android配置

```yaml
Android Settings:
  Resolution:
    Default: Device Native
    Orientation: Landscape
    
  Graphics:
    Quality Level: Medium
    Anti Aliasing: 2x MSAA (optional)
    Texture Quality: Adaptive
    Target Frame Rate: 60
    
  Memory:
    Texture Compression: ASTC 6x6
    Audio Compression: Vorbis
    
  Features:
    Vulkan: Preferred
    OpenGL ES 3.0: Minimum
    Multithreaded Rendering: Enabled
```

---

## 9. 资源命名规范 (Asset Naming Convention)

### 9.1 精灵命名

```
格式: [类型]_[名称]_[变体]_[帧号]

示例:
- Char_Commander_Idle_01
- Char_Commander_Walk_01
- Tower_MachineGun_Base
- Tower_MachineGun_Turret_01
- Enemy_Wanderer_Attack_01
- Env_Desert_Tile_Ground_01
- Prop_Barrel_Rusted_01
- UI_Icon_Weapon_Rifle
- UI_HUD_HealthBar_BG
```

### 9.2 动画命名

```
格式: [类型]_[名称]_[动作]

示例:
- Char_Commander_Idle
- Char_Commander_Walk
- Char_Commander_Attack
- Tower_MachineGun_Fire
- Enemy_Wanderer_Death
```

### 9.3 图集命名

```
格式: Atlas_[分类]_[子类]

示例:
- Atlas_Characters_Player
- Atlas_Characters_Enemies
- Atlas_Towers_All
- Atlas_Environment_Desert
- Atlas_UI_HUD
- Atlas_UI_Icons
```

---

## 10. 质量检查清单 (Quality Checklist)

### 10.1 导入检查

- [ ] 所有精灵使用相同的PPU设置
- [ ] 图集尺寸为2的幂次方
- [ ] 精灵边缘无透明像素残留
- [ ] Pivot点设置正确
- [ ] 压缩格式符合平台要求

### 10.2 视觉检查

- [ ] 所有资产风格一致
- [ ] 色彩符合规范
- [ ] 轮廓清晰可辨
- [ ] 动画流畅无跳帧
- [ ] UI元素可读性良好

### 10.3 性能检查

- [ ] Draw Calls < 100 (移动端)
- [ ] 内存使用在预算内
- [ ] 无内存泄漏
- [ ] 帧率稳定
- [ ] 无GC Spike

---

**文档版本**: v1.0
**创建日期**: 2026-02-13
**适用Unity版本**: 2022.3 LTS / 2023.2
