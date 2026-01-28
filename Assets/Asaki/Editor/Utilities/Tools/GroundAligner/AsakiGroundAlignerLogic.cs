using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Utilities.Tools.GroundAligner
{
    public static class AsakiGroundAlignerLogic
    {
        public enum AlignMode
        {
            Pivot,
            Bottom,
        }

        public enum Dimension
        {
            Mode3D,
            Mode2D,
        }

        public static void AlignSelected(
            Dimension dimension,
            LayerMask groundLayer,
            AlignMode alignMode,
            float offset,
            float maxDistance = 100f
        )
        {
            var selectedObjects = Selection.gameObjects;
            if (selectedObjects.Length == 0)
                return;

            // 开启撤销组
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Asaki Ground Align");
            int undoGroup = Undo.GetCurrentGroup();

            int successCount = 0;

            foreach (GameObject obj in selectedObjects)
            {
                if (obj == null)
                    continue;

                bool hitSuccess = false;
                Vector3 hitPoint = Vector3.zero;

                // 1. 射线检测
                if (dimension == Dimension.Mode3D)
                {
                    // 稍微向上抬一点起点，防止从地面以下开始检测
                    Vector3 startPos = obj.transform.position + Vector3.up * 1.0f;
                    if (
                        Physics.Raycast(
                            startPos,
                            Vector3.down,
                            out RaycastHit hit,
                            maxDistance,
                            groundLayer
                        )
                    )
                    {
                        hitSuccess = true;
                        hitPoint = hit.point;
                    }
                }
                else // 2D Mode
                {
                    Vector2 startPos = obj.transform.position + Vector3.up * 1.0f;
                    RaycastHit2D hit = Physics2D.Raycast(
                        startPos,
                        Vector2.down,
                        maxDistance,
                        groundLayer
                    );
                    if (hit.collider != null)
                    {
                        // 避免检测到自己 (如果自己也在 Ground 层)
                        if (hit.collider.gameObject != obj)
                        {
                            hitSuccess = true;
                            hitPoint = hit.point;
                        }
                    }
                }

                // 2. 执行对齐
                if (hitSuccess)
                {
                    Undo.RecordObject(obj.transform, "Align Move");

                    float finalY = hitPoint.y + offset;

                    // 如果是底部对齐，需要计算 Pivot 到 Bottom 的距离
                    if (alignMode == AlignMode.Bottom)
                    {
                        float distPivotToBottom = GetPivotToBottomDistance(obj, dimension);
                        finalY += distPivotToBottom;
                    }

                    Vector3 newPos = obj.transform.position;
                    newPos.y = finalY;
                    obj.transform.position = newPos;

                    successCount++;
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log(
                $"<color=#4CAF50>[Asaki Aligner]</color> 已对齐 {successCount}/{selectedObjects.Length} 个对象。"
            );
        }

        /// <summary>
        /// 计算 Pivot 到包围盒底部的距离（正值）
        /// </summary>
        private static float GetPivotToBottomDistance(GameObject obj, Dimension dim)
        {
            Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
            bool hasBounds = false;

            if (dim == Dimension.Mode3D)
            {
                Renderer renderer = obj.GetComponentInChildren<Renderer>();
                Collider collider = obj.GetComponentInChildren<Collider>();
                if (renderer != null)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else if (collider != null)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
            }
            else
            {
                SpriteRenderer renderer = obj.GetComponentInChildren<SpriteRenderer>();
                Collider2D collider = obj.GetComponentInChildren<Collider2D>();
                if (renderer != null)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else if (collider != null)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
            }

            if (!hasBounds)
                return 0f;

            // Pivot Y - Bounds Bottom Y
            return obj.transform.position.y - bounds.min.y;
        }
    }
}
