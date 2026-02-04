// 实体系统使用示例
// 这些示例展示了如何在项目中使用 Asaki 实体系统

/*
 * ============================================
 * 示例 1: 定义自定义组件（使用新的基类）
 * ============================================
 */

/*
// 改进前：需要实现所有接口方法
public class HealthComponent_Old : IEntityComponent
{
    public IEntity Entity { get; set; }
    
    public int MaxHealth { get; set; } = 100;
    public int CurrentHealth { get; set; } = 100;
    
    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose() { }
}

// 改进后：使用 EntityComponent 基类
public class HealthComponent : EntityComponent
{
    public int MaxHealth { get; set; } = 100;
    public int CurrentHealth { get; set; } = 100;
    
    public void TakeDamage(int damage)
    {
        CurrentHealth = Math.Max(0, CurrentHealth - damage);
        if (CurrentHealth == 0)
        {
            AsakiBroker.Publish(new EntityDeathEvent { EntityId = Entity.Id });
        }
    }
}

// 标签组件：一行代码即可
public class PlayerTag : TagComponent { }

public class EnemyTag : TagComponent { }
*/

/*
 * ============================================
 * 示例 2: 使用实体构建器创建实体
 * ============================================
 */

/*
// 改进前：多行代码创建实体
var entity = world.CreateEntity();
entity.AddComponent<HealthComponent>();
entity.AddComponent<PlayerTag>();
entity.AddComponent<LifecycleComponent>();
var health = entity.GetComponent<HealthComponent>();
health.MaxHealth = 200;
health.CurrentHealth = 200;

// 改进后：链式调用，一行代码
var player = world.Create()
    .With<HealthComponent>(h => {
        h.MaxHealth = 200;
        h.CurrentHealth = 200;
    })
    .WithTag<PlayerTag>()
    .With<LifecycleComponent>()
    .Build();

// 或者直接返回ID
EntityId playerId = world.Create()
    .With<HealthComponent>()
    .WithTag<PlayerTag>();
*/

/*
 * ============================================
 * 示例 3: 使用泛型查询（类型安全）
 * ============================================
 */

/*
// 改进前：需要手动获取组件
foreach (var entity in world.Query<HealthComponent>())
{
    var health = entity.GetComponent<HealthComponent>();
    health.CurrentHealth += 10;
}

// 改进后：使用解构直接获取组件
foreach (var (entity, health) in world.QueryWith<HealthComponent>())
{
    health.CurrentHealth += 10;
}

// 改进后：使用 ForEach 批量处理
world.ForEach<HealthComponent>((entity, health) => {
    health.CurrentHealth += 10;
});

// 双组件查询
foreach (var (entity, health, playerTag) in world.QueryWith<HealthComponent, PlayerTag>())
{
    health.CurrentHealth += 10;
}

// 获取第一个匹配的实体
var firstPlayer = world.FirstOrDefault<PlayerTag>();

// 获取匹配数量
int playerCount = world.Count<PlayerTag>();
*/

/*
 * ============================================
 * 示例 4: 批量操作实体
 * ============================================
 */

/*
// 批量恢复生命值
world.BatchModify<HealthComponent>(health => {
    if (health.CurrentHealth < health.MaxHealth)
    {
        health.CurrentHealth = health.MaxHealth;
        return true; // 表示已修改
    }
    return false;
});

// 批量添加组件
world.BatchAddComponent<FrozenComponent>(entity =>
    entity.HasComponent<EnemyTag>() && !entity.HasComponent<FrozenComponent>()
);

// 批量销毁死亡实体
world.BatchDestroy(entity => {
    if (entity.TryGetComponent<HealthComponent>(out var health))
    {
        return health.CurrentHealth <= 0;
    }
    return false;
});

// 批量设置激活状态
world.BatchSetActive(false, entity => entity.HasComponent<EnemyTag>());
*/

/*
 * ============================================
 * 示例 5: 使用实体模板
 * ============================================
 */

/*
// 注册模板
EntityTemplateRegistry.Register("Player", new EntityTemplate()
    .With<HealthComponent>(h => {
        h.MaxHealth = 100;
        h.CurrentHealth = 100;
    })
    .WithTag<PlayerTag>()
    .With<LifecycleComponent>()
    .Configure(e => e.IsActive = true));

EntityTemplateRegistry.Register("Enemy_Goblin", new EntityTemplate()
    .With<HealthComponent>(h => {
        h.MaxHealth = 50;
        h.CurrentHealth = 50;
    })
    .WithTag<EnemyTag>()
    .With<AIComponent>());

// 使用模板创建实体
var player = world.CreateFromTemplate("Player");
var goblin1 = world.CreateFromTemplate("Enemy_Goblin");
var goblin2 = world.CreateFromTemplate("Enemy_Goblin");

// 基于模板创建并自定义
var boss = EntityTemplateRegistry.Get("Enemy_Goblin")
    .Instantiate(world)
    .AddComponent<BossTag>();
var bossHealth = boss.GetComponent<HealthComponent>();
bossHealth.MaxHealth *= 10;
bossHealth.CurrentHealth = bossHealth.MaxHealth;
*/

/*
 * ============================================
 * 示例 6: 使用组件依赖注入
 * ============================================
 */

/*
public class CombatComponent : InjectableComponent
{
    [ComponentDependency(Required = true)]
    private HealthComponent _health;

    [ComponentDependency(Required = false)]
    private ManaComponent _mana;

    public void TakeDamage(int damage)
    {
        _health.CurrentHealth -= damage;
    }

    public void CastSpell(int manaCost)
    {
        if (_mana != null && _mana.CurrentMana >= manaCost)
        {
            _mana.CurrentMana -= manaCost;
        }
    }
}
*/

/*
 * ============================================
 * 示例 7: 使用动态标签系统
 * ============================================
 */

/*
// 添加动态标签
entity.AddTag("Boss");
entity.AddTag("Elite");

// 查询带标签的实体
foreach (var boss in world.QueryByTag("Boss"))
{
    // 处理Boss逻辑
}

// 检查标签
if (entity.HasTag("Elite"))
{
    // 精英怪逻辑
}

// 批量查询
var enemies = world.QueryByAnyTag("Goblin", "Orc", "Troll");
var bosses = world.QueryByAllTags("Boss", "Elite");

// 在组件中使用
public class StatusEffectComponent : EntityComponent
{
    public void ApplyPoison()
    {
        Entity.AddTag("Poisoned");
    }

    public void RemovePoison()
    {
        Entity.RemoveTag("Poisoned");
    }

    public bool IsPoisoned => Entity.HasTag("Poisoned");
}
*/

/*
 * ============================================
 * 示例 8: 在 System 中使用
 * ============================================
 */

/*
public class HealthRegenSystem : IAsakiSystem, IAsakiTickable
{
    private IEntityWorld _world;
    private float _regenInterval = 1f;
    private float _timer;
    
    public void Setup()
    {
        _world = AsakiContext.Get<IAsakiArchitecture>().GetEntityWorld();
        
        // 注册实体模板
        EntityTemplateRegistry.Register("Player", new EntityTemplate()
            .With<HealthComponent>()
            .WithTag<PlayerTag>());
    }
    
    public void Tick(float deltaTime)
    {
        _timer += deltaTime;
        if (_timer < _regenInterval) return;
        _timer = 0;
        
        // 使用新的 ForEach 高效遍历
        _world.ForEach<HealthComponent, PlayerTag>((entity, health, tag) => {
            if (health.CurrentHealth < health.MaxHealth)
            {
                health.CurrentHealth++;
            }
        });
    }
    
    public void Dispose() { }
}
*/

/*
 * ============================================
 * 完整迁移示例
 * ============================================
 */

/*
// ===== 迁移前 =====
public class OldSystem : IAsakiSystem
{
    private EntityModel _entityModel;

    public void Setup()
    {
        _entityModel = AsakiContext.Get<IAsakiArchitecture>().GetModel<EntityModel>();
    }

    public void SpawnPlayer()
    {
        var world = _entityModel.World;
        var entity = world.CreateEntity();
        entity.AddComponent<HealthComponent>();
        entity.AddComponent<PlayerTag>();
        entity.AddComponent<LifecycleComponent>();
        return entity.Id;
    }

    public void UpdateHealth()
    {
        var world = _entityModel.World;
        foreach (var entity in world.Query<HealthComponent>())
        {
            var health = entity.GetComponent<HealthComponent>();
            if (health.CurrentHealth < health.MaxHealth)
            {
                health.CurrentHealth++;
            }
        }
    }
}

// ===== 迁移后 =====
public class NewSystem : IAsakiSystem
{
    private IEntityWorld _world;

    public void Setup()
    {
        _world = AsakiContext.Get<IAsakiArchitecture>().GetEntityWorld();

        // 注册模板
        EntityTemplateRegistry.Register("Player", new EntityTemplate()
            .With<HealthComponent>()
            .WithTag<PlayerTag>()
            .With<LifecycleComponent>());
    }

    public EntityId SpawnPlayer()
    {
        return _world.CreateFromTemplate("Player").Id;
    }

    public void UpdateHealth()
    {
        _world.ForEach<HealthComponent>((entity, health) => {
            if (health.CurrentHealth < health.MaxHealth)
            {
                health.CurrentHealth++;
            }
        });
    }
}
*/
