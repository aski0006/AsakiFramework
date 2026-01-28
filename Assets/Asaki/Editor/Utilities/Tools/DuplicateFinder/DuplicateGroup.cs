using System.Collections.Generic;
using UnityEngine;

namespace Asaki.Editor.Utilities.Tools.DuplicateFinder
{
    public class DuplicateGroup
    {
        public GameObject Original;
        public List<GameObject> Duplicates = new List<GameObject>();
        public Bounds WorldBounds; // 用于绘制高光
    }

    public static class AsakiDuplicateFinderLogic
    {
        /// <summary>
        /// 查找重复物体
        /// 判定标准：位置极度接近 且 使用相同的 Mesh/Sprite
        /// </summary>
        public static List<DuplicateGroup> FindDuplicates(float positionTolerance = 0.01f)
        {
            var results = new List<DuplicateGroup>();

            // 1. 获取场景中所有有效的渲染器 (Mesh 或 Sprite)
            var allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var processed = new HashSet<Renderer>();

            for (int i = 0; i < allRenderers.Length; i++)
            {
                Renderer rendererA = allRenderers[i];
                if (processed.Contains(rendererA))
                    continue;
                if (rendererA == null)
                    continue;

                DuplicateGroup currentGroup = new DuplicateGroup();
                currentGroup.Original = rendererA.gameObject;
                currentGroup.WorldBounds = rendererA.bounds;

                // 2. 内层循环寻找匹配项
                for (int j = i + 1; j < allRenderers.Length; j++)
                {
                    Renderer rendererB = allRenderers[j];
                    if (processed.Contains(rendererB))
                        continue;

                    // 快速排斥：距离检查
                    if (
                        Vector3.SqrMagnitude(
                            rendererA.transform.position - rendererB.transform.position
                        )
                        > positionTolerance * positionTolerance
                    )
                        continue;

                    // 深度检查：资源是否一致
                    if (IsSameAsset(rendererA, rendererB))
                    {
                        currentGroup.Duplicates.Add(rendererB.gameObject);
                        processed.Add(rendererB); // 标记已处理
                    }
                }

                // 如果找到了副本，记录该组
                if (currentGroup.Duplicates.Count > 0)
                {
                    results.Add(currentGroup);
                    processed.Add(rendererA);
                }
            }

            return results;
        }

        private static bool IsSameAsset(Renderer r1, Renderer r2)
        {
            // Case 1: MeshRenderer
            if (r1 is MeshRenderer && r2 is MeshRenderer)
            {
                MeshFilter f1 = r1.GetComponent<MeshFilter>();
                MeshFilter f2 = r2.GetComponent<MeshFilter>();
                if (f1 && f2 && f1.sharedMesh == f2.sharedMesh)
                    return true;
            }
            // Case 2: SpriteRenderer
            else if (r1 is SpriteRenderer s1 && r2 is SpriteRenderer s2)
            {
                if (s1.sprite == s2.sprite)
                    return true;
            }
            // Case 3: SkinnedMeshRenderer (忽略骨骼动画状态，仅看Mesh)
            else if (r1 is SkinnedMeshRenderer sk1 && r2 is SkinnedMeshRenderer sk2)
            {
                if (sk1.sharedMesh == sk2.sharedMesh)
                    return true;
            }

            return false;
        }
    }
}
