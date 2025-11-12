using System.Collections.Generic;

/// <summary>
/// Contains all data loaded from a level file.
/// </summary>
public class LevelData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public List<(int x, int y, TileType tileType)> Tiles { get; set; }
    public List<(string animalName, int x, int y, int count)> Animals { get; set; }
    public List<(string itemName, int x, int y)> Items { get; set; }

    public LevelData()
    {
        Tiles = new List<(int x, int y, TileType tileType)>();
        Animals = new List<(string animalName, int x, int y, int count)>();
        Items = new List<(string itemName, int x, int y)>();
    }
}

