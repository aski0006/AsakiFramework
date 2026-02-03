using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Utilities.Tools.GroundAligner
{
    public class AsakiGroundAlignerWindow : EditorWindow
    {
        private AsakiGroundAlignerLogic.Dimension _dimension = AsakiGroundAlignerLogic
            .Dimension
            .Mode3D;
        private AsakiGroundAlignerLogic.AlignMode _alignMode = AsakiGroundAlignerLogic
            .AlignMode
            .Bottom; // 默认底部对齐更常用

        // 默认检测所有层，但排除 Trigger 等 (根据实际项目调整)
        private LayerMask _groundLayer = -1;
        private float _offset = 0f;
        private float _maxDistance = 100f;

        // 用于辅助序列化绘制 LayerMask
        private SerializedObject _serializedObject;
        private SerializedProperty _layerMaskProp;

        // 必须定义为 public 字段才能被 SerializedObject 找到
        public LayerMask GroundLayers = -1;

        [MenuItem("Asaki/Tools/Ground Aligner", false, 14)]
        public static void Open()
        {
            AsakiGroundAlignerWindow win = GetWindow<AsakiGroundAlignerWindow>("Ground Aligner");
            win.Show();
        }

        private void OnEnable()
        {
            // 初始化 SerializedObject 以便使用 Unity 原生的 LayerMask 绘制器
            _serializedObject = new SerializedObject(this);
            _layerMaskProp = _serializedObject.FindProperty("GroundLayers");
        }

        private void OnSelectionChange()
        {
            Repaint(); // 选中物体变化时重绘 UI
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawSettings();
            DrawActions();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Asaki Ground Aligner", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "一键将物体垂直吸附到下方地面。\n支持 3D Mesh 和 2D Sprite。",
                MessageType.Info
            );
        }

        private void DrawSettings()
        {
            GUILayout.Label("Configuration", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _dimension = (AsakiGroundAlignerLogic.Dimension)
                    EditorGUILayout.EnumPopup("Dimension", _dimension);
                _alignMode = (AsakiGroundAlignerLogic.AlignMode)
                    EditorGUILayout.EnumPopup("Align Anchor", _alignMode);

                // 绘制 LayerMask
                _serializedObject.Update();
                EditorGUILayout.PropertyField(_layerMaskProp, new GUIContent("Ground Layer"));
                _serializedObject.ApplyModifiedProperties();
                _groundLayer = GroundLayers; // 同步回字段

                _maxDistance = EditorGUILayout.FloatField("Max Ray Dist", _maxDistance);
                _offset = EditorGUILayout.FloatField("Height Offset", _offset);
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.Space();
            GUILayout.Label(
                $"Selected Objects: {Selection.gameObjects.Length}",
                EditorStyles.boldLabel
            );

            GUI.enabled = Selection.gameObjects.Length > 0;

            Color defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.7f, 1f); // Asaki 风格蓝

            if (GUILayout.Button("Align To Ground", GUILayout.Height(40)))
            {
                AsakiGroundAlignerLogic.AlignSelected(
                    _dimension,
                    _groundLayer,
                    _alignMode,
                    _offset,
                    _maxDistance
                );
            }

            GUI.backgroundColor = defaultColor;
            GUI.enabled = true;
        }
    }
}
