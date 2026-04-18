using System.Collections;
using UnityEngine;

/// <summary>
/// SFX ve müzik yönetimi. Plan §7.13.
///
/// SFX  — GameManager event'leri + CheckpointManager/HintManager doğrudan çağrısı.
/// Müzik — İki AudioSource arasında 1 saniyelik crossfade.
/// On/off — ProgressionService.AudioEnabled (PlayerPrefs["AudioEnabled"]).
///
/// DefaultExecutionOrder(0): GameManager(10) Start'tan önce subscribe olur.
/// </summary>
[DefaultExecutionOrder(0)]
public class AudioManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static AudioManager Instance { get; private set; }

    // ── Inspector — SFX ───────────────────────────────────────────────────────

    [Header("SFX Clips")]
    [SerializeField] private AudioClip _sfxCellSelect;    // geçerli adım
    [SerializeField] private AudioClip _sfxInvalidSwipe;  // geçersiz yön
    [SerializeField] private AudioClip _sfxUndo;          // undo
[SerializeField] private AudioClip _sfxLevelComplete; // level kazanıldı
    [SerializeField] private AudioClip _sfxHintReveal;    // hint açıldı

    [Header("Music Clips")]
    [SerializeField] private AudioClip _musicMainMenu;
    [SerializeField] private AudioClip _musicGameplay;

    [Header("Volumes")]
    [SerializeField] [Range(0f, 1f)] private float _sfxVolume   = 0.8f;
    [SerializeField] [Range(0f, 1f)] private float _musicVolume = 0.5f;

    [Header("Crossfade")]
    [SerializeField] private float _crossfadeDuration = 1f;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private AudioSource _sfxSource;
    private AudioSource _musicA;
    private AudioSource _musicB;
    private AudioSource _activeMusic;   // o an çalan kaynak
    private bool        _audioEnabled;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _sfxSource = CreateSource(loop: false);
        _musicA    = CreateSource(loop: true);
        _musicB    = CreateSource(loop: true);
        _activeMusic = _musicA;  // başlangıçta ikisi de sessiz
    }

    private void Start()
    {
        // PlayerPrefs durumunu oku
        _audioEnabled = ProgressionService.Instance != null
            ? ProgressionService.Instance.AudioEnabled
            : true;

        // GameManager event'leri
        GameManager.Instance.OnStepTaken    += OnStepTaken;
        GameManager.Instance.OnLevelComplete += OnLevelComplete;
        GameManager.Instance.OnMoveFailed    += OnMoveFailed;
        GameManager.Instance.OnUndoPerformed += OnUndoPerformed;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnStepTaken    -= OnStepTaken;
        GameManager.Instance.OnLevelComplete -= OnLevelComplete;
        GameManager.Instance.OnMoveFailed    -= OnMoveFailed;
        GameManager.Instance.OnUndoPerformed -= OnUndoPerformed;
    }

    // ── GameManager event handlers ────────────────────────────────────────────

    private void OnStepTaken(int _)    => PlaySFX(_sfxCellSelect);
    private void OnLevelComplete()     => PlaySFX(_sfxLevelComplete);
    private void OnMoveFailed(MoveOutcome _) => PlaySFX(_sfxInvalidSwipe);
    private void OnUndoPerformed()     => PlaySFX(_sfxUndo);

    // ── Public SFX — HintManager çağırır ────────────────────────────────────

    public void PlayHintReveal()  => PlaySFX(_sfxHintReveal);

    // ── Music API ─────────────────────────────────────────────────────────────

    /// <summary>Ana menü müziğine geç (1 saniyelik crossfade).</summary>
    public void PlayMainMenuMusic()  => CrossfadeTo(_musicMainMenu);

    /// <summary>Gameplay müziğine geç (1 saniyelik crossfade).</summary>
    public void PlayGameplayMusic()  => CrossfadeTo(_musicGameplay);

    /// <summary>Dışarıdan verilen clip'i çal (Resources.Load ile yüklenen clip'ler için).</summary>
    public void PlayMusicClip(AudioClip clip) => CrossfadeTo(clip);

    public void StopMusic()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOut(_activeMusic, _crossfadeDuration));
    }

    // ── On/Off ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ayarlar ekranından çağrılır; ProgressionService'i de günceller.
    /// </summary>
    public void SetAudioEnabled(bool enabled)
    {
        _audioEnabled = enabled;
        ProgressionService.Instance?.SetAudioEnabled(enabled);

        if (!enabled)
        {
            _sfxSource.Stop();
            StopAllCoroutines();
            _musicA.volume = 0f;
            _musicB.volume = 0f;
        }
        else
        {
            // Aktif kaynak sessiz kaldıysa sesi aç
            _activeMusic.volume = _musicVolume;
        }
    }

    public bool IsAudioEnabled => _audioEnabled;

    // ── Internals ─────────────────────────────────────────────────────────────

    private void PlaySFX(AudioClip clip)
    {
        if (!_audioEnabled || clip == null) return;
        _sfxSource.PlayOneShot(clip, _sfxVolume);
    }

    private void CrossfadeTo(AudioClip clip)
    {
        if (clip == null) return;

        // Aynı klip zaten çalıyorsa yeniden başlatma
        if (_activeMusic.clip == clip && _activeMusic.isPlaying) return;

        AudioSource next = (_activeMusic == _musicA) ? _musicB : _musicA;
        next.clip   = clip;
        next.volume = 0f;
        next.Play();

        StopAllCoroutines();
        StartCoroutine(Crossfade(_activeMusic, next));
        _activeMusic = next;
    }

    private IEnumerator Crossfade(AudioSource from, AudioSource to)
    {
        float startVolume = from.volume;
        float targetVolume = _audioEnabled ? _musicVolume : 0f;
        float elapsed = 0f;

        while (elapsed < _crossfadeDuration)
        {
            elapsed     += Time.deltaTime;
            float t      = elapsed / _crossfadeDuration;
            from.volume  = Mathf.Lerp(startVolume, 0f, t);
            to.volume    = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }

        from.volume = 0f;
        to.volume   = targetVolume;
        from.Stop();
    }

    private IEnumerator FadeOut(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float elapsed     = 0f;

        while (elapsed < duration)
        {
            elapsed     += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        source.volume = 0f;
        source.Stop();
    }

    private AudioSource CreateSource(bool loop)
    {
        var src  = gameObject.AddComponent<AudioSource>();
        src.loop         = loop;
        src.playOnAwake  = false;
        src.volume       = 0f;
        return src;
    }
}

// Kullanacak scriptler: HintManager (PlayHintReveal),
//                       MainMenu / GameBootstrap (PlayMainMenuMusic, PlayGameplayMusic),
//                       Ayarlar UI (SetAudioEnabled)
