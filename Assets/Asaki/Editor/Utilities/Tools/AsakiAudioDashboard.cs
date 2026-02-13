using System.IO;
using System.Linq;
using System.Text;
using Asaki.Core.FrameworkSettings;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Utilities.Tools
{
    public class AsakiAudioDashboard : EditorWindow
    {
        // === 路径配置 ===
        // [修改] 指向唯一的 AsakiFrameworkSetting
        private const string CONFIG_PATH =
            "Assets/Resources/Asaki/DataTable/AsakiFrameworkSetting.asset";
        private const string CODE_GEN_PATH =
            "Assets/Asaki/Generated/AudioAsset_2_Id/AudioAssetID.cs";

        // [修改] 持有主配置
        private AsakiFrameworkSetting _mainFrameworkSetting;

        // 快捷访问属性
        private AsakiAudioConfig _audioConfig => _mainFrameworkSetting?.AudioConfig;

        private Vector2 _scrollPos;
        private string _searchFilter = "";

        // === 播放器状态 ===
        private AudioSource _previewSource;
        private AudioItem _currentPlayingItem;
        private bool _isPaused;

        [MenuItem("Asaki/Window/Audio Dashboard", false, 40)]
        public static void ShowWindow()
        {
            AsakiAudioDashboard wnd = GetWindow<AsakiAudioDashboard>("Audio Dashboard");
            wnd.minSize = new Vector2(850, 600);
            wnd.Show();
        }

        private void OnEnable()
        {
            LoadConfig();
            InitAudioSource();
        }

        private void OnDisable()
        {
            StopPreview();
            if (_previewSource != null)
                DestroyImmediate(_previewSource.gameObject);
        }

        private void InitAudioSource()
        {
            if (_previewSource == null)
            {
                GameObject go = EditorUtility.CreateGameObjectWithHideFlags(
                    "AsakiAudioPreview",
                    HideFlags.HideAndDontSave,
                    typeof(AudioSource)
                );
                _previewSource = go.GetComponent<AudioSource>();
            }
        }

        private void OnGUI()
        {
            DrawHeader();

            if (_mainFrameworkSetting == null)
            {
                DrawEmptyState();
                return;
            }

            DrawToolbar();
            DrawDropArea();
            DrawList();
            DrawFooter();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Asaki Audio Dashboard V5.2 (Centralized)", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Force Refresh", EditorStyles.toolbarButton))
                LoadConfig();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEmptyState()
        {
            EditorGUILayout.HelpBox(
                $"AsakiFrameworkSetting not found at: {CONFIG_PATH}",
                MessageType.Error
            );
            if (GUILayout.Button("Create Config Asset"))
                CreateConfigAsset();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 搜索
            GUILayout.Label("Search:", EditorStyles.miniLabel, GUILayout.Width(45));
            _searchFilter = EditorGUILayout.TextField(
                _searchFilter,
                EditorStyles.toolbarSearchField,
                GUILayout.Width(200)
            );
            if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(20)))
                _searchFilter = "";

            GUILayout.FlexibleSpace();

            // 排序
            if (GUILayout.Button("Sort by Name", EditorStyles.toolbarButton))
            {
                // [修改] 记录主配置对象
                Undo.RecordObject(_mainFrameworkSetting, "Sort Audio Items");
                _audioConfig.Items = _audioConfig.Items.OrderBy(x => x.Key).ToList();
                EditorUtility.SetDirty(_mainFrameworkSetting);
            }

            if (GUILayout.Button("■ Stop All", EditorStyles.toolbarButton))
            {
                StopPreview();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDropArea()
        {
            Event evt = Event.current;
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 40.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag AudioClips Here (Batch Add)", EditorStyles.helpBox);

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                if (!dropArea.Contains(evt.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    // [修改] 记录主配置对象
                    Undo.RecordObject(_mainFrameworkSetting, "Add Audio Clips");
                    foreach (Object draggedObj in DragAndDrop.objectReferences)
                    {
                        if (draggedObj is AudioClip clip)
                            RegisterOrUpdateClip(clip);
                    }
                    EditorUtility.SetDirty(_mainFrameworkSetting);
                }
                Event.current.Use();
            }
        }

        private void DrawList()
        {
            if (_audioConfig == null || _audioConfig.Items == null)
                return;

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            for (int i = 0; i < _audioConfig.Items.Count; i++)
            {
                AudioItem item = _audioConfig.Items[i];
                if (
                    !string.IsNullOrEmpty(_searchFilter)
                    && !item.Key.ToLower().Contains(_searchFilter.ToLower())
                )
                    continue;

                DrawAudioItem(item, i);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawAudioItem(AudioItem item, int index)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            DrawTransportControls(item);

            Color defaultColor = GUI.color;
            if (string.IsNullOrEmpty(item.Key))
                GUI.color = Color.red;

            EditorGUI.BeginChangeCheck();
            string newKey = EditorGUILayout.TextField(item.Key, GUILayout.Width(180));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_mainFrameworkSetting, "Rename Key");
                item.Key = newKey;
            }
            GUI.color = defaultColor;

            EditorGUI.BeginChangeCheck();
            AudioClip newClip = (AudioClip)
                EditorGUILayout.ObjectField(item.Clip, typeof(AudioClip), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_mainFrameworkSetting, "Change Clip");
                item.Clip = newClip;
                if (newClip != null)
                    item.AssetPath = GetLoadPath(newClip);
            }

            EditorGUI.BeginChangeCheck();
            AsakiAudioGroup newGroup = (AsakiAudioGroup)
                EditorGUILayout.EnumPopup(item.Group, GUILayout.Width(60));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_mainFrameworkSetting, "Change Group");
                item.Group = newGroup;
            }

            string durationStr = item.Clip ? FormatDuration(item.Clip.length) : "--:--";
            GUILayout.Label(durationStr, EditorStyles.miniLabel, GUILayout.Width(35));

            item._editorExpanded = EditorGUILayout.Foldout(item._editorExpanded, "Edit", true);

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                StopPreview();
                Undo.RecordObject(_mainFrameworkSetting, "Remove Item");
                _audioConfig.Items.RemoveAt(index);
                EditorUtility.SetDirty(_mainFrameworkSetting);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.EndHorizontal();

            if (item._editorExpanded)
            {
                DrawDetailRow(item);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTransportControls(AudioItem item)
        {
            bool isCurrent = _currentPlayingItem == item;
            bool isSourcePlaying = _previewSource != null && _previewSource.isPlaying;

            if (isCurrent)
            {
                if (isSourcePlaying)
                {
                    if (
                        GUILayout.Button(
                            EditorGUIUtility.IconContent("PauseButton"),
                            GUILayout.Width(25),
                            GUILayout.Height(20)
                        )
                    )
                        PausePreview();
                }
                else if (_isPaused)
                {
                    if (
                        GUILayout.Button(
                            EditorGUIUtility.IconContent("PlayButton"),
                            GUILayout.Width(25),
                            GUILayout.Height(20)
                        )
                    )
                        ResumePreview();
                }
                else
                {
                    if (
                        GUILayout.Button(
                            EditorGUIUtility.IconContent("PlayButton"),
                            GUILayout.Width(25),
                            GUILayout.Height(20)
                        )
                    )
                        PlayPreview(item);
                }

                if (
                    GUILayout.Button(
                        EditorGUIUtility.IconContent("PreMatQuad"),
                        GUILayout.Width(25),
                        GUILayout.Height(20)
                    )
                )
                    StopPreview();
            }
            else
            {
                if (
                    GUILayout.Button(
                        EditorGUIUtility.IconContent("PlayButton"),
                        GUILayout.Width(25),
                        GUILayout.Height(20)
                    )
                )
                    PlayPreview(item);
                GUILayout.Space(29);
            }
        }

        private void DrawDetailRow(AudioItem item)
        {
            EditorGUI.indentLevel++;

            // ========================================
            // Volume & Pitch
            // ========================================
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("Vol", GUILayout.Width(25));
            float newVol = EditorGUILayout.Slider(item.Volume, 0f, 1f, GUILayout.Width(120));
            if (newVol != item.Volume)
            {
                Undo.RecordObject(_mainFrameworkSetting, "Change Volume");
                item.Volume = newVol;
                if (_currentPlayingItem == item && _previewSource != null)
                    _previewSource.volume = newVol;
            }

            GUILayout.Space(10);

            GUILayout.Label("Pitch", GUILayout.Width(35));
            float newPitch = EditorGUILayout.Slider(item.Pitch, 0.1f, 3f, GUILayout.Width(120));
            if (newPitch != item.Pitch)
            {
                Undo.RecordObject(_mainFrameworkSetting, "Change Pitch");
                item.Pitch = newPitch;
                if (_currentPlayingItem == item && _previewSource != null)
                    _previewSource.pitch = newPitch;
            }

            EditorGUILayout.EndHorizontal();

            // ========================================
            // Loop & Random Pitch
            // ========================================
            EditorGUILayout.BeginHorizontal();

            bool newLoop = EditorGUILayout.ToggleLeft("Loop", item.Loop, GUILayout.Width(50));
            if (newLoop != item.Loop)
            {
                Undo.RecordObject(_mainFrameworkSetting, "Change Loop");
                item.Loop = newLoop;
            }

            bool newRnd = EditorGUILayout.ToggleLeft(
                "Rnd Pitch",
                item.RandomPitch,
                GUILayout.Width(80)
            );
            if (newRnd != item.RandomPitch)
            {
                Undo.RecordObject(_mainFrameworkSetting, "Change RndPitch");
                item.RandomPitch = newRnd;
            }

            EditorGUILayout.EndHorizontal();

            // ========================================
            // ✅ 新增：3D Audio Settings
            // ========================================
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("Spatial", GUILayout.Width(45));
            float newSpatial = EditorGUILayout.Slider(
                item.SpatialBlend,
                0f,
                1f,
                GUILayout.Width(120)
            );
            if (newSpatial != item.SpatialBlend)
            {
                Undo.RecordObject(_mainFrameworkSetting, "Change SpatialBlend");
                item.SpatialBlend = newSpatial;
            }

            GUILayout.Space(10);

            GUILayout.Label("Priority", GUILayout.Width(50));
            int newPriority = EditorGUILayout.IntSlider(
                item.Priority,
                0,
                256,
                GUILayout.Width(120)
            );
            if (newPriority != item.Priority)
            {
                Undo.RecordObject(_mainFrameworkSetting, "Change Priority");
                item.Priority = newPriority;
            }

            EditorGUILayout.EndHorizontal();

            // ========================================
            // ✅ 新增：3D Position (仅当 SpatialBlend > 0 时显示)
            // ========================================
            if (item.SpatialBlend > 0)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Position", GUILayout.Width(60));

                Vector3 newPos = EditorGUILayout.Vector3Field("", item.Position);
                if (newPos != item.Position)
                {
                    Undo.RecordObject(_mainFrameworkSetting, "Change Position");
                    item.Position = newPos;
                }

                EditorGUILayout.EndHorizontal();
            }

            // ========================================
            // Asset Path (只读)
            // ========================================
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = false;
            EditorGUILayout.LabelField("Asset Path:", GUILayout.Width(70));
            EditorGUILayout.TextField(item.AssetPath);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        private void DrawFooter()
        {
            GUILayout.Space(10);
            GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);
            if (GUILayout.Button("Sync & Generate Code", GUILayout.Height(40)))
            {
                SyncAndGenerate();
            }
            GUI.backgroundColor = Color.white;
        }

        private void LoadConfig()
        {
            // [修改] 加载主配置
            _mainFrameworkSetting = AssetDatabase.LoadAssetAtPath<AsakiFrameworkSetting>(
                CONFIG_PATH
            );
        }

        private void CreateConfigAsset()
        {
            string dir = Path.GetDirectoryName(CONFIG_PATH);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            _mainFrameworkSetting = CreateInstance<AsakiFrameworkSetting>();
            AssetDatabase.CreateAsset(_mainFrameworkSetting, CONFIG_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void RegisterOrUpdateClip(AudioClip clip)
        {
            if (_audioConfig.Items.Any(x => x.Clip == clip))
                return;
            string key = SanitizeName(clip.name);
            AudioItem existing = _audioConfig.Items.FirstOrDefault(x => x.Key == key);

            if (existing != null)
            {
                existing.Clip = clip;
                existing.AssetPath = GetLoadPath(clip);
                return;
            }

            AudioItem newItem = new AudioItem
            {
                Clip = clip,
                Key = key,
                Volume = 1.0f,
                Pitch = 1.0f,
                Group = AsakiAudioGroup.SFX,
                AssetPath = GetLoadPath(clip),
            };

            string lowerName = newItem.Key.ToLower();
            if (lowerName.Contains("bgm") || lowerName.Contains("music"))
            {
                newItem.Group = AsakiAudioGroup.BGM;
                newItem.Loop = true;
            }
            else if (lowerName.Contains("ui") || lowerName.Contains("click"))
            {
                newItem.Group = AsakiAudioGroup.UI;
            }

            _audioConfig.Items.Add(newItem);
        }

        private void PlayPreview(AudioItem item)
        {
            if (item.Clip == null)
                return;
            InitAudioSource();
            _previewSource.Stop();
            _previewSource.clip = item.Clip;
            _previewSource.volume = item.Volume;
            _previewSource.pitch = item.Pitch;
            _previewSource.Play();
            _currentPlayingItem = item;
            _isPaused = false;
        }

        private void PausePreview()
        {
            if (_previewSource != null && _previewSource.isPlaying)
            {
                _previewSource.Pause();
                _isPaused = true;
            }
        }

        private void ResumePreview()
        {
            if (_previewSource != null && _isPaused)
            {
                _previewSource.UnPause();
                _isPaused = false;
            }
        }

        private void StopPreview()
        {
            if (_previewSource != null)
            {
                _previewSource.Stop();
                _previewSource.clip = null;
            }
            _currentPlayingItem = null;
            _isPaused = false;
        }

        private string FormatDuration(float seconds)
        {
            System.TimeSpan ts = System.TimeSpan.FromSeconds(seconds);
            return $"{ts.Minutes:D1}:{ts.Seconds:D2}";
        }

        private string GetLoadPath(AudioClip clip)
        {
            string rawPath = AssetDatabase.GetAssetPath(clip);
            if (rawPath.Contains("/Resources/"))
            {
                string ext = Path.GetExtension(rawPath);
                int resIndex = rawPath.IndexOf("/Resources/") + 11;
                return rawPath.Substring(resIndex).Replace(ext, "");
            }
            return rawPath;
        }

        private string SanitizeName(string rawName)
        {
            string name = rawName.Replace(" ", "_").Replace("-", "_").Replace(".", "_");
            char[] arr = name.ToCharArray();
            for (int i = 0; i < arr.Length; i++)
            {
                if (!char.IsLetterOrDigit(arr[i]) && arr[i] != '_')
                    arr[i] = '_';
            }
            name = new string(arr);
            if (char.IsDigit(name[0]))
                name = "Snd_" + name;
            return name;
        }

        private void SyncAndGenerate()
        {
            if (_mainFrameworkSetting == null || _audioConfig == null)
                return;

            foreach (AudioItem item in _audioConfig.Items)
            {
                if (string.IsNullOrEmpty(item.Key))
                    item.Key = "Unnamed_" + System.Guid.NewGuid().ToString().Substring(0, 4);
                item.ID = Animator.StringToHash(item.Key);
                if (item.Clip != null)
                    item.AssetPath = GetLoadPath(item.Clip);
            }

            EditorUtility.SetDirty(_mainFrameworkSetting);
            AssetDatabase.SaveAssets();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("// Generated by AsakiAudioDashboard.");
            sb.AppendLine(
                "// Audio configurations are stored in AudioItem and applied automatically."
            );
            sb.AppendLine("using Asaki.Core;");
            sb.AppendLine("using Asaki.Core.Audio;");
            sb.AppendLine("using System.Threading;");
            sb.AppendLine();
            sb.AppendLine("namespace Asaki.Generated");
            sb.AppendLine("{");
            sb.AppendLine("    public enum AudioAssetID");
            sb.AppendLine("    {");
            sb.AppendLine("        None = 0,");
            foreach (AudioItem item in _audioConfig.Items.OrderBy(x => x.Key))
            {
                // ✅ 添加注释显示配置
                sb.AppendLine(
                    $"        /// <summary>Vol:{item.Volume:F2}, Pitch:{item.Pitch:F2}, Loop:{item.Loop}</summary>"
                );
                sb.AppendLine($"        {item.Key} = {item.ID},");
            }
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    public static class AudioExtensions");
            sb.AppendLine("    {");
            sb.AppendLine("        /// <summary>");
            sb.AppendLine(
                "        /// Play audio with default parameters from AudioItem configuration"
            );
            sb.AppendLine("        /// </summary>");
            sb.AppendLine(
                "        public static AsakiAudioHandle Play(this IAsakiAudioService service, AudioAssetID id)"
            );
            sb.AppendLine("        {");
            sb.AppendLine("            return service.Play((int)id, default, default);");
            sb.AppendLine("        }");
            sb.AppendLine();

            sb.AppendLine("        /// <summary>");
            sb.AppendLine(
                "        /// Play audio with custom parameters (merged with AudioItem configuration)"
            );
            sb.AppendLine("        /// </summary>");
            sb.AppendLine(
                "        public static AsakiAudioHandle Play(this IAsakiAudioService service, AudioAssetID id, AsakiAudioParams p)"
            );
            sb.AppendLine("        {");
            sb.AppendLine("            return service.Play((int)id, p, default);");
            sb.AppendLine("        }");
            sb.AppendLine();

            sb.AppendLine(
                "        public static AsakiAudioHandle Play(this IAsakiAudioService service, AudioAssetID id, AsakiAudioParams p, CancellationToken token)"
            );
            sb.AppendLine("        {");
            sb.AppendLine("            return service.Play((int)id, p, token);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            string dir = Path.GetDirectoryName(CODE_GEN_PATH);
            if (!Directory.Exists(dir))
                if (dir != null)
                    Directory.CreateDirectory(dir);
            File.WriteAllText(CODE_GEN_PATH, sb.ToString(), Encoding.UTF8);

            AssetDatabase.Refresh();
            Debug.Log($"[AsakiAudio] Synced {_audioConfig.Items.Count} clips.");
        }
    }
}
