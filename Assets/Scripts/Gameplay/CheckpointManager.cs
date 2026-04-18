using System.Collections;
using UnityEngine;

/// <summary>
/// Oyuncunun çözüm yolunun %50'sine ulaştığı anda checkpoint tetikler:
///   - GameManager.SetCheckpointLock() ile undo kilidi koyar.
///   - Toast gösterir.
/// Plan §7.5, §8.6 (Saat 28-34).
///
/// DefaultExecutionOrder(0): GameManager(10)'dan önce Start çalışır,
/// event subscription'lar GameManager.OnLevelStarted ateşlenmeden hazır olur.
/// </summary>
[DefaultExecutionOrder(0)]
public class CheckpointManager : MonoBehaviour
{
    [Header("Toast UI (opsiyonel — null bırakılabilir)")]
    [SerializeField] private GameObject        _toastPanel;
    [SerializeField] private float             _toastDuration   = 2f;
    [SerializeField] private CanvasGroup       _toastCanvasGroup; // null ise sadece SetActive

    [Header("Eşik")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float _threshold = 0.5f;  // %50

    // ── State ─────────────────────────────────────────────────────────────────

    private bool _triggered;
    private int  _solutionLength;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        GameManager.Instance.OnLevelStarted += HandleLevelStarted;
        GameManager.Instance.OnStepTaken    += HandleStepTaken;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnLevelStarted -= HandleLevelStarted;
        GameManager.Instance.OnStepTaken    -= HandleStepTaken;
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void HandleLevelStarted(int solutionLength)
    {
        _solutionLength = solutionLength;
        _triggered      = false;
        HideToast();
    }

    private void HandleStepTaken(int currentPathLength)
    {
        if (_triggered || _solutionLength <= 0) return;

        float progress = (float)currentPathLength / _solutionLength;
        if (progress < _threshold) return;

        _triggered = true;

        // Undo kilidi: checkpoint öncesine geri dönülemez
        GameManager.Instance.SetCheckpointLock(currentPathLength);

        StopAllCoroutines();
        StartCoroutine(ShowToast());

        Debug.Log($"[CheckpointManager] Checkpoint! adım={currentPathLength}/{_solutionLength} ({progress:P0})");
    }

    // ── Toast ─────────────────────────────────────────────────────────────────

    private IEnumerator ShowToast()
    {
        if (_toastPanel == null) yield break;

        _toastPanel.SetActive(true);

        if (_toastCanvasGroup != null)
        {
            // Fade in
            yield return Fade(_toastCanvasGroup, 0f, 1f, 0.25f);
            yield return new WaitForSeconds(_toastDuration - 0.5f);
            // Fade out
            yield return Fade(_toastCanvasGroup, 1f, 0f, 0.25f);
        }
        else
        {
            yield return new WaitForSeconds(_toastDuration);
        }

        _toastPanel.SetActive(false);
    }

    private void HideToast()
    {
        StopAllCoroutines();
        if (_toastPanel != null) _toastPanel.SetActive(false);
        if (_toastCanvasGroup != null) _toastCanvasGroup.alpha = 0f;
    }

    private static IEnumerator Fade(CanvasGroup cg, float from, float to, float duration)
    {
        float elapsed = 0f;
        cg.alpha = from;
        while (elapsed < duration)
        {
            elapsed  += Time.deltaTime;
            cg.alpha  = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        cg.alpha = to;
    }
}

// Kullanacak scriptler: GameManager (OnLevelStarted, OnStepTaken event'lerini subscribe eder;
//                                    SetCheckpointLock çağrısı buradan gelir)
