#if UNITY_EDITOR
using Asaki.Core.Attributes;
using Asaki.Core.Blackboard;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Asaki.Core.Blackboard.Variables;
using Asaki.Core.Logging;

namespace Asaki.Editor.Diagnostics
{
    public static class AsakiTypeBridgeValidator
    {
        [MenuItem("Asaki/Diagnostics/Validate Type Registry", false, 56)]
        public static void ValidateTypeRegistry()
        {
            var allTypes = TypeCache
                .GetTypesDerivedFrom<AsakiValueBase>()
                .Where(t =>
                    !t.IsAbstract
                    && t.GetCustomAttributes(
                        typeof(AsakiBlackboardValueSchemaAttribute),
                        false
                    ).Length > 0
                )
                .ToList();

            ALog.Info(
                $"[TypeValidator] Found {allTypes.Count} types with [AsakiBlackboardValueSchema]"
            );

            foreach (Type type in allTypes)
            {
                Debug.Log($"  - {type.FullName}");
            }
        }

        [MenuItem("Asaki/Diagnostics/Test Type Registration", false, 57)]
        public static void TestTypeRegistration()
        {
            AsakiBlackboard testBB = new AsakiBlackboard();
            AsakiBlackboardKey key = new AsakiBlackboardKey("Test");

            try
            {
                testBB.SetValue(key, 42);
                testBB.SetValue(key, 3.14f);
                testBB.SetValue(key, "Hello");
                testBB.SetValue(key, new Vector3(1, 2, 3));

                ALog.Info("[TypeValidator] ✓ All basic types registered successfully");
            }
            catch (Exception e)
            {
                ALog.Error($"[TypeValidator] ✗ Registration test failed: {e}");
            }
        }
    }
}
#endif
