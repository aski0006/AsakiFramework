using UnityEngine;

namespace Game.Scripts.Grid
{
    [CreateAssetMenu(menuName = "Game/GridSO", fileName = "GridSO")]
    public class GridSO : ScriptableObject
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public float CellSize { get; private set; }
    }
}
