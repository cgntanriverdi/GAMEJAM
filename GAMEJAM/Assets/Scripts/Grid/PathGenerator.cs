using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gizli çözüm yolunu self-avoiding random walk ile üretir.
/// Start/end köşeler arasından random seçilir; renkler path'ten türetilir.
/// MonoBehaviour değil — GameManager veya LevelManager new'leyerek çağırır.
/// </summary>
public class PathGenerator
{
    private const int MaxAttempts = 200;

    // ── Public entry point ────────────────────────────────────────────────────

    public PathSolution Generate(LevelDefinition def, int seed = -1)
    {
        if (seed >= 0)
            Random.InitState(seed);

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            PickStartEnd(def, out GridCoord start, out GridCoord end);
            List<GridCoord> path = TryWalk(def, start, end);
            if (path != null)
                return BuildSolution(path, def);
        }

        Debug.LogWarning($"[PathGenerator] {MaxAttempts} denemede yol bulunamadı " +
                         $"({def.Width}x{def.Height}, min={def.MinPathLength}). Fallback gerekiyor.");
        return null;
    }

    // ── Start / End seçimi ────────────────────────────────────────────────────

    /// <summary>
    /// Dört köşeden rastgele iki farklı köşe seçer.
    /// Köşeler arası mesafe her zaman yeterince büyük olduğundan MinPathLength uyumu kolaylaşır.
    /// </summary>
    private static void PickStartEnd(LevelDefinition def, out GridCoord start, out GridCoord end)
    {
        GridCoord[] corners = {
            new GridCoord(0,            0),
            new GridCoord(def.Width-1,  0),
            new GridCoord(0,            def.Height-1),
            new GridCoord(def.Width-1,  def.Height-1),
        };
        int si = Random.Range(0, corners.Length);
        int ei;
        do { ei = Random.Range(0, corners.Length); } while (ei == si);
        start = corners[si];
        end   = corners[ei];
    }

    // ── Walk ──────────────────────────────────────────────────────────────────

    private List<GridCoord> TryWalk(LevelDefinition def, GridCoord start, GridCoord end)
    {
        var path    = new List<GridCoord> { start };
        var visited = new HashSet<GridCoord> { start };
        var current = start;

        int maxSteps = def.Width * def.Height;

        for (int step = 0; step < maxSteps; step++)
        {
            if (current == end) break;

            List<GridCoord> neighbors = GetUnvisitedNeighbors(current, visited, def);
            if (neighbors.Count == 0) return null;

            bool endReachable = neighbors.Contains(end);
            if (endReachable && path.Count >= def.MinPathLength - 1)
            {
                path.Add(end);
                return path;
            }
            if (endReachable && path.Count < def.MinPathLength - 1)
                neighbors.Remove(end);

            if (neighbors.Count == 0) return null;

            GridCoord next = neighbors[Random.Range(0, neighbors.Count)];
            path.Add(next);
            visited.Add(next);
            current = next;

            if (path.Count > def.MaxPathLength) return null;
        }

        if (path.Count == 0 || path[path.Count - 1] != end) return null;
        return path.Count >= def.MinPathLength ? path : null;
    }

    // ── Color assignment ──────────────────────────────────────────────────────

    /// <summary>
    /// Her path hücresine random renk atar; TargetColorCounts bu atamadan türetilir.
    /// PathColors dict'i GridManager.ApplySolution'a iletilir — çift hesaplama yok.
    /// </summary>
    private static PathSolution BuildSolution(List<GridCoord> path, LevelDefinition def)
    {
        CellColor[] palette    = BuildPalette(def.ActiveColorCount);
        var pathColors         = new Dictionary<GridCoord, CellColor>();
        var counts             = new Dictionary<CellColor, int>();

        foreach (var coord in path)
        {
            CellColor color = palette[Random.Range(0, palette.Length)];
            pathColors[coord] = color;
            counts.TryGetValue(color, out int prev);
            counts[color] = prev + 1;
        }

        return new PathSolution
        {
            Cells             = path,
            TargetColorCounts = counts,
            PathColors        = pathColors,
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<GridCoord> GetUnvisitedNeighbors(
        GridCoord coord, HashSet<GridCoord> visited, LevelDefinition def)
    {
        var result = new List<GridCoord>(4);
        foreach (var c in new[] { coord.Up(), coord.Down(), coord.Left(), coord.Right() })
        {
            if (c.X < 0 || c.X >= def.Width)  continue;
            if (c.Y < 0 || c.Y >= def.Height) continue;
            if (visited.Contains(c))           continue;
            result.Add(c);
        }
        return result;
    }

    private static CellColor[] BuildPalette(int activeCount)
    {
        CellColor[] all = (CellColor[])System.Enum.GetValues(typeof(CellColor));
        activeCount = Mathf.Clamp(activeCount, 1, all.Length);
        var palette = new CellColor[activeCount];
        System.Array.Copy(all, palette, activeCount);
        return palette;
    }
}

// Kullanacak scriptler: GameManager / LevelManager (Generate çağrısı),
//                       GridManager (dönen PathSolution'ı ApplySolution ile uygular)
