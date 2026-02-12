using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Jobs;

namespace Asaki.Editor.Utilities.Tools
{
    public class AsakiQuickLayoutWindow : EditorWindow
    {
        // ==================== 配置结构 ====================

        public enum LayoutMode
        {
            Horizontal,
            Vertical,
            Grid,
            Distribute,
            Stack,
        }

        public enum AlignmentMode
        {
            Start, // 起始对齐
            Center, // 中心对齐
            End, // 末端对齐
            SpaceBetween, // 两端对齐（仅Distribute）
        }

        public enum AnchorMode
        {
            Pivot, // 对齐到轴心
            Bounds, // 对齐到边界（RectTransform有效）
        }

        // ==================== 状态字段 ====================

        private LayoutMode _mode = LayoutMode.Horizontal;
        private AlignmentMode _alignment = AlignmentMode.Start;
        private AnchorMode _anchorMode = AnchorMode.Pivot;
        private Vector3 _spacing = new Vector3(10f, 10f, 0f);
        private int _gridColumns = 3;
        private bool _affectX = true;
        private bool _affectY = true;
        private bool _affectZ = false;

        // 预览系统
        private bool _previewEnabled = false;
        private TransformAccessArray _originalTransforms;
        private Vector3[] _originalPositions;

        // ==================== 窗口入口 ====================

        [MenuItem("Asaki/Tools/Quick Layout %&l", false, 15)] // Ctrl+Alt+L
        public static void ShowWindow()
        {
            AsakiQuickLayoutWindow window = GetWindow<AsakiQuickLayoutWindow>("Quick Layout");
            window.minSize = new Vector2(320, 500);
            window.Show();
        }

        [MenuItem("GameObject/Asaki/Quick Layout", false, 0)] // 右键菜单
        public static void ShowWindowFromHierarchy()
        {
            ShowWindow();
        }

        // ==================== GUI绘制 ====================

        private void OnGUI()
        {
            DrawSelectionInfo();
            EditorGUILayout.Space(10);
            DrawLayoutSettings();
            EditorGUILayout.Space(10);
            DrawAlignmentSettings();
            EditorGUILayout.Space(10);
            DrawPreviewControls();
            EditorGUILayout.Space(20);
            DrawActionButtons();
        }

        private void DrawSelectionInfo()
        {
            EditorGUILayout.LabelField("Selection Status", EditorStyles.boldLabel);
            int count = Selection.transforms.Length;
            EditorGUILayout.LabelField(
                $"Selected Objects: {count}",
                count >= 2 ? EditorStyles.whiteLabel : EditorStyles.helpBox
            );

            if (count < 2)
            {
                EditorGUILayout.HelpBox(
                    "⚠️ Select 2+ objects to enable layout tools",
                    MessageType.Warning
                );
            }
        }

        private void DrawLayoutSettings()
        {
            EditorGUILayout.LabelField("Layout DataTable", EditorStyles.boldLabel);

            // 模式选择（带图标）
            using (new EditorGUILayout.HorizontalScope())
            {
                _mode = (LayoutMode)EditorGUILayout.EnumPopup("Layout Mode", _mode);
                GUILayout.Button(
                    EditorGUIUtility.IconContent("d_align_horizontally_center@2x"),
                    GUILayout.Width(30)
                );
            }

            // 网格专属设置
            if (_mode == LayoutMode.Grid)
            {
                EditorGUI.indentLevel++;
                _gridColumns = EditorGUILayout.IntSlider("Columns", _gridColumns, 1, 12);
                EditorGUI.indentLevel--;
            }

            // 间距设置（三轴折叠）
            EditorGUILayout.LabelField("Spacing (px/unit)", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            _spacing.x = EditorGUILayout.FloatField("Horizontal (X)", _spacing.x);
            _spacing.y = EditorGUILayout.FloatField("Vertical (Y)", _spacing.y);
            _spacing.z = EditorGUILayout.FloatField("Depth (Z)", _spacing.z);
            EditorGUI.indentLevel--;
        }

        private void DrawAlignmentSettings()
        {
            EditorGUILayout.LabelField("Alignment", EditorStyles.boldLabel);

            // 轴向控制（折叠菜单）
            _alignment = (AlignmentMode)EditorGUILayout.EnumPopup("Primary Alignment", _alignment);
            _anchorMode = (AnchorMode)EditorGUILayout.EnumPopup("Anchor Mode", _anchorMode);

            // 轴向开关（支持3D布局）
            EditorGUILayout.LabelField("Active Axes", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;
            _affectX = EditorGUILayout.ToggleLeft("Affect X Axis", _affectX);
            _affectY = EditorGUILayout.ToggleLeft("Affect Y Axis", _affectY);
            _affectZ = EditorGUILayout.ToggleLeft("Affect Z Axis", _affectZ);
            EditorGUI.indentLevel--;
        }

        private void DrawPreviewControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 预览开关
            bool newPreview = EditorGUILayout.ToggleLeft(
                "Enable Realtime Preview",
                _previewEnabled
            );
            if (newPreview != _previewEnabled)
            {
                TogglePreview(newPreview);
            }

            // 实时参数更新
            if (_previewEnabled && HasValidSelection())
            {
                ApplyLayoutPreview(); // 不记录撤销
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActionButtons()
        {
            using (new EditorGUI.DisabledGroupScope(!HasValidSelection()))
            {
                GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
                if (GUILayout.Button("✓ Apply Layout", GUILayout.Height(35)))
                {
                    ApplyLayoutWithUndo();
                    TogglePreview(false);
                }
                GUI.backgroundColor = Color.white;

                if (GUILayout.Button("↶ Cancel Preview", GUILayout.Height(25)))
                {
                    TogglePreview(false);
                }
            }
        }

        // ==================== 预览系统 ====================

        private void TogglePreview(bool enable)
        {
            if (enable && !_previewEnabled)
            {
                StoreOriginalState();
                _previewEnabled = true;
            }
            else if (!enable && _previewEnabled)
            {
                RestoreOriginalState();
                _previewEnabled = false;
            }
        }

        private void StoreOriginalState()
        {
            var transforms = Selection.transforms;
            _originalTransforms = new TransformAccessArray(transforms);
            _originalPositions = transforms.Select(t => t.position).ToArray();
        }

        private void RestoreOriginalState()
        {
            if (
                _originalPositions == null
                || _originalPositions.Length != Selection.transforms.Length
            )
                return;

            for (int i = 0; i < _originalTransforms.length; i++)
            {
                _originalTransforms[i].position = _originalPositions[i];
            }

            _originalTransforms.Dispose();
            _originalPositions = null;
        }

        // ==================== 布局算法 ====================

        private void ApplyLayoutPreview()
        {
            ApplyLayoutInternal(recordUndo: false);
        }

        private void ApplyLayoutWithUndo()
        {
            Undo.RecordObjects(
                Selection.transforms.Cast<Object>().ToArray(),
                $"Quick Layout ({_mode})"
            );
            ApplyLayoutInternal(recordUndo: true);
        }

        private void ApplyLayoutInternal(bool recordUndo)
        {
            var transforms = Selection.transforms.OrderBy(t => t.GetSiblingIndex()).ToArray();
            if (transforms.Length < 2)
                return;

            switch (_mode)
            {
                case LayoutMode.Horizontal:
                    ApplyHorizontalLayout(transforms);
                    break;
                case LayoutMode.Vertical:
                    ApplyVerticalLayout(transforms);
                    break;
                case LayoutMode.Grid:
                    ApplyGridLayout(transforms);
                    break;
                case LayoutMode.Distribute:
                    ApplyDistributeLayout(transforms);
                    break;
                case LayoutMode.Stack:
                    ApplyStackLayout(transforms);
                    break;
            }
        }

        private void ApplyHorizontalLayout(Transform[] transforms)
        {
            Vector3 start = GetStartPosition(transforms, Axis.X);
            float accumulated = 0f;

            foreach (Transform t in transforms)
            {
                Vector3 pos = t.position;
                if (_affectX)
                    pos.x = start.x + accumulated + GetPivotOffset(t, Axis.X);
                if (_affectY)
                    pos.y = start.y;
                if (_affectZ)
                    pos.z = start.z;

                t.position = pos;
                accumulated += GetSpacingFor(Axis.X) + GetBoundsSize(t, Axis.X);
            }
        }

        private void ApplyVerticalLayout(Transform[] transforms)
        {
            Vector3 start = GetStartPosition(transforms, Axis.Y);
            float accumulated = 0f;

            foreach (Transform t in transforms)
            {
                Vector3 pos = t.position;
                if (_affectX)
                    pos.x = start.x;
                if (_affectY)
                    pos.y = start.y + accumulated + GetPivotOffset(t, Axis.Y);
                if (_affectZ)
                    pos.z = start.z;

                t.position = pos;
                accumulated += GetSpacingFor(Axis.Y) + GetBoundsSize(t, Axis.Y);
            }
        }

        private void ApplyGridLayout(Transform[] transforms)
        {
            Vector3 start = transforms[0].position;
            float cellWidth = GetAverageSize(transforms, Axis.X) + GetSpacingFor(Axis.X);
            float cellHeight = GetAverageSize(transforms, Axis.Y) + GetSpacingFor(Axis.Y);

            for (int i = 0; i < transforms.Length; i++)
            {
                int row = i / _gridColumns;
                int col = i % _gridColumns;

                Vector3 pos = start;
                if (_affectX)
                    pos.x += col * cellWidth;
                if (_affectY)
                    pos.y -= row * cellHeight; // Y轴向下为负

                transforms[i].position = pos;
            }
        }

        private void ApplyDistributeLayout(Transform[] transforms)
        {
            if (transforms.Length < 3)
                return;

            Vector3 first = transforms[0].position;
            Vector3 last = transforms[^1].position;

            // 计算总距离（仅激活轴）
            Vector3 totalDistance = last - first;
            if (!_affectX)
                totalDistance.x = 0;
            if (!_affectY)
                totalDistance.y = 0;
            if (!_affectZ)
                totalDistance.z = 0;

            float totalLength = GetActiveAxisDistance(totalDistance);
            float spacing = totalLength / (transforms.Length - 1);

            for (int i = 1; i < transforms.Length - 1; i++)
            {
                float t = (float)i / (transforms.Length - 1);
                Vector3 targetPos = Vector3.Lerp(first, last, t);

                Vector3 pos = transforms[i].position;
                if (_affectX)
                    pos.x = targetPos.x;
                if (_affectY)
                    pos.y = targetPos.y;
                if (_affectZ)
                    pos.z = targetPos.z;

                transforms[i].position = pos;
            }
        }

        private void ApplyStackLayout(Transform[] transforms)
        {
            Vector3 center = CalculateCenter(transforms);

            foreach (Transform t in transforms)
            {
                Vector3 pos = t.position;
                if (_affectX)
                    pos.x = center.x;
                if (_affectY)
                    pos.y = center.y;
                if (_affectZ)
                    pos.z = center.z;

                t.position = pos;
            }
        }

        // ==================== 辅助计算 ====================

        private enum Axis
        {
            X,
            Y,
            Z,
        }

        private Vector3 GetStartPosition(Transform[] transforms, Axis axis)
        {
            return _alignment switch
            {
                AlignmentMode.Start => transforms[0].position,
                AlignmentMode.Center => CalculateCenter(transforms),
                AlignmentMode.End => transforms[^1].position,
                _ => transforms[0].position,
            };
        }

        private float GetSpacingFor(Axis axis)
        {
            return axis switch
            {
                Axis.X => _spacing.x,
                Axis.Y => _spacing.y,
                Axis.Z => _spacing.z,
                _ => 0,
            };
        }

        private Vector3 CalculateCenter(Transform[] transforms)
        {
            Vector3 sum = Vector3.zero;
            foreach (Transform t in transforms)
                sum += t.position;
            return sum / transforms.Length;
        }

        private float GetActiveAxisDistance(Vector3 vector)
        {
            Vector3 v = vector;
            if (!_affectX)
                v.x = 0;
            if (!_affectY)
                v.y = 0;
            if (!_affectZ)
                v.z = 0;
            return v.magnitude;
        }

        private float GetPivotOffset(Transform t, Axis axis)
        {
            if (
                _anchorMode != AnchorMode.Pivot
                || !t.TryGetComponent<RectTransform>(out RectTransform rect)
            )
                return 0;

            return axis switch
            {
                Axis.X => rect.rect.width * (0.5f - rect.pivot.x),
                Axis.Y => rect.rect.height * (0.5f - rect.pivot.y),
                _ => 0,
            };
        }

        private float GetBoundsSize(Transform t, Axis axis)
        {
            if (t.TryGetComponent<RectTransform>(out RectTransform rect))
                return axis == Axis.X ? rect.rect.width : rect.rect.height;

            if (t.TryGetComponent<Renderer>(out Renderer renderer))
                return axis == Axis.X ? renderer.bounds.size.x : renderer.bounds.size.y;

            return 100f; // 默认值
        }

        private float GetAverageSize(Transform[] transforms, Axis axis)
        {
            return transforms.Average(t => GetBoundsSize(t, axis));
        }

        private bool HasValidSelection()
        {
            return Selection.transforms.Length >= 2;
        }

        private void OnSelectionChange()
        {
            Repaint();
        }
    }
}
