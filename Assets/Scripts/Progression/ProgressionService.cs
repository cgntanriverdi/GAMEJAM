using UnityEngine;

/// <summary>
/// Tek kayıt noktası: PlayerPrefs üstündeki kalıcı ayarlar burada tutulur.
/// Session map ilerlemesi burada saklanmaz; yalnızca hint ve audio gibi kalıcı veriler kalır.
///
/// DefaultExecutionOrder(-10): LevelManager(0) ve GameManager(10) Start'larından
/// önce hem Awake hem Start tamamlanır; CurrentLevel her zaman hazırdır.
/// </summary>
[DefaultExecutionOrder(-10)]
public class ProgressionService : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static ProgressionService Instance { get; private set; }

    // ── PlayerPrefs keys ──────────────────────────────────────────────────────

    private const string KeyCurrentLevel = "CurrentLevel";
    private const string KeyTotalCleared = "TotalCleared";
    private const string KeyHintCount    = "HintCount";
    private const string KeyAudioEnabled = "AudioEnabled";

    // ── Cached state ──────────────────────────────────────────────────────────

    public int  CurrentLevel { get; private set; }
    public int  TotalCleared { get; private set; }
    public int  HintCount    { get; private set; }
    public bool AudioEnabled { get; private set; }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    private void Start()
    {
        // Session-only level map akışı kalıcı CurrentLevel ilerletmiyor.
    }

    // ── Hint API (HintManager tarafından kullanılır) ──────────────────────────

    /// <summary>Hint hakkı varsa tüketir ve true döner.</summary>
    public bool TryUseHint()
    {
        if (HintCount <= 0) return false;
        HintCount--;
        Save();
        return true;
    }

    public void AddHint(int amount = 1)
    {
        HintCount += amount;
        Save();
    }

    // ── Audio API ─────────────────────────────────────────────────────────────

    public void SetAudioEnabled(bool enabled)
    {
        AudioEnabled = enabled;
        Save();
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    private void Load()
    {
        CurrentLevel = PlayerPrefs.GetInt(KeyCurrentLevel, 0);
        TotalCleared = PlayerPrefs.GetInt(KeyTotalCleared, 0);
        HintCount    = PlayerPrefs.GetInt(KeyHintCount,    0);
        AudioEnabled = PlayerPrefs.GetInt(KeyAudioEnabled, 1) == 1;
    }

    private void Save()
    {
        PlayerPrefs.SetInt(KeyCurrentLevel, CurrentLevel);
        PlayerPrefs.SetInt(KeyTotalCleared, TotalCleared);
        PlayerPrefs.SetInt(KeyHintCount,    HintCount);
        PlayerPrefs.SetInt(KeyAudioEnabled, AudioEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ── Debug ─────────────────────────────────────────────────────────────────

    [ContextMenu("Reset All Progress")]
    public void ResetAll()
    {
        CurrentLevel = 0;
        TotalCleared = 0;
        HintCount    = 0;
        AudioEnabled = true;
        Save();
        Debug.Log("[ProgressionService] Tüm ilerleme sıfırlandı.");
    }
}

// Kullanacak scriptler: HintManager (TryUseHint, AddHint),
//                       AudioManager (AudioEnabled),
//                       opsiyonel debug/UI akışları
