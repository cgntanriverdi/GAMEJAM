using System;
using System.Collections.Generic;

// ── Enums ────────────────────────────────────────────────────────────────────

public enum CellColor { Red, Blue, Green, Yellow }

// Data-only; SwipeInputController kullanır.
public enum SwipeDirection { Up, Down, Left, Right }

// ── Cell ─────────────────────────────────────────────────────────────────────

[Serializable]
public sealed class CellData
{
    public GridCoord Coord;
    public CellColor Color;
    public bool IsStart;
    public bool IsEnd;
}

// ── Level config ──────────────────────────────────────────────────────────────

[Serializable]
public sealed class LevelDefinition
{
    public int LevelIndex;
    public int Width;
    public int Height;
    public int MinPathLength;
    public int MaxPathLength;
    public int ActiveColorCount;  // kaç renk kullanılacak (2-4)
    public bool AllowGeneratedLevel;
}

// ── Solution ──────────────────────────────────────────────────────────────────

public sealed class PathSolution
{
    public List<GridCoord> Cells;                         // gizli yolun sıralı hücreleri
    public Dictionary<CellColor, int> TargetColorCounts;  // oyuncuya gösterilen hedef sayılar
    public Dictionary<GridCoord, CellColor> PathColors;   // hücre → renk (path'ten türetilir)
}

public readonly struct LevelCompletionResult
{
    public LevelCompletionResult(
        int levelIndex,
        float elapsedSeconds,
        int starsEarned,
        int bestStars,
        float bestTimeSeconds,
        bool isNewBest)
    {
        LevelIndex = levelIndex;
        ElapsedSeconds = elapsedSeconds;
        StarsEarned = starsEarned;
        BestStars = bestStars;
        BestTimeSeconds = bestTimeSeconds;
        IsNewBest = isNewBest;
    }

    public int LevelIndex { get; }
    public float ElapsedSeconds { get; }
    public int StarsEarned { get; }
    public int BestStars { get; }
    public float BestTimeSeconds { get; }
    public bool IsNewBest { get; }
}

// ── Runtime state ─────────────────────────────────────────────────────────────

public sealed class PlayerRunState
{
    public List<GridCoord> SelectedPath;
    public Dictionary<CellColor, int> CurrentColorCounts;
}

// Kullanacak scriptler: GridManager, PathGenerator, GameManager, RunValidator, CounterPanelUI
