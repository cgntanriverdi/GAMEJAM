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

// ── Runtime state ─────────────────────────────────────────────────────────────

public sealed class PlayerRunState
{
    public List<GridCoord> SelectedPath;
    public Dictionary<CellColor, int> CurrentColorCounts;
    public int CheckpointLockedLength;   // bu indeksin gerisine undo yapılamaz
    public bool CheckpointTriggered;
}

// Kullanacak scriptler: GridManager, PathGenerator, GameManager, RunValidator, CheckpointManager, CounterPanelUI
