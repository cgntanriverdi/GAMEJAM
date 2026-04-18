using UnityEngine;

/// <summary>
/// Swipe parametrelerini Inspector'dan düzenlemeye açar.
/// Project penceresinde sağ tık → Create → ChromePath → SwipeSettings.
/// Fiziksel telefon testinde bu değerler ayarlanmalı (bkz. Plan §10.1).
/// </summary>
[CreateAssetMenu(menuName = "ChromePath/SwipeSettings", fileName = "SwipeSettings")]
public class SwipeSettings : ScriptableObject
{
    [Tooltip("Swipe geçerli sayılmak için gereken minimum piksel mesafesi. (30–50px arası tavsiye)")]
    [Min(1f)]
    public float MinSwipeDistance = 30f;

    [Tooltip("Swipe tamamlanması için izin verilen maksimum süre (saniye). (0.4–0.5s tavsiye)")]
    [Range(0.1f, 1f)]
    public float MaxSwipeTime = 0.5f;
}

// Kullanacak scriptler: SwipeInputController
