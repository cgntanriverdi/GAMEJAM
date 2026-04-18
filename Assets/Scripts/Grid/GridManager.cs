using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grid hücrelerini instantiate eder, CellData dizisini yönetir ve
/// highlight/selection görsellerini CellView üzerinden tetikler.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Prefab & sizing")]
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private float gridWorldSize = 4f;

    public float CellSize { get; private set; }

    private int _width;
    private int _height;
    private Vector2 _gridOffset;
    private CellData[,] _cells;
    private CellView[,] _views;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>LevelDefinition'a göre grid'i sıfırdan oluşturur.</summary>
    public void Initialize(LevelDefinition def)
    {
        _width  = def.Width;
        _height = def.Height;

        CellSize = gridWorldSize / Mathf.Max(_width, _height);
        _gridOffset = new Vector2(
            -(_width  - 1) * CellSize * 0.5f,
             (_height - 1) * CellSize * 0.5f
        );

        _cells  = new CellData[_width, _height];
        _views  = new CellView[_width, _height];

        DestroyExistingCells();

        CellColor[] palette = BuildColorPalette(def.ActiveColorCount);

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                var coord = new GridCoord(x, y);
                var data  = new CellData
                {
                    Coord   = coord,
                    Color   = palette[Random.Range(0, palette.Length)],
                    IsStart = (x == 0 && y == 0),
                    IsEnd   = (x == _width - 1 && y == _height - 1)
                };
                _cells[x, y] = data;
                _views[x, y] = SpawnCell(data);
            }
        }
    }

    /// <summary>
    /// PathGenerator'ın döndürdüğü PathSolution'ı grid'e uygular:
    /// yol hücrelerinin rengini çözüme göre günceller.
    /// </summary>
    public void ApplySolution(PathSolution solution, LevelDefinition def)
    {
        CellColor[] palette = BuildColorPalette(def.ActiveColorCount);
        int pathLen = solution.Cells.Count;

        // Her yol hücresine renk ata (eşit dağılım, mod ile)
        for (int i = 0; i < pathLen; i++)
        {
            GridCoord coord = solution.Cells[i];
            CellColor color = palette[i % palette.Length];
            _cells[coord.X, coord.Y].Color = color;
            _views[coord.X, coord.Y].SetColor(color);
        }
    }

    /// <summary>Koordinata göre CellData döner; geçersiz koordinat için null.</summary>
    public CellData GetCell(GridCoord coord)
    {
        if (!IsInBounds(coord)) return null;
        return _cells[coord.X, coord.Y];
    }

    /// <summary>
    /// Belirtilen koordinatın sınır-içi 4 komşusunu döner.
    /// visitedPath verilirse ziyaret edilmiş hücreler sonuçtan çıkarılır.
    /// </summary>
    public List<GridCoord> GetValidNeighbors(GridCoord coord, ICollection<GridCoord> visitedPath = null)
    {
        var result = new List<GridCoord>(4);
        GridCoord[] candidates = { coord.Up(), coord.Down(), coord.Left(), coord.Right() };

        foreach (var c in candidates)
        {
            if (!IsInBounds(c)) continue;
            if (visitedPath != null && visitedPath.Contains(c)) continue;
            result.Add(c);
        }
        return result;
    }

    /// <summary>
    /// SwipeDirection'dan hedef komşu koordinatı hesaplar.
    /// Sınır dışıysa null döner.
    /// </summary>
    public GridCoord? GetNeighborInDirection(GridCoord from, SwipeDirection dir)
    {
        GridCoord next = dir switch
        {
            SwipeDirection.Up    => from.Up(),
            SwipeDirection.Down  => from.Down(),
            SwipeDirection.Left  => from.Left(),
            SwipeDirection.Right => from.Right(),
            _                    => from
        };
        return IsInBounds(next) ? next : (GridCoord?)null;
    }

    /// <summary>
    /// Oyuncunun mevcut pozisyonunu highlight eder;
    /// ulaşılabilir yönleri parlak, ziyaret edilmişleri soluk gösterir.
    /// </summary>
    public void RefreshDirectionalHighlights(GridCoord playerPos, ICollection<GridCoord> visitedPath)
    {
        // Önce tüm hücrelerin highlight'ını temizle
        foreach (var view in _views)
            view.SetHighlight(HighlightState.None);

        // Oyuncunun hücresini seçili göster
        _views[playerPos.X, playerPos.Y].SetHighlight(HighlightState.Selected);

        // Dört yönü değerlendir
        SwipeDirection[] directions = { SwipeDirection.Up, SwipeDirection.Down,
                                        SwipeDirection.Left, SwipeDirection.Right };
        foreach (var dir in directions)
        {
            GridCoord? neighbor = GetNeighborInDirection(playerPos, dir);
            if (neighbor == null)
                continue;  // grid sınırı — ok gösterme

            bool alreadyVisited = visitedPath != null && visitedPath.Contains(neighbor.Value);
            var state = alreadyVisited ? HighlightState.Dimmed : HighlightState.Reachable;
            _views[neighbor.Value.X, neighbor.Value.Y].SetHighlight(state);
        }
    }

    /// <summary>
    /// HintManager tarafından çağrılır: belirtilen hücreleri Hint rengiyle işaretler.
    /// ClearAllHighlights + GameManager.RestoreHighlights ile geri alınır.
    /// </summary>
    public void ShowHintHighlight(List<GridCoord> cells)
    {
        foreach (var coord in cells)
        {
            if (!IsInBounds(coord)) continue;
            _views[coord.X, coord.Y].SetHighlight(HighlightState.Hint);
        }
    }

    /// <summary>Tüm highlight'ları sıfırlar (level reset veya hint sonrası).</summary>
    public void ClearAllHighlights()
    {
        foreach (var view in _views)
            view.SetHighlight(HighlightState.None);
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private bool IsInBounds(GridCoord c) =>
        c.X >= 0 && c.X < _width && c.Y >= 0 && c.Y < _height;

    /// <summary>GridCoord'u dünya pozisyonuna çevirir. PlayerToken ve overlay'ler bunu kullanır.</summary>
    public Vector3 GetWorldPosition(GridCoord coord)
    {
        return transform.position + new Vector3(
            _gridOffset.x + coord.X * CellSize,
            _gridOffset.y - coord.Y * CellSize,
            0f
        );
    }

    private CellView SpawnCell(CellData data)
    {
        var worldPos = GetWorldPosition(data.Coord);
        var go       = Instantiate(cellPrefab, worldPos, Quaternion.identity, transform);
        go.name      = $"Cell_{data.Coord}";
        go.transform.localScale = new Vector3(CellSize, CellSize, 1f);
        var view     = go.GetComponent<CellView>();
        view.Initialize(data);
        return view;
    }

    private void DestroyExistingCells()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    private static CellColor[] BuildColorPalette(int activeCount)
    {
        // Enum değerlerinin ilk activeCount tanesini al
        CellColor[] all = (CellColor[])System.Enum.GetValues(typeof(CellColor));
        activeCount = Mathf.Clamp(activeCount, 1, all.Length);
        var palette = new CellColor[activeCount];
        System.Array.Copy(all, palette, activeCount);
        return palette;
    }
}

// Kullanacak scriptler: GameManager (Initialize, ApplySolution, RefreshDirectionalHighlights),
//                       RunValidator (GetCell, GetNeighborInDirection),
//                       SwipeInputController (GetNeighborInDirection)
