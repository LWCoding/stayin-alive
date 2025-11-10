using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NaughtyAttributes;

/// <summary>
/// Component that loads and parses level files from text format.
/// Handles loading the environment and spawning animals.
/// Level file format:
/// - Lines starting with "//" are comments and are ignored
/// - Each line represents a row (y-coordinate) for tiles
/// - Each character represents a tile type:
///   - ' ' or '.' = Empty
///   - 'W' or 'w' = Water
///   - 'G' or 'g' = Grass
///   - '#' = Obstacle
/// - Use "---" as separator between tilemap and animal information
/// - After separator, use "ANIMALS:" header followed by animal entries as: animalId x y (one per line)
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
    /// Checks if a line is a comment (starts with "//" after trimming).
    /// </summary>
    private bool IsCommentLine(string line)
    {
        return !string.IsNullOrEmpty(line) && line.TrimStart().StartsWith("//");
    }

    /// <summary>
    /// Parses level data from an array of lines.
    /// </summary>
    private LevelData ParseLevelData(string[] lines)
    {
        LevelData levelData = new LevelData();
        int separatorLine = -1;

        // First pass: find the separator line ("---")
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            // Skip comments when looking for separator
            if (IsCommentLine(line))
                continue;
                
            if (line == "---")
            {
                separatorLine = i;
                break;
            }
        }

        // If no separator found, use all lines as tiles (for backward compatibility)
        int tileSectionEndLine = separatorLine >= 0 ? separatorLine : lines.Length;

        // First pass: collect all non-empty, non-comment tile lines and determine dimensions
        List<string> tileLines = new List<string>();
        int maxWidth = 0;
        
        for (int i = 0; i < tileSectionEndLine; i++)
        {
            string line = lines[i].TrimEnd();
            // Skip empty lines and comment lines in tile section
            if (string.IsNullOrEmpty(line) || IsCommentLine(line))
                continue;
            
            tileLines.Add(line);
            maxWidth = Mathf.Max(maxWidth, line.Length);
        }

        int tileHeight = tileLines.Count;

        // Second pass: populate tiles (only actual tiles, no padding)
        // Note: Invert y-coordinate because text files are read top-to-bottom,
        // but Unity's grid has y=0 at the bottom
        for (int fileY = 0; fileY < tileHeight; fileY++)
        {
            string line = tileLines[fileY];
            int lineLength = line.Length;
            
            // Convert file line index to grid y-coordinate (invert)
            int gridY = tileHeight - 1 - fileY;

            // Only parse tiles that exist in the file (no padding with empty tiles)
            for (int x = 0; x < lineLength; x++)
            {
                char c = line[x];
                TileType tileType = ParseTileType(c);
                
                // Add all tiles including empty ones that are explicitly in the file
                levelData.Tiles.Add((x, gridY, tileType));
            }
        }

        levelData.Width = maxWidth;
        levelData.Height = tileHeight;

        // Parse animals section (after separator)
        if (separatorLine >= 0)
        {
            bool inAnimalsSection = false;
            bool foundAnimalsHeader = false;
            
            for (int i = separatorLine + 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                
                // Skip empty lines and comments
                if (string.IsNullOrEmpty(line) || IsCommentLine(line))
                    continue;

                // Check for ANIMALS: header
                if (line.ToUpper().StartsWith("ANIMALS:"))
                {
                    inAnimalsSection = true;
                    foundAnimalsHeader = true;
                    continue;
                }

                // Parse animal lines if:
                // 1. We've seen ANIMALS: header (inAnimalsSection is true), OR
                // 2. We haven't found ANIMALS: header yet (backward compatibility - parse all data after separator)
                if (inAnimalsSection || !foundAnimalsHeader)
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
