using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oyun döngüsünün orkestrasyonu. Plan §7.5, §7.7, §7.9.
/// Akış: StartLevel → TryMovePlayer (swipe) / TryUndo (button) → OnWin.
/// DefaultExecutionOrder(10): CheckpointManager ve HintManager Start'larının
/// event subscription'ları bu Start'tan önce tamamlanmış olur.
/// </summary>
[DefaultExecutionOrder(10)]
public class GameManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Inspector references ──────────────────────────────────────────────────

    [Header("Systems")]
    [SerializeField] private GridManager          _gridManager;
    [SerializeField] private SwipeInputController _swipeInput;
    [SerializeField] private CounterPanelUI       _counterPanel;
    [SerializeField] private LevelTimerUI         _levelTimerUI;
    [SerializeField] private PlayerToken          _playerToken;

    [Header("Test level (LevelManager devralana kadar)")]
    [SerializeField] private int _width            = 5;
    [SerializeField] private int _height           = 5;
    [SerializeField] private int _minPathLength    = 6;
    [SerializeField] private int _maxPathLength    = 18;
    [SerializeField] private int _activeColorCount = 3;

    // ── Events (CheckpointManager ve HintManager dinler) ─────────────────────

    /// <summary>Her başarılı adımda: arg = mevcut seçili path uzunluğu.</summary>
    public event Action<int> OnStepTaken;

    /// <summary>StartLevel tamamlandığında: arg = toplam çözüm path uzunluğu.</summary>
    public event Action<int> OnLevelStarted;

    /// <summary>Win koşulu sağlandığında.</summary>
    public event Action OnLevelComplete;

    /// <summary>Win anında level sonucu hazır olduğunda.</summary>
    public event Action<LevelCompletionResult> OnLevelResultReady;

    /// <summary>Geçersiz hamle denendiğinde: arg = red sebebi.</summary>
    public event Action<MoveOutcome> OnMoveFailed;

    /// <summary>Başarılı undo sonrasında.</summary>
    public event Action OnUndoPerformed;

    // ── Properties (HintManager okur) ────────────────────────────────────────

    public PathSolution CurrentSolution => _solution;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private RunValidator   _validator;
    private PathSolution   _solution;
    private PlayerRunState _runState;
    private LevelDefinition _currentDef;

    private enum GameState { Idle, Playing, Won }
    private GameState _gameState = GameState.Idle;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        // LevelManager varsa o yönetir; yoksa test level başlat (editor/standalone).
        if (LevelManager.Instance == null && !StartupMenuUI.ShouldBlockAutoStart)
            StartLevel(BuildCurrentDef());
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// LevelManager veya Start() tarafından çağrılır.
    /// Grid'i sıfırlar, path üretir, tüm sistemi level'a hazırlar.
    /// curatedFallback: PathGenerator başarısız olursa LevelManager'ın sağladığı yedek.
    /// </summary>
    public void StartLevel(
        LevelDefinition def,
        PathSolution sessionSolution = null,
        int gridSeed = -1,
        PathSolution curatedFallback = null)
    {
        _currentDef = def;
        _gameState  = GameState.Idle;
        _levelTimerUI?.ResetDisplay();

        // 1. Grid oluştur
        _gridManager.Initialize(def, gridSeed);

        // 2. Session çözümü varsa doğrudan kullan; yoksa runtime'da üret.
        _solution = sessionSolution;
        if (_solution == null)
        {
            var generator = new PathGenerator();
            _solution = generator.Generate(def) ?? curatedFallback;
        }

        if (_solution == null)
        {
            Debug.LogError("[GameManager] Path üretilemedi ve fallback yok; level başlatılamadı.");
            return;
        }

        // 3. Grid'e renkleri uygula
        _gridManager.ApplySolution(_solution, def);

        // 4. Validator oluştur (GridManager referansı gerekli)
        _validator = new RunValidator(_gridManager);

        // 5. Run state'i başlat
        //    Start hücresi seçili; rengi ilk sayıma dahil (çözüm path'ine dahil).
        var startCoord = _solution.Cells[0];
        _runState = new PlayerRunState
        {
            SelectedPath       = new List<GridCoord> { startCoord },
            CurrentColorCounts = new Dictionary<CellColor, int>(),
        };

        // 6. UI güncelle
        _counterPanel.SetSpriteSource(_gridManager.GetCellSprite);
        _counterPanel.Initialize(_solution.TargetColorCounts);
        _counterPanel.Refresh(_runState.CurrentColorCounts);

        // 7. Input ve highlight başlat
        _swipeInput.SetPlayerPosition(startCoord);
        _swipeInput.SetInputEnabled(true);
        _gridManager.RefreshDirectionalHighlights(startCoord, _runState.SelectedPath);
        if (_playerToken != null)
        {
            var sr = _playerToken.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                float naturalSize = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
                float targetSize  = _gridManager.CellSize * 0.7f;
                float scale       = targetSize / naturalSize;
                _playerToken.transform.localScale = new Vector3(scale, scale, 1f);
            }
            _playerToken.Teleport(_gridManager.GetWorldPosition(startCoord));
        }

        _gameState = GameState.Playing;
        _levelTimerUI?.StartTimer();
        OnLevelStarted?.Invoke(_solution.Cells.Count);
    }

    public void BeginGameplay()
    {
        if (_gameState == GameState.Playing)
            return;

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadCurrentLevel();
            return;
        }

        StartLevel(BuildCurrentDef());
    }

    /// <summary>
    /// SwipeInputController tarafından her swipe'ta çağrılır.
    /// Plan §7.9 akışı: komşuluk → ziyaret → renk → bitiş.
    /// </summary>
    public void TryMovePlayer(GridCoord target)
    {
        if (_gameState != GameState.Playing) return;

        ValidationResult result = _validator.ValidateMove(target, _runState, _solution);

        switch (result.Outcome)
        {
            case MoveOutcome.Valid:
                CommitMove(target, result.ProjectedCounts);
                break;

            case MoveOutcome.Win:
                CommitMove(target, result.ProjectedCounts);
                OnWin();
                break;

            // End cell'e erken ulaşıldı — hamle geçerli ama win yok; eksik renkler vurgulanır.
            case MoveOutcome.EndReachedButIncomplete:
                CommitMove(target, result.ProjectedCounts);
                _counterPanel.TriggerIncompleteEndFeedback(_runState.CurrentColorCounts);
                break;

            case MoveOutcome.InvalidColorOverflow:
                // Hangi renk taştı: hedefi aşan ilk rengi bul
                CellColor overflowColor = FindOverflowColor(result, target);
                _counterPanel.TriggerOverflowFeedback(overflowColor);
                OnInvalidMove(result.Outcome);
                break;

            case MoveOutcome.InvalidNotNeighbor:
            case MoveOutcome.InvalidAlreadyVisited:
                OnInvalidMove(result.Outcome);
                break;
        }
    }

    /// <summary>
    /// Undo butonu tarafından çağrılır (Plan §7.6.4 — sadece buton, swipe yok).
    /// </summary>
    public void TryUndo()
    {
        if (_gameState != GameState.Playing) return;

        ValidationResult result = _validator.ValidateUndo(_runState);
        if (!result.IsAccepted)
        {
            OnInvalidMove(result.Outcome);
            return;
        }

        // Son hücreyi çıkar
        int lastIdx = _runState.SelectedPath.Count - 1;
        _runState.SelectedPath.RemoveAt(lastIdx);

        // Tüm path'i yeniden say — undo sonrası tek güvenli yöntem
        _runState.CurrentColorCounts = RecomputeCountsFromPath();

        GridCoord newCurrent = _runState.SelectedPath[_runState.SelectedPath.Count - 1];
        _swipeInput.SetPlayerPosition(newCurrent);
        _gridManager.RefreshDirectionalHighlights(newCurrent, _runState.SelectedPath);
        _counterPanel.Refresh(_runState.CurrentColorCounts);
        _playerToken?.MoveTo(_gridManager.GetWorldPosition(newCurrent));
        OnUndoPerformed?.Invoke();
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private void CommitMove(GridCoord coord, Dictionary<CellColor, int> newCounts)
    {
        _runState.SelectedPath.Add(coord);
        _runState.CurrentColorCounts = newCounts;

        _swipeInput.SetPlayerPosition(coord);
        _gridManager.RefreshDirectionalHighlights(coord, _runState.SelectedPath);
        _counterPanel.Refresh(newCounts);
        _playerToken?.MoveTo(_gridManager.GetWorldPosition(coord));
        OnStepTaken?.Invoke(_runState.SelectedPath.Count);
    }

    /// <summary>
    /// HintManager, hint fade-out sonrası bu metodu çağırır.
    /// Mevcut oyuncu pozisyonuna göre yönsel highlight'ları geri yükler.
    /// </summary>
    public void RestoreHighlights()
    {
        if (_runState == null || _gameState != GameState.Playing) return;
        GridCoord current = _runState.SelectedPath[_runState.SelectedPath.Count - 1];
        _gridManager.RefreshDirectionalHighlights(current, _runState.SelectedPath);
    }

    private void OnWin()
    {
        _gameState = GameState.Won;
        _swipeInput.SetInputEnabled(false);
        _gridManager.ClearAllHighlights();
        _counterPanel.SetAllComplete();
        float elapsedSeconds = _levelTimerUI != null
            ? _levelTimerUI.StopTimer()
            : 0f;

        LevelCompletionResult result = CreateCompletionResult(elapsedSeconds);

        OnLevelResultReady?.Invoke(result);
        OnLevelComplete?.Invoke();
        Debug.Log("[GameManager] ✓ WIN!");
    }

    private void OnInvalidMove(MoveOutcome reason)
    {
        OnMoveFailed?.Invoke(reason);
        Debug.Log($"[GameManager] Geçersiz hamle: {reason}");
    }

    /// <summary>
    /// InvalidColorOverflow durumunda hangi rengin taştığını tespit eder.
    /// ProjectedCounts null olduğu için target hücrenin rengini doğrudan kullanır.
    /// </summary>
    private CellColor FindOverflowColor(ValidationResult result, GridCoord target)
    {
        CellData cell = _gridManager.GetCell(target);
        return cell != null ? cell.Color : CellColor.Red;
    }

    /// <summary>Undo sonrası mevcut SelectedPath'ten renk sayılarını yeniden hesaplar.
    /// Start (index 0) ve End hücreleri sayılmaz.</summary>
    private Dictionary<CellColor, int> RecomputeCountsFromPath()
    {
        var counts = new Dictionary<CellColor, int>();
        for (int i = 1; i < _runState.SelectedPath.Count; i++)
        {
            CellData cell = _gridManager.GetCell(_runState.SelectedPath[i]);
            if (cell == null || cell.IsEnd) continue;
            counts.TryGetValue(cell.Color, out int prev);
            counts[cell.Color] = prev + 1;
        }
        return counts;
    }

    private static Dictionary<CellColor, int> InitialCounts(CellData startCell)
    {
        if (startCell == null) return new Dictionary<CellColor, int>();
        return new Dictionary<CellColor, int> { { startCell.Color, 1 } };
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private LevelDefinition BuildCurrentDef() => new LevelDefinition
    {
        LevelIndex         = 0,
        Width              = _width,
        Height             = _height,
        MinPathLength      = _minPathLength,
        MaxPathLength      = _maxPathLength,
        ActiveColorCount   = _activeColorCount,
        AllowGeneratedLevel = true
    };

    private LevelCompletionResult CreateCompletionResult(float elapsedSeconds)
    {
        int levelIndex = _currentDef != null ? _currentDef.LevelIndex : 0;

        if (LevelManager.Instance != null)
        {
            int stars = LevelManager.Instance.CalculateStars(levelIndex, elapsedSeconds);
            return LevelManager.Instance.RecordLevelResult(levelIndex, elapsedSeconds, stars);
        }

        int starsFallback = CalculateFallbackStars(elapsedSeconds);
        return new LevelCompletionResult(levelIndex, elapsedSeconds, starsFallback, starsFallback, elapsedSeconds, true);
    }

    private int CalculateFallbackStars(float elapsedSeconds)
    {
        int moveCount = _solution != null && _solution.Cells != null
            ? Mathf.Max(1, _solution.Cells.Count - 1)
            : Mathf.Max(1, _minPathLength + 1);

        float threeStarSeconds = Mathf.Max(12f, moveCount * 3.5f);
        float twoStarSeconds = Mathf.Max(18f, moveCount * 5f);

        if (elapsedSeconds <= threeStarSeconds) return 3;
        if (elapsedSeconds <= twoStarSeconds)   return 2;
        return 1;
    }

    [ContextMenu("Restart Level")]
    private void DebugRestartLevel()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.ReloadActiveLevel())
            return;

        StartLevel(BuildCurrentDef());
    }
}

// Kullanacak scriptler: SwipeInputController (TryMovePlayer çağrısı),
//                       LevelManager (StartLevel çağrısı),
//                       HintManager (SetInputEnabled üzerinden)
