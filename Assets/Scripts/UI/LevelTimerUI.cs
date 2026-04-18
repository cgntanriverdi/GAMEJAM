using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public sealed class LevelTimerUI : MonoBehaviour
{
    private TextMeshProUGUI _timerText;
    private bool _isRunning;
    private float _startedAtUnscaled;
    private float _frozenElapsedSeconds;
    private int _lastDisplayedWholeSeconds = -1;

    private void Awake()
    {
        CacheReferences();
        ResetDisplay();
    }

    private void OnValidate()
    {
        CacheReferences();

        if (!Application.isPlaying)
            ForceDisplay(0f);
    }

    private void Update()
    {
        if (!_isRunning)
            return;

        ForceDisplay(Time.unscaledTime - _startedAtUnscaled);
    }

    public void ResetDisplay()
    {
        _isRunning = false;
        _startedAtUnscaled = 0f;
        _frozenElapsedSeconds = 0f;
        _lastDisplayedWholeSeconds = -1;
        ForceDisplay(0f);
    }

    public void StartTimer()
    {
        _frozenElapsedSeconds = 0f;
        _startedAtUnscaled = Time.unscaledTime;
        _isRunning = true;
        _lastDisplayedWholeSeconds = -1;
        ForceDisplay(0f);
    }

    public void StopTimer()
    {
        if (_isRunning)
            _frozenElapsedSeconds = Time.unscaledTime - _startedAtUnscaled;

        _isRunning = false;
        _lastDisplayedWholeSeconds = -1;
        ForceDisplay(_frozenElapsedSeconds);
    }

    private void CacheReferences()
    {
        if (_timerText == null)
            _timerText = GetComponent<TextMeshProUGUI>();
    }

    private void ForceDisplay(float elapsedSeconds)
    {
        if (_timerText == null)
            return;

        int wholeSeconds = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds));
        if (wholeSeconds == _lastDisplayedWholeSeconds)
            return;

        _lastDisplayedWholeSeconds = wholeSeconds;

        int minutes = wholeSeconds / 60;
        int seconds = wholeSeconds % 60;
        _timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
