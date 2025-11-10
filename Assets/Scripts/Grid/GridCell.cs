using UnityEngine;

/// <summary>
/// Represents a single cell in the grid, storing its type and position.
/// </summary>
[System.Serializable]
public class GridCell
{
    public TileType TileType;
    public Vector2Int Position;

    public GridCell(TileType type, Vector2Int pos)
    {
        TileType = type;
        Position = pos;
    }

    public GridCell(TileType type, int x, int y)
    {
        TileType = type;
        Position = new Vector2Int(x, y);
    }
}

