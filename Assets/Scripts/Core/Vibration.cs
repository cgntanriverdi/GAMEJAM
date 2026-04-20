using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Platform-bağımsız titreşim yöneticisi.
///
/// Native Android/iOS : Handheld.Vibrate()
/// WebGL (Android)    : navigator.vibrate() — JS köprüsü üzerinden
/// WebGL (iOS Safari) : Vibration API desteklenmiyor; sessizce atlanır
/// PC / Editor        : Hiçbir şey yapmaz
///
/// Kullanım: Vibration.Vibrate() veya Vibration.Vibrate(durationMs: 80)
/// </summary>
public static class Vibration
{
    // ── WebGL JS imports ──────────────────────────────────────────────────────

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void JS_Vibrate(int durationMs);
    [DllImport("__Internal")] private static extern int  JS_IsMobileBrowser();
#endif

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Varsayılan kısa titreşim (50 ms).
    /// </summary>
    public static void Vibrate() => Vibrate(50);

    /// <summary>
    /// Belirtilen süre (ms) kadar titreşim.
    /// PC / Editor'da çağrılırsa sessizce yok sayılır.
    /// </summary>
    public static void Vibrate(int durationMs)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (JS_IsMobileBrowser() == 1)
            JS_Vibrate(durationMs);

#elif UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();

#endif
    }

    /// <summary>
    /// Hafif dokunuş hissi (UI tıklama vb.) — 30 ms.
    /// </summary>
    public static void Light()  => Vibrate(30);

    /// <summary>
    /// Orta şiddette titreşim (hata bildirimi vb.) — 60 ms.
    /// </summary>
    public static void Medium() => Vibrate(60);

    /// <summary>
    /// Güçlü titreşim (level complete vb.) — 100 ms.
    /// </summary>
    public static void Heavy()  => Vibrate(100);
}

/*
 * Sahneye eklenmesi gerekmez — static sınıf, instance yok.
 *
 * Örnek kullanımlar:
 *   Vibration.Vibrate();          // varsayılan 50 ms
 *   Vibration.Vibrate(80);        // özel süre
 *   Vibration.Light();            // hafif dokunuş
 *   Vibration.Medium();           // orta şiddet
 *   Vibration.Heavy();            // güçlü (level win vb.)
 */
