// 文件位置：RenameOperationV2.cs

using System;
using UnityEditor;
using UnityEngine;

namespace Asaki.Editor.Utilities.Tools.BatchRename
{
    [Serializable]
    public class RenameOperation
    {
        [SerializeField]
        private string _originalName;

        [SerializeField]
        private string _newName;

        [SerializeField]
        private int _instanceId;

        public string OriginalName => _originalName;
        public string NewName => _newName;
        public int InstanceId => _instanceId;

        public void SetNewName(string newName)
        {
            _newName = newName;
        }

        public RenameOperation(GameObject target, string newName)
        {
            _originalName = target.name;
            _newName = newName;
            _instanceId = target.GetInstanceID();
        }

        public bool TryGetGameObject(out GameObject go)
        {
            go = EditorUtility.InstanceIDToObject(_instanceId) as GameObject;
            return go != null;
        }

        public void Apply()
        {
            if (TryGetGameObject(out GameObject go) && go.name != _newName)
            {
                Undo.RegisterCompleteObjectUndo(go, "Rename");
                go.name = _newName;
                EditorUtility.SetDirty(go);
            }
        }
    }
}
