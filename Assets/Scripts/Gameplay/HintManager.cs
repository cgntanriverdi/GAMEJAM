using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Hint hakkı ekonomisi ve yol ipucu gösterimi.
///   - Her 3 başarılı levelda +1 hint (PlayerPrefs "HintCount").
///   - Oyuncu çözüm path'indeyse: kalan yolun ortasına altın indikatör.
///   - Değilse: adım adım undo ile çözüm path'ine döndür, sonra indikatör.
/// </summary>
[DefaultExecutionOrder(0)]
public class HintManager : MonoBehaviour
{
    // ── PlayerPrefs keys ──────────────────────────────────────────────────────

    private const string HintCountKey      = "HintCount";
    private const string LevelsSinceHintKey = "LevelsSinceHint";
    private const int    LevelsPerHint      = 3;

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("References")]
    [SerializeField] private GridManager       _gridManager;

    [Header("Reveal")]
    [SerializeField] private float _revealDuration  = 2f;
    [SerializeField] private float _undoStepDelay   = 0.18f;

    [Header("Indicator sprite (altın rengi ile gösterilir)")]
    [SerializeField] private Sprite _indicatorSprite;

    [Header("UI (opsiyonel)")]
    [SerializeField] private TextMeshProUGUI   _hintCountText;
    [SerializeField] private GameObject        _noHintFeedback;

    // ── State ─────────────────────────────────────────────────────────────────

    private bool _revealActive;
    private GridCoord _lastIndicatorCoord;

    // ── Public property ───────────────────────────────────────────────────────

    public int HintCount => PlayerPrefs.GetInt(HintCountKey, 0);

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        GameManager.Instance.OnLevelComplete += HandleLevelComplete;
        RefreshDisplay();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnLevelComplete -= HandleLevelComplete;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void UseHint()
    {
        if (_revealActive) return;

        if (HintCount <= 0)
        {
            OnNoHintsLeft();
            return;
        }

        PathSolution solution = GameManager.Instance.CurrentSolution;
        if (solution == null || solution.Cells == null || solution.Cells.Count < 2) return;

        PlayerPrefs.SetInt(HintCountKey, HintCount - 1);
        PlayerPrefs.Save();
        RefreshDisplay();

        StartCoroutine(HintRoutine(solution));
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private IEnumerator HintRoutine(PathSolution solution)
    {
        _revealActive = true;
        AudioManager.Instance?.PlayHintReveal();
        GameManager.Instance.SetPlayerInputEnabled(false);

        // Eğer oyuncu çözüm path'i dışındaysa, adım adım geri sar
        var solutionSet = new HashSet<GridCoord>(solution.Cells);
        while (true)
        {
            var path = GameManager.Instance.CurrentPlayerPath;
            if (path == null || path.Count <= 1) break;

            GridCoord cur = path[path.Count - 1];
            if (solutionSet.Contains(cur)) break;

            GameManager.Instance.TryUndo();
            yield return new WaitForSeconds(_undoStepDelay);
        }

        // Çözüm path'indeki mevcut konumu bul
        var finalPath = GameManager.Instance.CurrentPlayerPath;
        if (finalPath == null || finalPath.Count == 0)
        {
            GameManager.Instance.SetPlayerInputEnabled(true);
            _revealActive = false;
            yield break;
        }

        GridCoord currentPos = finalPath[finalPath.Count - 1];
        int solutionIdx = solution.Cells.IndexOf(currentPos);
        if (solutionIdx < 0) solutionIdx = 0;

        // Kalan yolun (currentPos sonrası) tam ortası
        int remaining = solution.Cells.Count - 1 - solutionIdx;
        if (remaining <= 0)
        {
            GameManager.Instance.SetPlayerInputEnabled(true);
            _revealActive = false;
            yield break;
        }

        int midIdx = Mathf.Clamp(solutionIdx + 1 + remaining / 2,
                                  solutionIdx + 1,
                                  solution.Cells.Count - 1);
        _lastIndicatorCoord = solution.Cells[midIdx];

        GameManager.Instance.SetPlayerInputEnabled(true);
        _gridManager.ShowHintTarget(_lastIndicatorCoord, _indicatorSprite);

        yield return new WaitForSeconds(_revealDuration);

        _gridManager.HideHintTarget(_lastIndicatorCoord);
        GameManager.Instance.RestoreHighlights();
        _revealActive = false;
    }

    private void HandleLevelComplete()
    {
        int levelsSince = PlayerPrefs.GetInt(LevelsSinceHintKey, 0) + 1;

        if (levelsSince >= LevelsPerHint)
        {
            levelsSince = 0;
            int newCount = HintCount + 1;
            PlayerPrefs.SetInt(HintCountKey, newCount);
            Debug.Log($"[HintManager] +1 hint kazanıldı! Toplam: {newCount}");
            // TODO: "Hint Kazandın!" toast animasyonu
        }

        PlayerPrefs.SetInt(LevelsSinceHintKey, levelsSince);
        PlayerPrefs.Save();
        RefreshDisplay();
    }

    private void OnNoHintsLeft()
    {
        Debug.Log("[HintManager] Hint hakkı kalmadı.");
        if (_noHintFeedback != null)
            StartCoroutine(FlashNoHint());
        // TODO: SFX
    }

    private IEnumerator FlashNoHint()
    {
        _noHintFeedback.SetActive(true);
        yield return new WaitForSeconds(0.8f);
        _noHintFeedback.SetActive(false);
    }

    private void RefreshDisplay()
    {
        if (_hintCountText != null)
            _hintCountText.text = HintCount.ToString();
    }
}

// Kullanacak scriptler: GameManager (OnLevelComplete event'i, CurrentSolution property, RestoreHighlights),
//                       GridManager (ShowHintHighlight, ClearAllHighlights),
//                       UI Hint butonu (UseHint() çağrısı)
