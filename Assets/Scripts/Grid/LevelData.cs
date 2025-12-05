using System.Collections.Generic;

/// <summary>
/// Represents data for an interactable in level data.
/// </summary>
public enum InteractableType
{
    Den,
    RabbitSpawner,
    PredatorDen,
    WormSpawner,
    Bush,
    Grass,
    Tree,
    BeeTree,
    GrassPatch
}

/// <summary>
/// Data structure for an interactable in level data.
/// </summary>
public struct InteractableData
{
    public InteractableType Type;
    public int X;
    public int Y;
    public string PredatorType; // Only used for PredatorDen
    
    public InteractableData(InteractableType type, int x, int y, string predatorType = null)
    {
        Type = type;
        X = x;
        Y = y;
        PredatorType = predatorType;
    }
}

/// <summary>
/// Contains all data loaded from a level file.
/// </summary>
public class LevelData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public List<(int x, int y, TileType tileType)> Tiles { get; set; }
    public List<(string animalName, int x, int y, int count)> Animals { get; set; }
    public List<(ItemType itemType, int x, int y)> Items { get; set; }
    public List<InteractableData> Interactables { get; set; }
    public int FoodCount { get; set; }

    public LevelData()
    {
        Tiles = new List<(int x, int y, TileType tileType)>();
        Animals = new List<(string animalName, int x, int y, int count)>();
        Items = new List<(ItemType itemType, int x, int y)>();
        Interactables = new List<InteractableData>();
        FoodCount = 0;
    }
}

