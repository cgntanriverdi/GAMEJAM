using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Hint hakkı ekonomisi ve yol ipucu gösterimi.
///   - Her 3 başarılı levelda +1 hint (PlayerPrefs "HintCount").
///   - Hint butonuna basılınca çözüm yolunun ilk %50'si 2 saniye gösterilir.
/// Plan §7.5, §8.6 (Saat 28-34).
///
/// DefaultExecutionOrder(0): GameManager(10)'dan önce Start çalışır.
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
    [SerializeField] private float _revealDuration = 2f;

    [Header("UI (opsiyonel)")]
    [SerializeField] private TextMeshProUGUI   _hintCountText;   // header'daki "🔮 2" sayacı
    [SerializeField] private GameObject        _noHintFeedback;  // "hint yok" flash panel

    // ── State ─────────────────────────────────────────────────────────────────

    private bool _revealActive;

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

    // ── Public API (hint butonu çağırır) ──────────────────────────────────────

    /// <summary>
    /// Hint butonuna bağlanacak metod.
    /// Hint hakkı varsa pathin ilk yarısını _revealDuration saniye gösterir.
    /// </summary>
    public void UseHint()
    {
        if (_revealActive) return;  // zaten aktif

        if (HintCount <= 0)
        {
            OnNoHintsLeft();
            return;
        }

        PathSolution solution = GameManager.Instance.CurrentSolution;
        if (solution == null || solution.Cells == null || solution.Cells.Count == 0) return;

        // Hakkı harca
        PlayerPrefs.SetInt(HintCountKey, HintCount - 1);
        PlayerPrefs.Save();
        RefreshDisplay();

        // İlk %50 (en az 1 hücre)
        int halfCount  = Mathf.Max(1, solution.Cells.Count / 2);
        var hintCells  = solution.Cells.GetRange(0, halfCount);

        StartCoroutine(RevealAndHide(hintCells));
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private IEnumerator RevealAndHide(List<GridCoord> cells)
    {
        _revealActive = true;
        AudioManager.Instance?.PlayHintReveal();

        // Swipe input'u durdur: hint sırasında yanlışlıkla hareket edilmesin
        // (Input'u disable etmiyoruz — sadece görsel, oyuncu isterse oynayabilir)

        _gridManager.ShowHintHighlight(cells);

        yield return new WaitForSeconds(_revealDuration);

        // Highlight'ı temizle ve normal duruma dön
        _gridManager.ClearAllHighlights();
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
