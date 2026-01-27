#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Asaki.Core.Blackboard
{
	public static class BlackboardProfiler
	{
		private static Dictionary<string, ProfileData> _accessStats = new Dictionary<string, ProfileData>();
		private static bool _isEnabled = false;

		public struct ProfileData
		{
			public int AccessCount;
			public int HashHitCount;
			public int HashMissCount;
			public long TotalHashTime;
		}

		public static void Enable()
		{
			_isEnabled = true;
			_accessStats.Clear();
		}

		public static void Disable()
		{
			_isEnabled = false;
		}

		public static void RecordAccess(string key, bool cacheHit, long elapsedTicks = 0)
		{
			if (!_isEnabled) return;

			if (!_accessStats.TryGetValue(key, out ProfileData data))
			{
				data = new ProfileData();
			}

			data.AccessCount++;
			if (cacheHit)
				data.HashHitCount++;
			else
			{
				data.HashMissCount++;
				data.TotalHashTime += elapsedTicks;
			}

			_accessStats[key] = data;
		}

		public static void PrintReport()
		{
			if (_accessStats.Count == 0)
			{
				Debug.Log("[BlackboardProfiler] No data collected");
				return;
			}

			Debug.Log("=== Blackboard Access Report ===");
			foreach (var kvp in _accessStats)
			{
				ProfileData data = kvp.Value;
				float hitRate = (float)data.HashHitCount / data.AccessCount * 100f;
				float avgHashTime = data.HashMissCount > 0
					? (float)data.TotalHashTime / data.HashMissCount
					: 0f;

				Debug.Log($"Key: {kvp.Key} | Access: {data.AccessCount} | " +
				          $"Hit Rate: {hitRate:F1}% | Avg Hash Time: {avgHashTime:F2} ticks");
			}
		}

		public static IReadOnlyDictionary<string, ProfileData> GetStats()
		{
			return _accessStats;
		}
	}
}
#endif
