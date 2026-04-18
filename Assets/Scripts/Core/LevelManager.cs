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

        // Sadece orta hücreler sayılır (start=0 ve end=son hariç)
        for (int i = 1; i < path.Count - 1; i++)
        {
            var c = palette[(i - 1) % palette.Length];
            counts.TryGetValue(c, out int prev);
            counts[c] = prev + 1;
        }

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
