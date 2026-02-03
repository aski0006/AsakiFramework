// 实体系统使用示例
// 这些示例展示了如何在项目中使用 Asaki 实体系统

/*
 * ============================================
 * 示例 1: 定义自定义组件
 * ============================================
 */

/*
public class HealthComponent : IEntityComponent
{
    public IEntity Entity { get; set; }
    
    public int MaxHealth { get; set; } = 100;
    public int CurrentHealth { get; set; } = 100;
    
    public void TakeDamage(int damage)
    {
        CurrentHealth = Math.Max(0, CurrentHealth - damage);
        if (CurrentHealth == 0)
        {
            // 发布实体死亡事件
            AsakiBroker.Publish(new EntityDeathEvent { EntityId = Entity.Id });
        }
    }
    
    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose() { }
}

public class PlayerTag : IEntityComponent
{
    public IEntity Entity { get; set; }
    public void OnAttach() { }
    public void OnDetach() { }
    public void OnEnable() { }
    public void OnDisable() { }
    public void Dispose() { }
}
*/

/*
 * ============================================
 * 示例 2: 在 Architecture 中注册实体系统
 * ============================================
 */

/*
public class GameArchitecture : AsakiArchitecture
{
    protected override void OnSetup()
    {
        // 注册实体模型
        var entityModel = new EntityModel();
        RegisterModel(entityModel);
        
        // 注册游戏系统
        RegisterSystem(new PlayerSystem());
        RegisterSystem(new EnemySystem());
    }
}
*/

/*
 * ============================================
 * 示例 3: 使用 Command 创建实体
 * ============================================
 */

/*
// 创建玩家实体命令
public class CreatePlayerCommand : AsakiCommand<EntityId>
{
    public override EntityId Execute()
    {
        var world = GetModel<EntityModel>().World;
        var entity = world.CreateEntity();
        
        entity.AddComponent<LifecycleComponent>();
        entity.AddComponent<HealthComponent>();
        entity.AddComponent<PlayerTag>();
        
        return entity.Id;
    }
}

// 使用命令
var playerId = architecture.ExecuteCommand(new CreatePlayerCommand());
*/

/*
 * ============================================
 * 示例 4: System 中批量处理实体（利用魔法容器性能）
 * ============================================
 */

/*
public class HealthRegenSystem : IAsakiSystem, IAsakiTickable
{
    private EntityModel _entityModel;
    private float _regenInterval = 1f;
    private float _timer;
    
    public void Setup()
    {
        // 可以通过 Resolver 或其他方式获取 EntityModel
    }
    
    public void Tick(float deltaTime)
    {
        _timer += deltaTime;
        if (_timer < _regenInterval) return;
        _timer = 0;
        
        // 利用魔法容器的高性能遍历
        var world = _entityModel.World as EntityWorld;
        if (world != null)
        {
            world.ForEach(entity =>
            {
                if (entity is Entity e && e.HasComponent<HealthComponent>())
                {
                    var health = e.GetComponent<HealthComponent>();
                    if (health.CurrentHealth < health.MaxHealth)
                    {
                        health.CurrentHealth++;
                    }
                }
            });
        }
    }
    
    public void Dispose() { }
}
*/

/*
 * ============================================
 * 示例 5: 事件响应
 * ============================================
 */

/*
public class PlayerDeathHandler : IAsakiHandler<EntityDestroyedEvent>
{
    public void OnEvent(EntityDestroyedEvent e)
    {
        // 可以在这里处理实体销毁逻辑
        // 例如：检查是否是玩家，触发游戏结束等
    }
}
*/

/*
 * ============================================
 * 示例 6: 实体查询
 * ============================================
 */

/*
// 查询所有带 HealthComponent 的实体
var damagedEntities = world.Query<HealthComponent>()
    .Where(e => e.GetComponent<HealthComponent>().CurrentHealth < 100);

// 查询所有玩家
var players = world.Query<PlayerTag>();

// 查询带特定组件组合的实体
var alivePlayers = world.Query<PlayerTag, HealthComponent>();
*/
