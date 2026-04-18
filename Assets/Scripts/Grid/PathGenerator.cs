using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gizli çözüm yolunu self-avoiding random walk ile üretir.
/// MinPathLength / MaxPathLength = sadece ORTA hücreler (start ve end HARİÇ).
/// Toplam path uzunluğu = MinPathLength + 2 (start + renkli hücreler + end).
/// TargetColorCounts yalnızca orta hücrelerden türetilir.
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

    /// <summary>
    /// Self-avoiding random walk. Her adımda end'e ulaşılabilirlik (BFS) kontrol edilir;
    /// tıkanan path'ler erken elenir, 200 denemede geçerli çözüm bulunur.
    /// colorCells = path.Count - 1  (start hariç, end henüz eklenmemiş)
    /// Koşul: colorCells ∈ [MinPathLength, MaxPathLength]
    /// </summary>
    private List<GridCoord> TryWalk(LevelDefinition def, GridCoord start, GridCoord end)
    {
        var path    = new List<GridCoord> { start };
        var visited = new HashSet<GridCoord> { start };
        var current = start;

        int maxSteps = def.Width * def.Height;

        for (int step = 0; step < maxSteps; step++)
        {
            if (current == end) break;

            // Renkli hücre sayısı (start hariç, end henüz eklenmedi)
            int colorCells = path.Count - 1;

            List<GridCoord> neighbors = GetUnvisitedNeighbors(current, visited, def);
            if (neighbors.Count == 0) return null;

            bool endIsNeighbor = neighbors.Contains(end);

            // Minimum renk hücresi sayısına ulaştıysak end'i ekleyip bitir
            if (endIsNeighbor && colorCells >= def.MinPathLength)
            {
                path.Add(end);
                return path;
            }

            // Minimum dolmadıysa end'i geçici olarak seçeneklerden çıkar
            if (endIsNeighbor && colorCells < def.MinPathLength)
                neighbors.Remove(end);

            if (neighbors.Count == 0) return null;

            // Maksimum renk hücresi doldu, ilerleyemeyiz
            if (colorCells >= def.MaxPathLength) return null;

            // Bağlanabilirlik filtresi: sonraki adımdan end'e hâlâ ulaşılabiliyor mu?
            neighbors.RemoveAll(n =>
            {
                var temp = new HashSet<GridCoord>(visited) { n };
                return !IsEndReachable(n, end, temp, def);
            });

            if (neighbors.Count == 0) return null;

            GridCoord next = neighbors[Random.Range(0, neighbors.Count)];
            path.Add(next);
            visited.Add(next);
            current = next;
        }

        if (path.Count == 0 || path[path.Count - 1] != end) return null;

        // Son kontrol: toplam renkli hücre sayısı yeterli mi?
        int finalColorCells = path.Count - 2; // start ve end hariç
        return finalColorCells >= def.MinPathLength ? path : null;
    }

    // ── Color assignment ──────────────────────────────────────────────────────

    /// <summary>
    /// Tüm hücrelere görsel için renk atar; ancak TargetColorCounts sadece
    /// orta hücrelerden (index 1 .. Count-2) türetilir — start ve end sayılmaz.
    /// </summary>
    private static PathSolution BuildSolution(List<GridCoord> path, LevelDefinition def)
    {
        CellColor[] palette = BuildPalette(def.ActiveColorCount);
        var pathColors      = new Dictionary<GridCoord, CellColor>();
        var counts          = new Dictionary<CellColor, int>();

        for (int i = 0; i < path.Count; i++)
        {
            CellColor color = palette[Random.Range(0, palette.Length)];
            pathColors[path[i]] = color;

            // Sadece orta hücreler (start=0 ve end=son hariç)
            if (i > 0 && i < path.Count - 1)
            {
                counts.TryGetValue(color, out int prev);
                counts[color] = prev + 1;
            }
        }

        return new PathSolution
        {
            Cells             = path,
            TargetColorCounts = counts,
            PathColors        = pathColors,
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>BFS: visited kümesini aşmadan from'dan end'e ulaşılabiliyor mu?</summary>
    private static bool IsEndReachable(
        GridCoord from, GridCoord end,
        HashSet<GridCoord> visited, LevelDefinition def)
    {
        var queue = new Queue<GridCoord>();
        var seen  = new HashSet<GridCoord>(visited);
        queue.Enqueue(from);
        seen.Add(from);

        while (queue.Count > 0)
        {
            var curr = queue.Dequeue();
            if (curr == end) return true;
            foreach (var n in GetAllNeighbors(curr, def))
                if (seen.Add(n)) queue.Enqueue(n);
        }
        return false;
    }

    private static List<GridCoord> GetUnvisitedNeighbors(
        GridCoord coord, HashSet<GridCoord> visited, LevelDefinition def)
    {
        var result = new List<GridCoord>(4);
        foreach (var c in GetAllNeighbors(coord, def))
            if (!visited.Contains(c)) result.Add(c);
        return result;
    }

    private static List<GridCoord> GetAllNeighbors(GridCoord coord, LevelDefinition def)
    {
        var result = new List<GridCoord>(4);
        foreach (var c in new[] { coord.Up(), coord.Down(), coord.Left(), coord.Right() })
        {
            if (c.X < 0 || c.X >= def.Width)  continue;
            if (c.Y < 0 || c.Y >= def.Height) continue;
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
