using UnityEngine;

/// <summary>
/// Represents a single cell in the grid, storing its type and position.
/// </summary>
[System.Serializable]
public class GridCell
{
    public TileType TileType;
    public Vector2Int Position;
    public bool IsOccupied; // Whether an animal is on this cell
    public int OccupantId;  // ID of the animal occupying this cell (-1 if empty)

    public GridCell(TileType type, Vector2Int pos)
    {
        TileType = type;
        Position = pos;
        IsOccupied = false;
        OccupantId = -1;
    }

    public GridCell(TileType type, int x, int y)
    {
        TileType = type;
        Position = new Vector2Int(x, y);
        IsOccupied = false;
        OccupantId = -1;
    }

    /// <summary>
    /// Sets an occupant on this cell.
    /// </summary>
    public void SetOccupant(int id)
    {
        IsOccupied = true;
        OccupantId = id;
    }

    /// <summary>
    /// Removes the occupant from this cell.
    /// </summary>
    public void ClearOccupant()
    {
        IsOccupied = false;
        OccupantId = -1;
    }
}

