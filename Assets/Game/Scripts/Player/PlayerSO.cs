using UnityEngine;

namespace Game.Scripts.Player
{
    [CreateAssetMenu(menuName = "Game/SO/Player", fileName = "PlayerSO")]
    public class PlayerSO : ScriptableObject
    {
        [field: SerializeField] public float MoveSpeed { get; set; }
    }
}
