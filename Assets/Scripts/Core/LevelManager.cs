using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Session içi level map'ini yönetir; LevelDefinitionAsset'lerden veya dahili
/// tier tablosundan LevelDefinition üretir, çözümü sabit seed ile hazırlar ve
/// GameManager'a iletir. PathGenerator başarısız olursa: curated asset →
/// snake pattern fallback.
///
/// DefaultExecutionOrder(0):
///   ProgressionService(-10).Start'tan sonra,
///   GameManager(10).Start'tan önce çalışır.
/// </summary>
[DefaultExecutionOrder(0)]
public class LevelManager : MonoBehaviour
{
    private sealed class SessionLevelState
    {
        public LevelDefinition Definition;
        public PathSolution Solution;
        public PathSolution Fallback;
        public int GridSeed;
        public int BestStars;
        public float BestTimeSeconds = float.PositiveInfinity;
        public float ThreeStarSeconds;
        public float TwoStarSeconds;
        public bool IsUnlocked;
    }

    // ── Singleton ─────────────────────────────────────────────────────────────

    public static LevelManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Level assets (boş slotlar tier tablosuna düşer)")]
    [SerializeField] private LevelDefinitionAsset[] _levelAssets;

    [Header("Session map")]
    [SerializeField] [Min(1)] private int _sessionLevelCount = 10;

    private readonly List<SessionLevelState> _sessionLevels = new();
    private int _activeLevelIndex = -1;
    private int _highestUnlockedLevelIndex;

    public int ActiveLevelIndex => _activeLevelIndex;
    public int SessionLevelCount => _sessionLevels.Count;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildSessionCatalog();
    }

    private void Start()
    {
        if (!StartupMenuUI.ShouldBlockAutoStart)
            LoadCurrentLevel();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Session içinde aktif level varsa onu, yoksa açılmış en son level'ı yükler.
    /// </summary>
    public bool LoadCurrentLevel()
    {
        if (_sessionLevels.Count == 0)
            return false;

        int idx = _activeLevelIndex >= 0
            ? _activeLevelIndex
            : Mathf.Clamp(_highestUnlockedLevelIndex, 0, _sessionLevels.Count - 1);

        return LoadLevel(idx);
    }

    public bool ReloadActiveLevel()
    {
        if (_sessionLevels.Count == 0)
            return false;

        int idx = _activeLevelIndex >= 0 ? _activeLevelIndex : 0;
        return LoadLevel(idx);
    }

    public bool LoadLevel(int levelIndex)
    {
        if (!IsValidLevelIndex(levelIndex))
        {
            Debug.LogWarning($"[LevelManager] Geçersiz level index: {levelIndex}");
            return false;
        }

        if (!IsLevelUnlocked(levelIndex))
        {
            Debug.LogWarning($"[LevelManager] Kilitli level seçildi: {levelIndex + 1}");
            return false;
        }

        if (GameManager.Instance == null)
            return false;

        SessionLevelState entry = _sessionLevels[levelIndex];
        _activeLevelIndex = levelIndex;

        GameManager.Instance.StartLevel(
            entry.Definition,
            sessionSolution: entry.Solution,
            gridSeed: entry.GridSeed,
            curatedFallback: entry.Fallback);

        Debug.Log($"[LevelManager] Session level {levelIndex + 1} yüklendi — " +
                  $"{entry.Definition.Width}x{entry.Definition.Height}, minPath={entry.Definition.MinPathLength}, renkler={entry.Definition.ActiveColorCount}");
        return true;
    }

    public bool IsLevelUnlocked(int levelIndex) =>
        IsValidLevelIndex(levelIndex) && _sessionLevels[levelIndex].IsUnlocked;

    public int GetBestStars(int levelIndex) =>
        IsValidLevelIndex(levelIndex) ? _sessionLevels[levelIndex].BestStars : 0;

    public float GetBestTimeSeconds(int levelIndex)
    {
        if (!IsValidLevelIndex(levelIndex))
            return 0f;

        float bestTime = _sessionLevels[levelIndex].BestTimeSeconds;
        return float.IsPositiveInfinity(bestTime) ? 0f : bestTime;
    }

    public bool TryGetNextUnlockedLevelIndex(int currentLevelIndex, out int nextLevelIndex)
    {
        nextLevelIndex = currentLevelIndex + 1;
        return IsLevelUnlocked(nextLevelIndex);
    }

    public int CalculateStars(int levelIndex, float elapsedSeconds)
    {
        if (!IsValidLevelIndex(levelIndex))
            return 1;

        SessionLevelState entry = _sessionLevels[levelIndex];
        if (elapsedSeconds <= entry.ThreeStarSeconds) return 3;
        if (elapsedSeconds <= entry.TwoStarSeconds)   return 2;
        return 1;
    }

    public LevelCompletionResult RecordLevelResult(int levelIndex, float elapsedSeconds, int starsEarned)
    {
        if (!IsValidLevelIndex(levelIndex))
            return new LevelCompletionResult(levelIndex, elapsedSeconds, starsEarned, starsEarned, elapsedSeconds, true);

        SessionLevelState entry = _sessionLevels[levelIndex];
        int previousBestStars = entry.BestStars;
        float previousBestTime = entry.BestTimeSeconds;

        entry.BestStars = Mathf.Max(entry.BestStars, starsEarned);
        entry.BestTimeSeconds = float.IsPositiveInfinity(entry.BestTimeSeconds)
            ? elapsedSeconds
            : Mathf.Min(entry.BestTimeSeconds, elapsedSeconds);

        bool isNewBest = entry.BestStars > previousBestStars
            || float.IsPositiveInfinity(previousBestTime)
            || elapsedSeconds < previousBestTime - 0.001f;

        int nextLevelIndex = levelIndex + 1;
        if (IsValidLevelIndex(nextLevelIndex))
        {
            _sessionLevels[nextLevelIndex].IsUnlocked = true;
            _highestUnlockedLevelIndex = Mathf.Max(_highestUnlockedLevelIndex, nextLevelIndex);
        }

        return new LevelCompletionResult(
            levelIndex,
            elapsedSeconds,
            starsEarned,
            entry.BestStars,
            entry.BestTimeSeconds,
            isNewBest);
    }

    // ── Level config ──────────────────────────────────────────────────────────

    private LevelDefinition GetDefinition(int idx)
    {
        if (_levelAssets != null && idx < _levelAssets.Length && _levelAssets[idx] != null)
            return CloneDefinition(_levelAssets[idx].Definition, idx);

        return BuildTierDefinition(idx);
    }

    private static LevelDefinition CloneDefinition(LevelDefinition source, int idx) =>
        new LevelDefinition
        {
            LevelIndex = idx,
            Width = source.Width,
            Height = source.Height,
            MinPathLength = source.MinPathLength,
            MaxPathLength = source.MaxPathLength,
            ActiveColorCount = source.ActiveColorCount,
            AllowGeneratedLevel = source.AllowGeneratedLevel
        };

    /// <summary>
    /// Tier tablosu — min/max = RENK hücresi sayısı (start ve end HARİÇ).
    /// Toplam path uzunluğu = min/max + 2.
    ///   1-2  : 3×3, 1-2 renk hücresi,   2 renk
    ///   3-4  : 4×4, 3-4 renk hücresi,   2 renk
    ///   5-6  : 4×4, 5-7 renk hücresi,   3 renk
    ///   7-8  : 5×5, 8-10 renk hücresi,  3 renk
    ///   9-10 : 5×5, 11-13 renk hücresi, 4 renk
    ///   11+  : 6×6, 14-18 renk hücresi, 4 renk
    /// </summary>
    private static LevelDefinition BuildTierDefinition(int idx)
    {
        int level = idx + 1;

        if (level <= 2)  return Make(idx, w: 3, h: 3, min: 1,  max: 2,  colors: 2);
        if (level <= 4)  return Make(idx, w: 4, h: 4, min: 3,  max: 4,  colors: 2);
        if (level <= 6)  return Make(idx, w: 4, h: 4, min: 5,  max: 7,  colors: 3);
        if (level <= 8)  return Make(idx, w: 5, h: 5, min: 8,  max: 10, colors: 3);
        if (level <= 10) return Make(idx, w: 5, h: 5, min: 11, max: 13, colors: 4);

        return Make(idx, w: 6, h: 6, min: 14, max: 18, colors: 4);
    }

    private static LevelDefinition Make(int idx, int w, int h, int min, int max, int colors) =>
        new LevelDefinition
        {
            LevelIndex          = idx,
            Width               = w,
            Height              = h,
            MinPathLength       = min,
            MaxPathLength       = max,
            ActiveColorCount    = colors,
            AllowGeneratedLevel = true
        };

    // ── Fallback ──────────────────────────────────────────────────────────────

    private void BuildSessionCatalog()
    {
        if (_sessionLevels.Count > 0)
            return;

        int assetCount = _levelAssets != null ? _levelAssets.Length : 0;
        int levelCount = Mathf.Max(_sessionLevelCount, Mathf.Max(assetCount, 1));
        var random = new System.Random(unchecked(Environment.TickCount ^ GetInstanceID()));

        for (int idx = 0; idx < levelCount; idx++)
        {
            LevelDefinition definition = GetDefinition(idx);
            int gridSeed = random.Next(1, int.MaxValue);
            int pathSeed = random.Next(1, int.MaxValue);

            PathSolution fallback = GetFallback(definition, pathSeed);
            PathSolution solution = BuildSessionSolution(definition, pathSeed, fallback);

            BuildStarThresholds(solution, out float threeStarSeconds, out float twoStarSeconds);

            _sessionLevels.Add(new SessionLevelState
            {
                Definition = definition,
                Solution = solution,
                Fallback = fallback,
                GridSeed = gridSeed,
                ThreeStarSeconds = threeStarSeconds,
                TwoStarSeconds = twoStarSeconds,
                IsUnlocked = idx == 0
            });
        }

        _highestUnlockedLevelIndex = 0;
    }

    private PathSolution BuildSessionSolution(LevelDefinition def, int pathSeed, PathSolution fallback)
    {
        if (def.AllowGeneratedLevel)
        {
            PathSolution generated = new PathGenerator().Generate(def, pathSeed);
            if (generated != null)
                return generated;
        }

        return fallback ?? BuildSnakeFallback(def, pathSeed);
    }

    private PathSolution GetFallback(LevelDefinition def, int seed)
    {
        int idx = def.LevelIndex;

        // 1. Önce asset curated path
        if (_levelAssets != null && idx < _levelAssets.Length && _levelAssets[idx]?.HasCuratedPath == true)
            return _levelAssets[idx].BuildCuratedSolution(seed);

        // 2. Garantili snake pattern
        return BuildSnakeFallback(def, seed);
    }

    /// <summary>
    /// Grid üzerinde satır-satır yılan şeklinde ilerler ve
    /// End hücresi (width-1, height-1)'e ulaşınca durur.
    /// Her grid boyutu ve yüksekliği için garantili çalışır.
    /// </summary>
    private static PathSolution BuildSnakeFallback(LevelDefinition def, int seed)
    {
        var end  = new GridCoord(def.Width - 1, def.Height - 1);
        var path = new List<GridCoord>();
        bool done = false;

        for (int y = 0; y < def.Height && !done; y++)
        {
            bool goRight = (y % 2 == 0);
            int  xStart  = goRight ? 0 : def.Width - 1;
            int  xEnd    = goRight ? def.Width - 1 : 0;
            int  xStep   = goRight ? 1 : -1;

            for (int x = xStart; goRight ? x <= xEnd : x >= xEnd; x += xStep)
            {
                var coord = new GridCoord(x, y);
                path.Add(coord);
                if (coord == end) { done = true; break; }
            }
        }

        Dictionary<GridCoord, CellColor> pathColors = BuildPathColors(path, BuildPalette(def.ActiveColorCount), seed);
        Dictionary<CellColor, int> counts = BuildTargetCounts(path, pathColors);

        return new PathSolution
        {
            Cells = path,
            TargetColorCounts = counts,
            PathColors = pathColors
        };
    }

    private static CellColor[] BuildPalette(int activeCount)
    {
        CellColor[] all = (CellColor[])System.Enum.GetValues(typeof(CellColor));
        activeCount = Mathf.Clamp(activeCount, 1, all.Length);
        var palette = new CellColor[activeCount];
        System.Array.Copy(all, palette, activeCount);
        return palette;
    }

    private static Dictionary<GridCoord, CellColor> BuildPathColors(
        IReadOnlyList<GridCoord> path,
        CellColor[] palette,
        int seed)
    {
        var result = new Dictionary<GridCoord, CellColor>(path.Count);
        var random = new System.Random(seed);

        for (int i = 0; i < path.Count; i++)
        {
            CellColor color = palette[random.Next(0, palette.Length)];
            result[path[i]] = color;
        }

        return result;
    }

    private static Dictionary<CellColor, int> BuildTargetCounts(
        IReadOnlyList<GridCoord> path,
        Dictionary<GridCoord, CellColor> pathColors)
    {
        var result = new Dictionary<CellColor, int>();
        for (int i = 1; i < path.Count - 1; i++)
        {
            CellColor color = pathColors[path[i]];
            result.TryGetValue(color, out int prev);
            result[color] = prev + 1;
        }

        return result;
    }

    private static void BuildStarThresholds(PathSolution solution, out float threeStarSeconds, out float twoStarSeconds)
    {
        int moveCount = Mathf.Max(1, solution.Cells.Count - 1);
        threeStarSeconds = Mathf.Max(12f, moveCount * 3.5f);
        twoStarSeconds = Mathf.Max(18f, moveCount * 5f);
    }

    private bool IsValidLevelIndex(int levelIndex) =>
        levelIndex >= 0 && levelIndex < _sessionLevels.Count;
}

// Kullanacak scriptler: GameManager (StartLevel çağrısı — session çözümü + grid seed),
//                       StartupMenuUI (map verisi ve level yükleme)
