using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NaughtyAttributes;

/// <summary>
/// Component that loads and parses level files from text format.
/// Handles loading the environment and spawning animals.
/// Level file format:
/// - Each line represents a row (y-coordinate) for tiles
/// - Each character represents a tile type:
///   - ' ' or '.' = Empty
///   - 'W' or 'w' = Water
///   - 'G' or 'g' = Grass
///   - '#' = Obstacle
/// - After an empty line or "ANIMALS:" header, list animals as: animalId x y (one per line)
/// </summary>
public class LevelLoader : MonoBehaviour
{
    [Header("Level Loading")]
    [InfoBox("Enter level file name (without .txt extension) from Levels/ folder, or full path to level file.")]
    [SerializeField] private string _levelFileName = "";
    
    [Button("Load Level")]
    private void LoadLevel()
    {
        if (string.IsNullOrEmpty(_levelFileName))
        {
            Debug.LogWarning("LevelLoader: Level file name is empty!");
            return;
        }

        LoadAndApplyLevel(_levelFileName);
    }

    /// <summary>
    /// Loads a level from a text file and returns level data including tiles and animals.
    /// </summary>
    /// <param name="filePath">Path to the level file (relative to Assets folder or Resources folder)</param>
    /// <returns>LevelData containing tiles, animals, and dimensions, or null if file not found</returns>
    public LevelData LoadLevelFromFile(string filePath)
    {
        string fullPath = GetFullPath(filePath);
        
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"LevelLoader: File not found at path: {fullPath}");
            return null;
        }

        string[] allLines = File.ReadAllLines(fullPath);
        return ParseLevelData(allLines);
    }

    /// <summary>
    /// Loads a level from Resources folder.
    /// </summary>
    /// <param name="resourcePath">Path relative to Resources folder (without .txt extension)</param>
    /// <returns>LevelData containing tiles, animals, and dimensions, or null if file not found</returns>
    public LevelData LoadLevelFromResources(string resourcePath)
    {
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        
        if (textAsset == null)
        {
            Debug.LogError($"LevelLoader: Resource not found at path: {resourcePath}");
            return null;
        }

        string[] lines = textAsset.text.Split('\n');
        return ParseLevelData(lines);
    }

    /// <summary>
    /// Loads a level and applies it to the environment and spawns animals.
    /// </summary>
    /// <param name="levelFileName">Level file name (without .txt) from Levels/ folder, or full path to level file</param>
    public void LoadAndApplyLevel(string levelFileName)
    {
        // Try loading from Resources/Levels folder first
        string resourcePath = $"Levels/{levelFileName}";
        LevelData levelData = LoadLevelFromResources(resourcePath);

        // If not found in Resources, try as file path
        if (levelData == null)
        {
            levelData = LoadLevelFromFile(levelFileName);
        }

        if (levelData == null)
        {
            Debug.LogError($"LevelLoader: Failed to load level '{levelFileName}'");
            return;
        }

        // Apply environment using EnvironmentManager
        if (EnvironmentManager.Instance == null)
        {
            Debug.LogError("LevelLoader: EnvironmentManager instance not found!");
            return;
        }

        // Initialize grid with level dimensions
        EnvironmentManager.Instance.InitializeGrid(levelData.Width, levelData.Height);

        // Apply tiles to the grid
        foreach (var (x, y, tileType) in levelData.Tiles)
        {
            if (EnvironmentManager.Instance.IsValidPosition(x, y))
            {
                EnvironmentManager.Instance.SetTileType(x, y, tileType);
            }
        }

        // Spawn animals using AnimalManager
        if (AnimalManager.Instance != null)
        {
            AnimalManager.Instance.SpawnAnimalsFromLevelData(levelData.Animals);
        }
        else
        {
            Debug.LogWarning("LevelLoader: AnimalManager instance not found! Animals will not be spawned.");
        }

        Debug.Log($"LevelLoader: Successfully loaded level '{levelFileName}' with {levelData.Tiles.Count} tiles and {levelData.Animals.Count} animals");
    }

    /// <summary>
    /// Parses level data from an array of lines.
    /// </summary>
    private LevelData ParseLevelData(string[] lines)
    {
        LevelData levelData = new LevelData();
        bool inAnimalsSection = false;
        int tileSectionEndLine = 0;

        // First pass: find where tile section ends and animals section begins
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            
            if (string.IsNullOrEmpty(line))
            {
                if (i > 0 && !inAnimalsSection)
                {
                    // Empty line after tiles marks the start of animals section
                    inAnimalsSection = true;
                    tileSectionEndLine = i;
                }
                continue;
            }

            if (line.ToUpper().StartsWith("ANIMALS:"))
            {
                inAnimalsSection = true;
                tileSectionEndLine = i;
                continue;
            }

            if (!inAnimalsSection)
            {
                tileSectionEndLine = i + 1;
            }
        }

        // First pass: determine grid dimensions
        int maxWidth = 0;
        int tileHeight = 0;
        for (int y = 0; y < tileSectionEndLine; y++)
        {
            string line = lines[y].TrimEnd();
            if (!string.IsNullOrEmpty(line))
            {
                maxWidth = Mathf.Max(maxWidth, line.Length);
            }
            tileHeight = y + 1;
        }

        // Second pass: populate all tiles (including empty ones)
        // Note: Invert y-coordinate because text files are read top-to-bottom,
        // but Unity's grid has y=0 at the bottom
        for (int fileY = 0; fileY < tileHeight; fileY++)
        {
            string line = fileY < lines.Length ? lines[fileY].TrimEnd() : "";
            int lineLength = string.IsNullOrEmpty(line) ? 0 : line.Length;
            
            // Convert file line index to grid y-coordinate (invert)
            int gridY = tileHeight - 1 - fileY;

            for (int x = 0; x < maxWidth; x++)
            {
                TileType tileType;
                if (x < lineLength)
                {
                    // Parse tile from file
                    char c = line[x];
                    tileType = ParseTileType(c);
                }
                else
                {
                    // Position beyond line length - fill with empty
                    tileType = TileType.Empty;
                }
                
                // Add all tiles, including empty ones
                levelData.Tiles.Add((x, gridY, tileType));
            }
        }

        levelData.Width = maxWidth;
        levelData.Height = tileHeight;

        // Parse animals section
        inAnimalsSection = false;
        for (int i = tileSectionEndLine; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            
            if (string.IsNullOrEmpty(line))
                continue;

            if (line.ToUpper().StartsWith("ANIMALS:"))
            {
                inAnimalsSection = true;
                continue;
            }

            if (inAnimalsSection || i > tileSectionEndLine)
            {
                // Parse animal line: animalId x y
                // Note: y-coordinate from file needs to be inverted to match grid coordinates
                string[] parts = line.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    if (int.TryParse(parts[0], out int animalId) &&
                        int.TryParse(parts[1], out int x) &&
                        int.TryParse(parts[2], out int fileY))
                    {
                        // Convert file y-coordinate to grid y-coordinate (invert)
                        int gridY = tileHeight - 1 - fileY;
                        levelData.Animals.Add((animalId, x, gridY));
                    }
                    else
                    {
                        Debug.LogWarning($"LevelLoader: Could not parse animal line: {line}");
                    }
                }
            }
        }

        Debug.Log($"LevelLoader: Loaded level with {levelData.Tiles.Count} tiles, {levelData.Animals.Count} animals, size: {levelData.Width}x{levelData.Height}");
        return levelData;
    }

    /// <summary>
    /// Parses a character into a TileType.
    /// </summary>
    private TileType ParseTileType(char c)
    {
        switch (char.ToUpper(c))
        {
            case 'W':
                return TileType.Water;
            case 'G':
                return TileType.Grass;
            case '#':
                return TileType.Obstacle;
            case ' ':
            case '.':
            default:
                return TileType.Empty;
        }
    }

    /// <summary>
    /// Gets the full file path, handling both absolute paths and paths relative to Assets folder.
    /// </summary>
    private string GetFullPath(string filePath)
    {
        if (Path.IsPathRooted(filePath))
        {
            return filePath;
        }

        // Try relative to Assets folder
        string assetsPath = Path.Combine(Application.dataPath, filePath);
        if (File.Exists(assetsPath))
        {
            return assetsPath;
        }

        // Try as-is (might be absolute path already)
        return filePath;
    }
}
