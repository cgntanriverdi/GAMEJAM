using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grid hücrelerini instantiate eder, CellData dizisini yönetir ve
/// highlight/selection görsellerini CellView üzerinden tetikler.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Prefab & sizing")]
    [SerializeField] private GameObject cellPrefab;   // CellView bileşeni içermeli
    [SerializeField] private float gridWorldSize = 8f; // grid'in toplam dünya boyutu (sabit)
    [SerializeField] private float spacingRatio  = 0.05f; // cellSize'ın yüzdesi olarak boşluk

    private float cellSize;    // Initialize'da hesaplanır
    private float cellSpacing; // Initialize'da hesaplanır

    public float CellSize => cellSize;

    public Sprite GetCellSprite(CellColor color)
    {
        var view = cellPrefab != null ? cellPrefab.GetComponent<CellView>() : null;
        return view != null ? view.GetSprite(color) : null;
    }

    [Header("End sprite")]
    [SerializeField] private Sprite boneSprite;

    private int _width;
    private int _height;
    private CellData[,] _cells;
    private CellView[,] _views;  // visual bileşenler (CellView.cs ayrı implement edilecek)

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>LevelDefinition'a göre grid'i sıfırdan oluşturur.</summary>
    public void Initialize(LevelDefinition def, int seed = -1)
    {
        if (seed >= 0)
            Random.InitState(seed);

        _width  = def.Width;
        _height = def.Height;
        _cells  = new CellData[_width, _height];
        _views  = new CellView[_width, _height];

        // Hücre boyutunu grid'in toplam dünya boyutuna göre hesapla
        int maxDim    = Mathf.Max(_width, _height);
        cellSize      = gridWorldSize / maxDim;
        cellSpacing   = cellSize * spacingRatio;

        // Grid'i ortalamak için başlangıç offset'ini hesapla
        float step    = cellSize + cellSpacing;
        float offsetX = -((_width  - 1) * step) / 2f;
        float offsetY =  ((_height - 1) * step) / 2f;
        transform.localPosition = new Vector3(offsetX, offsetY, transform.localPosition.z);

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
                    IsStart = false,
                    IsEnd   = false
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
        // Önceki start/end bayraklarını temizle
        foreach (var cell in _cells)
        {
            cell.IsStart = false;
            cell.IsEnd   = false;
        }

        // Gerçek start ve end'i işaretle
        GridCoord startCoord = solution.Cells[0];
        GridCoord endCoord   = solution.Cells[solution.Cells.Count - 1];
        _cells[startCoord.X, startCoord.Y].IsStart = true;
        _cells[endCoord.X,   endCoord.Y  ].IsEnd   = true;

        // Renkleri PathColors'tan uygula (path'ten türetilmiş, random)
        foreach (var kvp in solution.PathColors)
        {
            _cells[kvp.Key.X, kvp.Key.Y].Color = kvp.Value;
            _views[kvp.Key.X, kvp.Key.Y].SetColor(kvp.Value);
        }

        // Start hücresini gri, end hücresini mor göster
        _views[startCoord.X, startCoord.Y].SetAsStart();
        _views[endCoord.X,   endCoord.Y  ].SetAsEnd();

        // End hücresine bone overlay — hücrenin %70'i kadar
        if (boneSprite != null)
            _views[endCoord.X, endCoord.Y].ShowOverlay(boneSprite, cellSize * 0.7f);
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

    /// <summary>GridCoord'u world-space pozisyona çevirir (PlayerToken için).</summary>
    public Vector3 GetWorldPosition(GridCoord coord)
    {
        float step     = cellSize + cellSpacing;
        var localPos   = new Vector3(coord.X * step, -coord.Y * step, 0f);
        return transform.TransformPoint(localPos);
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private bool IsInBounds(GridCoord c) =>
        c.X >= 0 && c.X < _width && c.Y >= 0 && c.Y < _height;

    private CellView SpawnCell(CellData data)
    {
        float step     = cellSize + cellSpacing;
        var localPos   = new Vector3(data.Coord.X * step, -data.Coord.Y * step, 0f);
        var go         = Instantiate(cellPrefab, transform);
        go.transform.localPosition = localPos;
        go.name      = $"Cell_{data.Coord}";
        go.transform.localScale = new Vector3(cellSize, cellSize, 1f);
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
