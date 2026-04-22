using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PC web'de portrait oyunu landscape ekrana düzgün sığdırır.
/// Canvas Scaler, Expand modunda olmalı (sahne YAML'ında ayarlı).
/// Canvas'ın üzerinde component olarak veya child olarak çalışır.
/// </summary>
[DefaultExecutionOrder(-100)]
public sealed class CanvasResolutionFixer : MonoBehaviour
{
    [SerializeField] private float _referenceWidth  = 1080f;
    [SerializeField] private float _referenceHeight = 1920f;

    private CanvasScaler _scaler;
    private int _lastW;
    private int _lastH;

    private void Awake()
    {
        _scaler = GetComponent<CanvasScaler>() ?? GetComponentInParent<CanvasScaler>();
        Apply();
    }

    private void Update()
    {
        if (Screen.width == _lastW && Screen.height == _lastH) return;
        Apply();
    }

    private void Apply()
    {
        _lastW = Screen.width;
        _lastH = Screen.height;
        if (_scaler == null) return;

        float screenRatio = (float)Screen.width / Screen.height;
        float refRatio    = _referenceWidth / _referenceHeight;

        if (screenRatio > refRatio)
        {
            // Landscape (PC): yüksekliği sabitle, genişliği ekrana göre büyüt
            _scaler.referenceResolution = new Vector2(_referenceHeight * screenRatio, _referenceHeight);
        }
        else
        {
            // Portrait (mobil): referansı aynen koru
            _scaler.referenceResolution = new Vector2(_referenceWidth, _referenceHeight);
        }
    }
}
