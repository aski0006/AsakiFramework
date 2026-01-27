#if UNITY_EDITOR
using Asaki.Core.Blackboard;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Profiler
{
	public class BlackboardProfilerWindow : EditorWindow
	{
		private Vector2 _scrollPos;
		private bool _isRecording = false;

		[MenuItem("Asaki/Diagnostics/Blackboard Profiler")]
		public static void ShowWindow()
		{
			GetWindow<BlackboardProfilerWindow>("BB Profiler");
		}

		private void OnGUI()
		{
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button(_isRecording ? "Stop Recording" : "Start Recording"))
			{
				_isRecording = !_isRecording;
				if (_isRecording)
					BlackboardProfiler.Enable();
				else
					BlackboardProfiler.Disable();
			}

			if (GUILayout.Button("Clear Data"))
			{
				BlackboardProfiler.Disable();
				BlackboardProfiler.Enable();
			}

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Access Statistics", EditorStyles.boldLabel);

			_scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

			var stats = BlackboardProfiler.GetStats();
			if (stats.Count == 0)
			{
				EditorGUILayout.HelpBox("No data collected.  Start recording and run your graph.", MessageType.Info);
			}
			else
			{
				var sorted = stats.OrderByDescending(kvp => kvp.Value.AccessCount);

				foreach (var kvp in sorted)
				{
					BlackboardProfiler.ProfileData data = kvp.Value;
					float hitRate = data.AccessCount > 0
						? (float)data.HashHitCount / data.AccessCount * 100f
						: 0f;

					EditorGUILayout.BeginVertical("box");
					EditorGUILayout.LabelField($"Key: {kvp.Key}", EditorStyles.boldLabel);
					EditorGUILayout.LabelField($"Total Access: {data.AccessCount}");
					EditorGUILayout.LabelField($"Cache Hit Rate: {hitRate:F1}%");

					Rect rect = GUILayoutUtility.GetRect(18, 18, GUILayout.ExpandWidth(true));
					EditorGUI.ProgressBar(rect, hitRate / 100f, $"{hitRate:F1}%");

					EditorGUILayout.EndVertical();
					EditorGUILayout.Space();
				}
			}

			EditorGUILayout.EndScrollView();
		}
	}
}
#endif
