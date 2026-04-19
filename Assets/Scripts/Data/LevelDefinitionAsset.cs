using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inspector'da elle düzenlenebilen level konfigürasyonu.
/// Project penceresinde sağ tık → Create → ChromePath → Level Definition.
/// Curated fallback path boş bırakılırsa LevelManager snake pattern kullanır.
/// </summary>
[CreateAssetMenu(menuName = "ChromePath/Level Definition", fileName = "Level_00")]
public class LevelDefinitionAsset : ScriptableObject
{
    [Header("Level Config")]
    public LevelDefinition Definition;

    [Header("Curated Fallback (boş = LevelManager snake pattern üretir)")]
    [Tooltip("PathGenerator başarısız olursa bu sabit yol kullanılır.")]
    public List<GridCoord> CuratedPath;
    public List<CuratedColorEntry> CuratedCounts;

    // ── API ───────────────────────────────────────────────────────────────────

    public bool HasCuratedPath => CuratedPath != null && CuratedPath.Count >= 2;

    public PathSolution BuildCuratedSolution(int seed = -1)
    {
        var cells = CuratedPath != null ? new List<GridCoord>(CuratedPath) : new List<GridCoord>();
        var authoredCounts = new Dictionary<CellColor, int>();
        if (CuratedCounts != null)
        {
            foreach (var entry in CuratedCounts)
                authoredCounts[entry.Color] = Mathf.Max(0, entry.Count);
        }

        Dictionary<GridCoord, CellColor> pathColors = BuildPathColors(cells, authoredCounts, seed);
        Dictionary<CellColor, int> actualCounts = BuildActualCounts(cells, pathColors);

        return new PathSolution
        {
            Cells = cells,
            TargetColorCounts = actualCounts,
            PathColors = pathColors
        };
    }

    private static Dictionary<GridCoord, CellColor> BuildPathColors(
        IReadOnlyList<GridCoord> cells,
        Dictionary<CellColor, int> counts,
        int seed)
    {
        var result = new Dictionary<GridCoord, CellColor>();
        if (cells == null || cells.Count == 0)
            return result;

        var innerColors = new List<CellColor>();
        foreach (var pair in counts)
        {
            for (int i = 0; i < pair.Value; i++)
                innerColors.Add(pair.Key);
        }

        if (innerColors.Count == 0)
            innerColors.Add(CellColor.Red);

        CellColor anchorColor = innerColors.Count > 0 ? innerColors[0] : CellColor.Red;

        int innerCellCount = Mathf.Max(0, cells.Count - 2);
        if (innerColors.Count > innerCellCount)
            innerColors.RemoveRange(innerCellCount, innerColors.Count - innerCellCount);

        while (innerColors.Count < innerCellCount)
            innerColors.Add(innerColors[innerColors.Count % Mathf.Max(1, innerColors.Count)]);

        if (seed >= 0)
        {
            var random = new System.Random(seed);
            for (int i = innerColors.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(0, i + 1);
                CellColor temp = innerColors[i];
                innerColors[i] = innerColors[swapIndex];
                innerColors[swapIndex] = temp;
            }
        }

        result[cells[0]] = anchorColor;
        if (cells.Count == 1)
            return result;

        result[cells[cells.Count - 1]] = anchorColor;

        for (int i = 1; i < cells.Count - 1; i++)
            result[cells[i]] = innerColors[i - 1];

        return result;
    }

    private static Dictionary<CellColor, int> BuildActualCounts(
        IReadOnlyList<GridCoord> cells,
        Dictionary<GridCoord, CellColor> pathColors)
    {
        var result = new Dictionary<CellColor, int>();
        if (cells == null || pathColors == null)
            return result;

        for (int i = 1; i < cells.Count - 1; i++)
        {
            if (!pathColors.TryGetValue(cells[i], out CellColor color))
                continue;

            result.TryGetValue(color, out int previous);
            result[color] = previous + 1;
        }

        return result;
    }
}

/// <summary>
/// SerializableColorCount — ScriptableObject'te Dictionary yerine kullanılır.
/// </summary>
[Serializable]
public struct CuratedColorEntry
{
    public CellColor Color;
    public int       Count;
}

// Kullanacak scriptler: LevelManager (Definition ve BuildCuratedSolution erişimi)
