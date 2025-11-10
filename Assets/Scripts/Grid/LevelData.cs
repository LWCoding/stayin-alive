using System.Collections.Generic;

/// <summary>
/// Contains all data loaded from a level file.
/// </summary>
public class LevelData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public List<(int x, int y, TileType tileType)> Tiles { get; set; }
    public List<(int animalId, int x, int y)> Animals { get; set; }

    public LevelData()
    {
        Tiles = new List<(int x, int y, TileType tileType)>();
        Animals = new List<(int animalId, int x, int y)>();
    }
}

