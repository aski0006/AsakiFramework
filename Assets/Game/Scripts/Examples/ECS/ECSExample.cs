using System.Collections.Generic;
using Asaki.Core.Architecture.Entities;
using Asaki.Core.Attributes;
using Asaki.Core.Context;
using Asaki.Core.Context.Resolvers;
using Asaki.Core.Logging;
using Asaki.Unity;
using UnityEngine;

namespace Asaki.Game.Scripts.Examples.ECS
{
    /// <summary>
    /// ECS示例驱动 - 展示如何使用Asaki的ECS系统
    /// 将此脚本挂载到场景中的GameObject上即可运行
    /// </summary>
    public class ECSExample : AsakiMono, IAsakiAutoInject, IAsakiInject<ECSArchitecture>
    {
        [Header("配置")]
        [SerializeField]
        private int _initialEnemyCount = 5;

        [SerializeField]
        private int _initialStaticCount = 3;

        private ECSArchitecture _architecture;
        private IEntityWorld _world;
        private HealthSystem _healthSystem;
        private readonly List<IEntity> _entities = new();

        [AsakiInject]
        public void Inject(ECSArchitecture args)
        {
            _architecture = args;
        }

        protected override void OnStart()
        {
            InitializeArchitecture();
            CreateInitialEntities();
            ALog.Info("[ECSExample] ECS Example started. Use GUI buttons to interact.");
        }

        private void InitializeArchitecture()
        {
            _world = _architecture.GetWorld();
            _healthSystem = _architecture.GetHealthSystem();
        }

        private void CreateInitialEntities()
        {
            var player = _architecture.CreatePlayerEntity();
            _entities.Add(player);

            for (int i = 0; i < _initialEnemyCount; i++)
            {
                var enemy = _architecture.CreateEnemyEntity(Random.Range(30, 80));
                SetRandomPosition(enemy);
                _entities.Add(enemy);
            }

            for (int i = 0; i < _initialStaticCount; i++)
            {
                var staticEntity = _architecture.CreateStaticEntity();
                SetRandomPosition(staticEntity);
                _entities.Add(staticEntity);
            }

            ALog.Info($"[ECSExample] Created {_entities.Count} initial entities");
        }

        private void SetRandomPosition(IEntity entity)
        {
            var pos = entity.GetComponent<PositionComponent>();
            if (pos != null)
            {
                pos.Position = new Vector3(Random.Range(-20f, 20f), 0f, Random.Range(-20f, 20f));
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 500));

            GUILayout.Label(
                "<b>ECS Example</b>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 18 }
            );
            GUILayout.Space(10);

            GUILayout.Label($"Total Entities: {_world?.EntityCount ?? 0}");
            GUILayout.Label($"Tracked Entities: {_entities.Count}");
            GUILayout.Space(10);

            GUILayout.Label(
                "<b>Create Entities:</b>",
                new GUIStyle(GUI.skin.label) { richText = true }
            );

            if (GUILayout.Button("Create Player Entity"))
            {
                var player = _architecture.CreatePlayerEntity();
                _entities.Add(player);
            }

            if (GUILayout.Button("Create Enemy Entity"))
            {
                var enemy = _architecture.CreateEnemyEntity(Random.Range(30, 80));
                SetRandomPosition(enemy);
                _entities.Add(enemy);
            }

            if (GUILayout.Button("Create Moving Entity"))
            {
                var moving = _architecture.CreateMovingEntity();
                SetRandomPosition(moving);
                _entities.Add(moving);
            }

            if (GUILayout.Button("Create Static Entity"))
            {
                var staticEntity = _architecture.CreateStaticEntity();
                SetRandomPosition(staticEntity);
                _entities.Add(staticEntity);
            }

            GUILayout.Space(10);
            GUILayout.Label("<b>Actions:</b>", new GUIStyle(GUI.skin.label) { richText = true });

            if (GUILayout.Button("Damage Random Entity (25)"))
            {
                DamageRandomEntity(25);
            }

            if (GUILayout.Button("Heal Random Entity (50)"))
            {
                HealRandomEntity(50);
            }

            if (GUILayout.Button("Destroy Random Entity"))
            {
                DestroyRandomEntity();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Clear All Entities"))
            {
                ClearAllEntities();
            }

            GUILayout.Space(10);
            GUILayout.Label("<b>Controls:</b>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label("WASD - Move player entities");
            GUILayout.Label("Space - Jump (logged)");
            GUILayout.Label("Left Click - Attack (logged)");

            GUILayout.EndArea();
        }

        private void DamageRandomEntity(int damage)
        {
            var entitiesWithHealth = new List<IEntity>();
            foreach (var entity in _entities)
            {
                if (entity != null && entity.HasComponent<HealthComponent>())
                {
                    var health = entity.GetComponent<HealthComponent>();
                    if (!health.IsDead)
                    {
                        entitiesWithHealth.Add(entity);
                    }
                }
            }

            if (entitiesWithHealth.Count > 0)
            {
                var target = entitiesWithHealth[Random.Range(0, entitiesWithHealth.Count)];
                _healthSystem.ApplyDamage(target, damage);
            }
            else
            {
                ALog.Warn("[ECSExample] No alive entities to damage");
            }
        }

        private void HealRandomEntity(int amount)
        {
            var entitiesWithHealth = new List<IEntity>();
            foreach (var entity in _entities)
            {
                if (entity != null && entity.HasComponent<HealthComponent>())
                {
                    var health = entity.GetComponent<HealthComponent>();
                    if (!health.IsDead && health.CurrentHealth < health.MaxHealth)
                    {
                        entitiesWithHealth.Add(entity);
                    }
                }
            }

            if (entitiesWithHealth.Count > 0)
            {
                var target = entitiesWithHealth[Random.Range(0, entitiesWithHealth.Count)];
                _healthSystem.HealEntity(target, amount);
            }
            else
            {
                ALog.Warn("[ECSExample] No damaged entities to heal");
            }
        }

        private void DestroyRandomEntity()
        {
            var aliveEntities = _entities.FindAll(e => e != null);

            if (aliveEntities.Count > 0)
            {
                var target = aliveEntities[Random.Range(0, aliveEntities.Count)];
                ALog.Info($"[ECSExample] Destroying entity: {target.Id}");
                _world.DestroyEntity(target.Id);
                _entities.Remove(target);
            }
        }

        private void ClearAllEntities()
        {
            foreach (var entity in _entities)
            {
                if (entity != null)
                {
                    _world.DestroyEntity(entity.Id);
                }
            }
            _entities.Clear();
            ALog.Info("[ECSExample] All entities cleared");
        }

        private void OnDestroy()
        {
            _architecture?.Dispose();
            ALog.Info("[ECSExample] ECS Example destroyed");
        }
    }
}
