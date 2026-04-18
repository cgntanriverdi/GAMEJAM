using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Level sırasını yönetir; LevelDefinitionAsset'lerden veya dahili
/// tier tablosundan LevelDefinition üretir ve GameManager'a iletir.
/// PathGenerator başarısız olursa: curated asset → snake pattern fallback.
/// Plan §7.5, §8.7.
///
/// DefaultExecutionOrder(0):
///   ProgressionService(-10).Start'tan sonra,
///   GameManager(10).Start'tan önce çalışır.
///   → ProgressionService.CurrentLevel hazır; StartLevel henüz ateşlenmemiş.
/// </summary>
[DefaultExecutionOrder(0)]
public class LevelManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static LevelManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Level assets (boş slotlar tier tablosuna düşer)")]
    [SerializeField] private LevelDefinitionAsset[] _levelAssets;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // ProgressionService(-10) OnLevelComplete'e ilk abone → CurrentLevel önceden artar.
        // LevelManager(0) ikinci abone → doğru level'ı yükler.
        GameManager.Instance.OnLevelComplete += HandleLevelComplete;
        LoadCurrentLevel();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelComplete -= HandleLevelComplete;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// ProgressionService.CurrentLevel'a göre level'ı yükler.
    /// GameManager.StartLevel'a hem LevelDefinition hem fallback iletilir.
    /// </summary>
    public void LoadCurrentLevel()
    {
        int idx = ProgressionService.Instance.CurrentLevel;
        LevelDefinition  def      = GetDefinition(idx);
        PathSolution     fallback = GetFallback(def);

        GameManager.Instance.StartLevel(def, fallback);

        Debug.Log($"[LevelManager] Level {idx + 1} yüklendi — " +
                  $"{def.Width}x{def.Height}, minPath={def.MinPathLength}, renkler={def.ActiveColorCount}");
    }

    // ── Level config ──────────────────────────────────────────────────────────

    private LevelDefinition GetDefinition(int idx)
    {
        if (idx < _levelAssets.Length && _levelAssets[idx] != null)
            return _levelAssets[idx].Definition;

        return BuildTierDefinition(idx);
    }

    /// <summary>
    /// Tier tablosu — Plan §8.7:
    ///   1-3  : 4×4, 5-7 adım, 2 renk
    ///   4-6  : 5×5, 8-10 adım, 3 renk
    ///   7-10 : 6×6, 11-14 adım, 4 renk
    ///   11+  : 7×7, 15+ adım, 4 renk
    /// </summary>
    private static LevelDefinition BuildTierDefinition(int idx)
    {
        int level = idx + 1;  // 1-tabanlı gösterim

        if (level <= 3)
            return Make(idx, w: 4, h: 4, min: 5,  max: 7,  colors: 2);
        if (level <= 6)
            return Make(idx, w: 5, h: 5, min: 8,  max: 10, colors: 3);
        if (level <= 10)
            return Make(idx, w: 6, h: 6, min: 11, max: 14, colors: 4);

        // Level 11+: artan zorluk — her 5 levelda max +2 adım
        int bonus = ((level - 11) / 5) * 2;
        return Make(idx, w: 7, h: 7, min: 15 + bonus, max: 20 + bonus, colors: 4);
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

    private PathSolution GetFallback(LevelDefinition def)
    {
        int idx = def.LevelIndex;

        // 1. Önce asset curated path
        if (idx < _levelAssets.Length && _levelAssets[idx]?.HasCuratedPath == true)
            return _levelAssets[idx].BuildCuratedSolution();

        // 2. Garantili snake pattern
        return BuildSnakeFallback(def);
    }

    /// <summary>
    /// Grid üzerinde satır-satır yılan şeklinde ilerler ve
    /// End hücresi (width-1, height-1)'e ulaşınca durur.
    /// Her grid boyutu ve yüksekliği için garantili çalışır.
    /// </summary>
    private static PathSolution BuildSnakeFallback(LevelDefinition def)
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

        CellColor[] palette = BuildPalette(def.ActiveColorCount);
        var counts          = new Dictionary<CellColor, int>();
        foreach (var c in palette) counts[c] = 0;
        for (int i = 0; i < path.Count; i++)
            counts[palette[i % palette.Length]]++;

        return new PathSolution { Cells = path, TargetColorCounts = counts };
    }

    private static CellColor[] BuildPalette(int activeCount)
    {
        CellColor[] all = (CellColor[])System.Enum.GetValues(typeof(CellColor));
        activeCount = Mathf.Clamp(activeCount, 1, all.Length);
        var palette = new CellColor[activeCount];
        System.Array.Copy(all, palette, activeCount);
        return palette;
    }

    // ── Event handler ─────────────────────────────────────────────────────────

    private void HandleLevelComplete()
    {
        // ProgressionService(-10) önceden CurrentLevel'ı artırdı; sadece yükle.
        LoadCurrentLevel();
    }
}

// Kullanacak scriptler: GameManager (StartLevel çağrısı — LevelDefinition + fallback),
//                       ProgressionService (CurrentLevel okur),
//                       MainMenu (LoadCurrentLevel çağrısı)
