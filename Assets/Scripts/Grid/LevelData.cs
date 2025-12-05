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
    Tree
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
    public List<(string itemName, int x, int y)> Items { get; set; }
    public List<InteractableData> Interactables { get; set; }
    public int FoodCount { get; set; }

    // Legacy properties for backward compatibility with InteractableManager
    // These are kept alongside Interactables for now until InteractableManager is updated
    public List<(int x, int y)> Dens { get; set; }
    public List<(int x, int y)> RabbitSpawners { get; set; }
    public List<(int x, int y, string predatorType)> PredatorDens { get; set; }
    public List<(int x, int y)> WormSpawners { get; set; }
    public List<(int x, int y)> Bushes { get; set; }
    public List<(int x, int y)> Grasses { get; set; }
    public List<(int x, int y)> Trees { get; set; }

    public LevelData()
    {
        Tiles = new List<(int x, int y, TileType tileType)>();
        Animals = new List<(string animalName, int x, int y, int count)>();
        Items = new List<(string itemName, int x, int y)>();
        Interactables = new List<InteractableData>();
        FoodCount = 0;
        
        // Initialize legacy lists for backward compatibility
        Dens = new List<(int x, int y)>();
        RabbitSpawners = new List<(int x, int y)>();
        PredatorDens = new List<(int x, int y, string predatorType)>();
        WormSpawners = new List<(int x, int y)>();
        Bushes = new List<(int x, int y)>();
        Grasses = new List<(int x, int y)>();
        Trees = new List<(int x, int y)>();
    }
}

