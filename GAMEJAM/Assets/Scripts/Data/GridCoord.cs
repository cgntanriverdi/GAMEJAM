using System;

/// <summary>
/// Immutable 2D integer coordinate on the puzzle grid.
/// Origin (0,0) is top-left; X grows right, Y grows down.
/// </summary>
[Serializable]
public struct GridCoord : IEquatable<GridCoord>
{
    public int X;
    public int Y;

    public GridCoord(int x, int y) { X = x; Y = y; }

    // Cardinal neighbours — callers must clamp to grid bounds themselves.
    public GridCoord Up()    => new GridCoord(X,     Y - 1);
    public GridCoord Down()  => new GridCoord(X,     Y + 1);
    public GridCoord Left()  => new GridCoord(X - 1, Y);
    public GridCoord Right() => new GridCoord(X + 1, Y);

    public bool Equals(GridCoord other) => X == other.X && Y == other.Y;
    public override bool Equals(object obj) => obj is GridCoord c && Equals(c);
    public override int GetHashCode() => X * 10000 + Y;
    public static bool operator ==(GridCoord a, GridCoord b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(GridCoord a, GridCoord b) => !(a == b);
    public override string ToString() => $"({X},{Y})";
}

// Kullanacak scriptler: GridManager, PathGenerator, SwipeInputController, RunValidator, CheckpointManager
