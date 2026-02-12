using UnityEngine;

namespace Game.Scripts.Grid
{
    public enum GridCellState { Empty, Occupied }

    [System.Serializable]
    public class GridCell
    {
        public int X { get; set; }
        public int Z { get; set; }
        public Vector3 WorldPosition { get; set; }
        public GridCellState CellState { get; set; } = GridCellState.Empty;
        public string PlacedObjectID { get; set; } = string.Empty;

        public GridCell(int x, int z, Vector3 worldPosition)
        {
            X = x;
            Z = z;
            WorldPosition = worldPosition;
        }
    }
}
