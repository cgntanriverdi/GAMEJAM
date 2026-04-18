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

    public PathSolution BuildCuratedSolution()
    {
        var cells  = new List<GridCoord>(CuratedPath);
        var counts = new Dictionary<CellColor, int>();
        foreach (var entry in CuratedCounts)
            counts[entry.Color] = entry.Count;
        return new PathSolution { Cells = cells, TargetColorCounts = counts };
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
