using System.Collections.Generic;

namespace Asaki.Core.Architecture.Entities
{
    /// <summary>
    /// 多标签组件 - 一个组件支持多个标签
    /// </summary>
    public class TagsComponent : EntityComponent
    {
        private readonly HashSet<string> _tags = new();

        /// <summary>
        /// 添加标签
        /// </summary>
        public void AddTag(string tag)
        {
            _tags.Add(tag);
        }

        /// <summary>
        /// 移除标签
        /// </summary>
        public bool RemoveTag(string tag)
        {
            return _tags.Remove(tag);
        }

        /// <summary>
        /// 检查是否有标签
        /// </summary>
        public bool HasTag(string tag)
        {
            return _tags.Contains(tag);
        }

        /// <summary>
        /// 检查是否有任意指定标签
        /// </summary>
        public bool HasAnyTag(params string[] tags)
        {
            foreach (var tag in tags)
            {
                if (_tags.Contains(tag))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 检查是否有所有指定标签
        /// </summary>
        public bool HasAllTags(params string[] tags)
        {
            foreach (var tag in tags)
            {
                if (!_tags.Contains(tag))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 获取所有标签
        /// </summary>
        public IEnumerable<string> GetTags()
        {
            return _tags;
        }

        /// <summary>
        /// 标签数量
        /// </summary>
        public int TagCount => _tags.Count;

        /// <summary>
        /// 清空所有标签
        /// </summary>
        public void Clear()
        {
            _tags.Clear();
        }
    }

    /// <summary>
    /// 标签查询扩展
    /// </summary>
    public static class TagQueryExtensions
    {
        /// <summary>
        /// 查询具有指定标签的实体
        /// </summary>
        public static IEnumerable<IEntity> QueryByTag(this IEntityWorld world, string tag)
        {
            foreach (var entity in world.GetAllEntities())
            {
                if (entity.TryGetComponent<TagsComponent>(out var tags) && tags.HasTag(tag))
                {
                    yield return entity;
                }
            }
        }

        /// <summary>
        /// 查询具有任意指定标签的实体
        /// </summary>
        public static IEnumerable<IEntity> QueryByAnyTag(this IEntityWorld world, params string[] tags)
        {
            foreach (var entity in world.GetAllEntities())
            {
                if (entity.TryGetComponent<TagsComponent>(out var tagsComp) && tagsComp.HasAnyTag(tags))
                {
                    yield return entity;
                }
            }
        }

        /// <summary>
        /// 查询具有所有指定标签的实体
        /// </summary>
        public static IEnumerable<IEntity> QueryByAllTags(this IEntityWorld world, params string[] tags)
        {
            foreach (var entity in world.GetAllEntities())
            {
                if (entity.TryGetComponent<TagsComponent>(out var tagsComp) && tagsComp.HasAllTags(tags))
                {
                    yield return entity;
                }
            }
        }

        /// <summary>
        /// 添加标签（便捷方法）
        /// </summary>
        public static void AddTag(this IEntity entity, string tag)
        {
            if (!entity.TryGetComponent<TagsComponent>(out var tags))
            {
                tags = entity.AddComponent<TagsComponent>();
            }
            tags.AddTag(tag);
        }

        /// <summary>
        /// 移除标签（便捷方法）
        /// </summary>
        public static bool RemoveTag(this IEntity entity, string tag)
        {
            if (entity.TryGetComponent<TagsComponent>(out var tags))
            {
                return tags.RemoveTag(tag);
            }
            return false;
        }

        /// <summary>
        /// 检查标签（便捷方法）
        /// </summary>
        public static bool HasTag(this IEntity entity, string tag)
        {
            return entity.TryGetComponent<TagsComponent>(out var tags) && tags.HasTag(tag);
        }

        /// <summary>
        /// 检查是否有任意标签（便捷方法）
        /// </summary>
        public static bool HasAnyTag(this IEntity entity, params string[] tags)
        {
            return entity.TryGetComponent<TagsComponent>(out var comp) && comp.HasAnyTag(tags);
        }

        /// <summary>
        /// 检查是否有所有标签（便捷方法）
        /// </summary>
        public static bool HasAllTags(this IEntity entity, params string[] tags)
        {
            return entity.TryGetComponent<TagsComponent>(out var comp) && comp.HasAllTags(tags);
        }
    }
}
