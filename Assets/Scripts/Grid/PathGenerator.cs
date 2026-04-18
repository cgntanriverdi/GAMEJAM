using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gizli çözüm yolunu self-avoiding random walk ile üretir.
/// Renk frekanslarını hesaplar ve PathSolution olarak döner.
/// MonoBehaviour değil — GameManager veya LevelManager new'leyerek çağırır.
/// </summary>
public class PathGenerator
{
    private const int MaxAttempts = 200;  // bu kadar denemede yol bulunamazsa curated fallback

    // ── Public entry point ────────────────────────────────────────────────────

    /// <summary>
    /// LevelDefinition'a göre bir PathSolution üretir.
    /// Tüm denemeler başarısız olursa null döner (GameManager fallback'e geçmeli).
    /// </summary>
    public PathSolution Generate(LevelDefinition def, int seed = -1)
    {
        if (seed >= 0)
            Random.InitState(seed);

        var start = new GridCoord(0, 0);
        var end   = new GridCoord(def.Width - 1, def.Height - 1);

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            List<GridCoord> path = TryWalk(def, start, end);
            if (path != null)
                return BuildSolution(path, def);
        }

        Debug.LogWarning($"[PathGenerator] {MaxAttempts} denemede yol bulunamadı " +
                         $"({def.Width}x{def.Height}, min={def.MinPathLength}). Fallback gerekiyor.");
        return null;
    }

    // ── Walk ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Self-avoiding random walk: başlangıçtan bitişe ulaşmaya çalışır.
    /// Başarılıysa ve MinPathLength sağlanıyorsa yolu döner; aksi hâlde null.
    /// </summary>
    private List<GridCoord> TryWalk(LevelDefinition def, GridCoord start, GridCoord end)
    {
        var path    = new List<GridCoord> { start };
        var visited = new HashSet<GridCoord> { start };
        var current = start;

        int maxSteps = def.Width * def.Height;  // sonsuz döngü güvencesi

        for (int step = 0; step < maxSteps; step++)
        {
            if (current == end)
                break;

            List<GridCoord> neighbors = GetUnvisitedNeighbors(current, visited, def);

            if (neighbors.Count == 0)
                return null;  // çıkmaz sokak — yeniden dene

            // Bitiş komşuysa erken yakalama: yol min uzunluğu sağlıyorsa bitir
            bool endReachable = neighbors.Contains(end);
            if (endReachable && path.Count >= def.MinPathLength - 1)
            {
                path.Add(end);
                return path;
            }
            // Bitişi şimdilik atla, daha uzun yol için başka yön dene
            if (endReachable && path.Count < def.MinPathLength - 1)
                neighbors.Remove(end);

            if (neighbors.Count == 0)
                return null;

            GridCoord next = neighbors[Random.Range(0, neighbors.Count)];
            path.Add(next);
            visited.Add(next);
            current = next;

            if (path.Count > def.MaxPathLength)
                return null;  // çok uzadı
        }

        // End'e hiç ulaşılmadıysa geçersiz
        if (path.Count == 0 || path[path.Count - 1] != end)
            return null;

        return path.Count >= def.MinPathLength ? path : null;
    }

    // ── Color assignment ──────────────────────────────────────────────────────

    /// <summary>
    /// Yol hücrelerine renk atar ve TargetColorCounts hesaplar.
    /// Start ve End hücreleri de renklendirilir (oyuncu görür).
    /// </summary>
    private PathSolution BuildSolution(List<GridCoord> path, LevelDefinition def)
    {
        CellColor[] palette = BuildPalette(def.ActiveColorCount);
        var counts = new Dictionary<CellColor, int>();

        // Her renge ön-sıfır ver
        foreach (var color in palette)
            counts[color] = 0;

        // Renkleri path boyunca eşit aralıklarla dağıt (deterministik desen)
        int pathLen = path.Count;
        for (int i = 0; i < pathLen; i++)
        {
            CellColor color = palette[i % palette.Length];
            counts[color]++;
        }

        return new PathSolution
        {
            Cells             = path,
            TargetColorCounts = counts
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<GridCoord> GetUnvisitedNeighbors(
        GridCoord coord, HashSet<GridCoord> visited, LevelDefinition def)
    {
        var result = new List<GridCoord>(4);
        GridCoord[] candidates = {
            coord.Up(), coord.Down(), coord.Left(), coord.Right()
        };
        foreach (var c in candidates)
        {
            if (c.X < 0 || c.X >= def.Width) continue;
            if (c.Y < 0 || c.Y >= def.Height) continue;
            if (visited.Contains(c)) continue;
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
