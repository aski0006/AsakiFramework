using Asaki.Core.Context;
using Asaki.Core.Logging;
using Asaki.Unity;
using UnityEngine;

public class ScenePrefabs : AsakiMono, IAsakiSceneService
{
    protected override void OnStart()
    {
        ALog.Info("ScenePrefabs OnStart");
    }
}
