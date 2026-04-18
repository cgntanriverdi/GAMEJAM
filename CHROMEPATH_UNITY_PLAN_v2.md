# ChromePath Unity Gelisim Plani ve APPS GameJam 2026 Kural Eslesmesi
# ⚠️ GÜNCELLEME: Birincil build Android APK, input sistemi Swipe olarak değiştirildi.

## 1. Amac

Bu dokumanin amaci iki seyi tek yerde toplamak:

1. `README.md` icindeki ChromePath fikrini Unity ile 48 saat icinde oynanabilir bir prototipe cevirmek icin teknik ve operasyonel bir yol haritasi cikarmak.
2. `APPS_GameJam_2026_Rules.pdf` icindeki kurallarin hangisinin `README.md` icinde hangi spesifik bolum ve satirlarda karsilandigini gostermek; eksik kalan maddeler icin de net aksiyon tanimlamak.

Bu plan, yarismaya teslim edilebilir bir MVP uretmeye odaklidir. Ticari kalite hedeflenmez; calisan, anlasilir, mobile-friendly ve juriye rahat gosterilebilir bir prototip hedeflenir.

---

## 2. Kaynaklar

- Proje tanimi: `README.md`
- Yarisma kurallari: `APPS_GameJam_2026_Rules.pdf`

---

## 3. Ana Karar: Engine ve Teslim Stratejisi

Engine: **Unity**

### ⚠️ Teslim stratejisi güncellendi

| Oncelik | Format | Neden |
|---|---|---|
| **Birincil** | **Android APK** | Oyun tamamen swipe input uzerine kuruluyor; native mobil deneyim zorunlu |
| Yedek | WebGL / Desktop build | Juri sunum laptopunda acilma garantisi icin |

Bu karar kural 14.4 ile uyumludur:
- Android cihazlarda calistirilab ilir `.apk` formati kabul ediliyor.

**Dikkat:** APK build Unity'de WebGL'den daha uzun surebilir. Build pipeline'i en gec saat 38-40'ta acilmali, son saate birakilmamali.

Unity Player Settings'te ilk gun yapilmasi gerekenler:
- Platform: Android
- Orientation: Portrait locked
- Minimum API Level: Android 8.0 (API 26) veya ustu
- IL2CPP veya Mono: Jam icin Mono daha hizli build verir
- Target Architecture: ARM64 + ARMv7

---

## 4. Oyunun Kisa Urun Tanimi

ChromePath'in cekirdek oyunu:

- Oyuncu gorunen renkli bir grid ustunde ilerler.
- Oyuncuya gizli dogru path verilmez; yalnizca bu path uzerindeki renk frekanslari verilir.
- **Oyuncu her adimi swipe ile verir: yukarı, asagi, sol, sag.**
- Fazla kullanilan renkler anlik hata sinyali verir.
- Undo, checkpoint ve hint sistemleri oyuncuya destek olur.

---

## 5. Juri Acisindan Kazanmasi Gereken Sey

Kural 15.1'e gore juri su basliklara bakacak:

- Tema ve konsepte uyum
- Mobile-friendly gorunum
- Yaraticilik ve ozgunluk
- Oynanabilirlik
- Gorsel ve isitsel tasarim
- Teknik kalite
- Sunum ve butunluk

ChromePath icin kritik nokta: mekanik ilk 15 saniyede anlasilmiyorsa juri oyunun derinligini goremez. Swipe input bu konuda avantajlidir — sezgisel, ogretici metin gerektirmez.

---

## 6. README Referans Indeksi

| Kod | README bolumu |
|---|---|
| R1 | Oyun Ozeti / Core Loop |
| R2 | Tema ve Konsept Uyumu |
| R3 | Grid Yapisi |
| R4 | Path Generation |
| R5 | Oyuncu Path Cizimi |
| R6 | Possible Cell Highlight |
| R7 | Renk Sayaci |
| R8 | Undo Sistemi |
| R9 | Level Tamamlama |
| R10 | Checkpoint Sistemi |
| R11 | Hint Sistemi |
| R12 | Level ve Zorluk Skalasi |
| R13 | UI/UX ve Mobile-First |
| R14 | Teknik Notlar / Algoritmalar |
| R15 | Onerilen Dosya Yapisi |
| R16 | Gelistirme Oncelik Sirasi |
| R17 | Juriye Sunum Notlari |
| R18 | Riskler ve Yedek Planlar |

---

## 7. Unity Teknik Uygulama Plani

### 7.1 Proje kapsamini sabitleme

- Engine: Unity
- Oyun tipi: 2D puzzle
- Orientation: Portrait locked
- Birincil build: Android APK
- Yedek build: WebGL veya Desktop
- MVP hedefi: 10 oynanabilir level

### 7.2 Unity proje yapisi

```text
Assets/
  Art/
    Sprites/
    UI/
    VFX/
  Audio/
    SFX/
    Music/
  Data/
    Levels/
    Balancing/
  Prefabs/
    Grid/
    UI/
    Gameplay/
  Scenes/
    Bootstrap.unity
    MainMenu.unity
    Game.unity
    LevelComplete.unity
  Scripts/
    Core/
    Data/
    Gameplay/
    Grid/
    UI/
    Progression/
    Utils/
  Settings/
```

### 7.3 Sahne tasarimi

**Bootstrap:** global init, save yukle, sahneye gec.

**MainMenu:** Play, Continue, Hint sayisi, Nasil oynanir.

**Game:** Header (level no, hint, undo) + Grid + Counter panel + Overlay'lar.

**LevelComplete:** Rozet, hint kazanimi, sonraki level.

### 7.4 Veri modeli

```csharp
public enum CellColor { Red, Blue, Green, Yellow }

public struct GridCoord { public int X; public int Y; }

public enum SwipeDirection { Up, Down, Left, Right }

public sealed class CellData
{
    public GridCoord Coord;
    public CellColor Color;
    public bool IsStart;
    public bool IsEnd;
}

public sealed class LevelDefinition
{
    public int LevelIndex;
    public int Width;
    public int Height;
    public int MinPathLength;
    public int MaxPathLength;
    public int ActiveColorCount;
    public bool AllowGeneratedLevel;
}

public sealed class PathSolution
{
    public List<GridCoord> Cells;
    public Dictionary<CellColor, int> TargetColorCounts;
}

public sealed class PlayerRunState
{
    public List<GridCoord> SelectedPath;
    public Dictionary<CellColor, int> CurrentColorCounts;
    public int CheckpointLockedLength;
    public bool CheckpointTriggered;
}
```

### 7.5 Script sorumluluklari

| Script | Gorev |
|---|---|
| `GameBootstrap` | Global init, save yukle, sahne gec |
| `GameManager` | Oyun state, level baslat, win/lose/reset, orkestra |
| `LevelManager` | Level progression, config sec, generate/curated karar |
| `GridManager` | Grid instantiate, hucre data, highlight render, komsu highlight |
| `PathGenerator` | Gizli path uret, renk frekanslarini hesapla |
| `SwipeInputController` | ⚠️ Swipe algilama, yon hesaplama, adim tetikleme |
| `RunValidator` | Gecerli adim kontrolu, renk asim kontrolu, win kontrolu |
| `CounterPanelUI` | Hedef ve mevcut sayilari goster, renk durumu guncelle |
| `CheckpointManager` | %50 esik, undo lock, event tetikleme |
| `HintManager` | Hint hakki, yari path reveal, fade out |
| `ProgressionService` | Level kaydi, hint kaydi, save/load |
| `AudioManager` | SFX yonetimi |

---

## 7.6 ⚠️ Swipe Input Sistemi — Tam Spesifikasyon

Bu bolum orijinal dokumanda yoktu. Swipe input kararinin tum etkileri burada tanimlanmistir.

### 7.6.1 Swipe nasil calisir

Oyuncu ekranda herhangi bir yere parmakla kaydirma (swipe) yapar.

- Swipe yonu hesaplanir: yukari / asagi / sol / sag
- Oyuncunun bulundugu hucrenin o yondeki komsusuna gecilmeye calisilir
- Gecerli komsu ise adim atilir
- Gecersiz ise (duvar, daha once secilmis, grid disi) kisa hata sinyali verilir, hareket edilmez

Swipe icin parmak **grid uzerinde olmak zorunda degildir**. Ekranin herhangi bir yerinden swipe edilebilir. Bu, kucuk grid'lerde parmak altinda kalan hucreleri gormek icin kritik bir UX kararidir.

### 7.6.2 SwipeInputController implementasyon detayi

```csharp
public class SwipeInputController : MonoBehaviour
{
    [SerializeField] private float minSwipeDistance = 30f; // pixel
    [SerializeField] private float maxSwipeTime = 0.5f;    // saniye

    private Vector2 touchStartPos;
    private float touchStartTime;

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                touchStartPos = touch.position;
                touchStartTime = Time.time;
            }

            if (touch.phase == TouchPhase.Ended)
            {
                float duration = Time.time - touchStartTime;
                Vector2 delta = touch.position - touchStartPos;

                if (duration <= maxSwipeTime && delta.magnitude >= minSwipeDistance)
                {
                    SwipeDirection dir = GetDirection(delta);
                    OnSwipe(dir);
                }
            }
        }
    }

    private SwipeDirection GetDirection(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
        else
            return delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
    }

    private void OnSwipe(SwipeDirection dir)
    {
        GridCoord next = GetNeighbor(currentPlayerPos, dir);
        GameManager.Instance.TryMovePlayer(next);
    }
}
```

**Parametre tuning notu:**
- `minSwipeDistance`: Cok dusukse yanlis swipe, cok yuksekse kasitli swipe algılanmaz. 30–50px arasi test edilmeli.
- `maxSwipeTime`: Hizli kaydirmalar icin 0.4–0.5s yeterli.

### 7.6.3 Possible Cell Highlight — Swipe'a uyarlanmis hali

Orijinal planda "komsu hucreleri highlight et" vardi. Swipe ile bu degisiyor:

**Eski davranis:** Dokunulabilir komsu hucreler parliyordu.

**Yeni davranis:** Oyuncunun bulundugu hucrenin 4 kenarinda **yonsel ok ikonlari** gosterilir. Gidilebilir yon icin ok parlak, duvar veya daha once gecilmis yon icin ok soluk/yok.

```
         ▲ (parlak = gidilebilir)
    ◀    [X]    ▶ (soluk = daha once gecildi)
         ▼ (yok = grid siniri)
```

Bu hem oyuncuyu yonlendirir hem de swipe mekanigiyle uyumlu gorsel dil olusturur. Arrow/Vector konseptiyle de guclu gorsel bag kurar.

### 7.6.4 Undo swipe ile mi yoksa butonla mi?

Undo butonu ekranda kalir (sag ust kose). Swipe ile undo eklenmez cunku:
- "Geri swipe" hareket yonu ile karisir
- Accidental undo riski artar

Undo yalnizca butonla tetiklenir.

### 7.6.5 Hint ve Checkpoint swipe etkilenmez

Hint ve Checkpoint sistemleri swipe'tan bagimsizdir. Hint butonu tap ile tetiklenir, animasyon ayni sekilde calisir.

---

### 7.7 Oynanis akis diyagrami (Swipe ile guncellenmis)

```text
Level load
-> Grid olustur
-> Gizli path uret
-> Hedef renk sayaçlarini hesapla
-> UI'ya yansit
-> Oyuncu start hucresinde baslar
-> Oyuncu swipe yapar (yukari/asagi/sol/sag)
   -> Yon hesaplanir
   -> Komsu hucre gecerli mi?
      -> Evet: adim atilir, sayac guncellenir, highlight guncellenir
      -> Hayir: hata sinyali, hareket yok
   -> Checkpoint esigi kontrol et
-> Oyuncu end hucresine gelince:
   -> Tum sayaclar hedefle esit mi?
      -> Evet: Level complete
      -> Hayir: devam / geri duzelt
```

### 7.8 Path generation algoritması

1. Grid olustur
2. Start ve end belirle
3. Self-avoiding random walk ile path arat (4 yon)
4. Min uzunluk saglanmiyorsa yeniden dene
5. Max iteration asılırsa curated fallback yukle

### 7.9 Dogrulama mantigi

- `SwipeInputController` sadece yon niyetini iletir
- `RunValidator` gecerliligi belirler
- `GameManager` sonucu uygular

Her adimda kontroller:
1. Hucre komsu mu? (swipe yonu + 1 adim)
2. Hucre daha once secildi mi?
3. Checkpoint kilidinin gerisine undo yapiliyor mu?
4. Yeni secimle renk sayaclari ne oluyor?
5. End cell'e gelindiyse tum hedefler tamam mi?

### 7.10 Mobile-friendly Unity ayarlari

- `Canvas Scaler` → `Scale With Screen Size`, portrait referans cozunurluk
- Grid hucreleri minimum **60x60 Unity unit** (dokunma degil, gorunum icin; swipe ekran genelinde alindigindan hitbox sorunu yok)
- Yonsel ok ikonlari oyuncunun bulundugu hucrenin etrafinda net gorunmeli
- Tek elle swipe desteklenmeli
- Safe area (notch) dikkate alinmali

### 7.11 UI yerlesimi

```
┌─────────────────────────────────┐
│  Level 5      [HINT 🔮]  [↩️]   │  ← Header
├─────────────────────────────────┤
│                                 │
│    ▲                            │
│ ◀ [OYUNCU] ▶   ... grid ...    │  ← Yonsel oklar oyuncunun yaninda
│    ▼                            │
│                                 │
├─────────────────────────────────┤
│  🔴 3/4  🔵 2/2✅  🟢 1/3  🟡 0/1 │  ← Renk Sayaçları
└─────────────────────────────────┘
```

Tasarim ilkeleri:
- Sayaçlar buyuk ve tek bakista okunur
- Asim durumunda kirmizi + shake
- Yonsel oklar cok guclu olmayacak, board'u kirletmemeli
- Start/end hucreleri ikon ile net isaretlenmeli

### 7.12 Save sistemi

`PlayerPrefs` yeterli:
- Mevcut acik level
- Toplam tamamlanan level
- Mevcut hint hakki
- Ses on/off

### 7.13 Ses ve geri bildirim

Minimum SFX:
- swipe (gecerli adim)
- invalid swipe (gecersiz yon)
- undo
- checkpoint
- level complete
- hint reveal

### 7.14 Debug overlay

Inspector ve debug mode'da gosterilebilecekler:
- Gizli path toggle
- Hedef renk sayaclari
- Checkpoint threshold
- Mevcut selected path uzunlugu
- Procedural seed
- Swipe delta ve yon log'u ← **yeni eklendi, swipe tuning icin kritik**

---

## 8. Kodlama Sirasi: 48 Saatlik Uygulama Takvimi

### Saat 0-4: Proje kurulum

- Unity proje olustur
- **Android build target ayarla (ilk gunun ilk isi)**
- Portrait locked
- Git repo + `.gitignore`
- Scene iskeletleri
- Klasor yapisi
- Dummy grid prefab

Teslim: Acilan, dogru platform ayarli proje.

### Saat 4-10: Grid ve temel swipe input

- `GridManager`: grid instantiate, renk atama
- `CellView`: gorsel state'ler
- Komsuluk hesaplari
- **`SwipeInputController`: swipe algilama ve yon hesaplama**
- **Swipe → komsu hucreye gecis**
- Start hucresine oyuncu yerlesimi

Teslim: Oyuncu swipe ile grid uzerinde hareket edebiliyor.

**Not:** Bu fazda swipe parametrelerini (minSwipeDistance, maxSwipeTime) fiziksel telefonda test edin. Emulator swipe davranisi gercekten farklidir.

### Saat 10-16: Path generator ve hedef sayac

- `PathGenerator`: gizli path uretimi
- Renk frekansi cikarimi
- Alt panel `CounterPanelUI`
- **Yonsel ok highlight** (mevcut hucrenin etrafinda gidilebilir yonler)

Teslim: Gizli solution uretiliyor, oyuncuya hedef renkler ve yon gostergesi gosteriliyor.

### Saat 16-22: Validasyon ve kazanma kosulu

- `RunValidator`: renk asim kontrolu, end cell + success kontrolu
- Gecersiz swipe hata sinyali (titresim, renk flash)
- Gecersiz ama end'e ulasilan durum feedback'i

Teslim: **Ana puzzle loop calisiyor. Bu gercek MVP noktasi.**

### Saat 22-28: Undo ve gorseller

- Undo butonu → adim geri al → sayac rollback
- Yonsel ok highlight guncelleme
- Checkpoint locked segment → undo alt limiti
- Temel animasyonlar (cell select, invalid swipe shake)

Teslim: Oyun okunur ve oynanabilir hale gelir.

### Saat 28-34: Checkpoint ve hint

- `CheckpointManager`: %50 esik, toast animasyonu
- `HintManager`: hint hakki ekonomisi, yari path reveal (2sn)
- Fade out

Teslim: Destekleyici sistemler oyuna eklendi.

### Saat 34-38: Level progression ve balancing

- 10 level config (curated veya procedural)
- Ilk 3 level onboarding (kisa path, 2 renk)
- Level complete ekrani
- Hint hakki kaydi

Teslim: Juriye gosterilecek dikey dilim hazir.

### Saat 38-40: ⚠️ APK Build — Erkenden Al

**Bu blok mutlaka saat 38-40'ta acilmali.**

- Android build al
- Fiziksel telefonda test et
- Swipe parametrelerini gercek cihazda tune et
- WebGL yedek build al
- Bozuk sistemi devre disi birak, calisan sistemle build ver

APK build ilk seferde genellikle sorun cikarir (SDK, signing, gradle). Bunu son saate birakmak teslimi riske atar.

### Saat 40-44: UI polish, ses, onboarding

- Toast ve win panel
- Kisa "Nasil oynanir?" overlay (swipe gesture animasyonu goster)
- Temel SFX
- Counter ve buton polish

### Saat 44-48: Smoke test, paketleme, sunum

- APK final test (telefonda baslardan sona oynayin)
- Upload paketi hazirla
- Sunum akisini prova et
- Juri demo rotasini belirle

---

## 9. Kapsam Kirpma Kurallari

Yetismezse bu sira ile kirpin:

1. Gelismis animasyonlar
2. Ses cesitliligi
3. Ileri polish
4. Procedural level sayisi
5. Hint sistemi

Asla kirpmayin:

1. **Calisir APK build**
2. **Swipe input**
3. Mobile-friendly grid
4. Tema ve Arrow/Vector baglantisi
5. Core puzzle loop
6. Juriye anlasilir sunum

---

## 10. Teknik Riskler ve Cozumler

### 10.1 Risk: Swipe yanlis algilaniyor (cok hassas veya kör)

Cozum:
- `minSwipeDistance` ve `maxSwipeTime` fiziksel telefonda erken test et (saat 4-10 blogu)
- Debug log ile swipe delta degerlerini izle
- Tolerans degerlerini ScriptableObject'e al, Inspector'dan degistir

### 10.2 Risk: APK build bozuk / signing sorunu

Cozum:
- Build en gec saat 38-40'ta alinmali, son saate birakilmamali
- Development build ile baslayip release build deneyin
- Gradle veya SDK sorunu cikinca WebGL yedege gec

### 10.3 Risk: Procedural generator sacmaliyor

Cozum:
- Max attempt limiti + seed log
- Curated level fallback

### 10.4 Risk: Puzzle cok belirsiz

Cozum:
- Ilk levellerde 2 renk, kisa path
- Yonsel ok highlight guclu tutulsun
- Sayac okunabilirligi optimize edilsin

### 10.5 Risk: Hint/checkpoint bug

Cozum:
- Bug'liysa disable et, bozuk sistem gostermekten iyidir

### 10.6 Risk: Emulator'da swipe calisiyor, gercek cihazda calısmiyor

Cozum:
- Fiziksel telefon ilk gunun sonunda mutlaka test edilmeli
- `Input.touchCount` yerine Unity'nin yeni `InputSystem` paketi kullanilabilir (daha tutarli)

---

## 11. Teslim Paketi Checklist

Kural 14.3-14.7 icin:

### Zorunlu
- [ ] Calisir APK (fiziksel telefonda test edilmis)
- [ ] WebGL yedek build
- [ ] Oyun aciklamasi (Turkce)
- [ ] Kontroller: "Swipe ile hareket et, undo icin sag ust butounu kullan"
- [ ] Ekip uyeleri ve roller

### Tavsiye edilen
- [ ] AI kullanim notu
- [ ] Asset/lisans listesi
- [ ] Bilinen kisitlar notu

### Paket ornegi
```text
ChromePath_Submission/
  ChromePath.apk
  WebGL_Build/           ← yedek
  README_Submission.md
  Credits_And_Licenses.md
```

---

## 12. Sunum Plani

Kural 16'ya gore fiziksel sunum zorunlu.

1. Tek cumle pitch: "Renk sayisi ipuclarini kullanarak swipe ile gizli pathin uzerinden gec"
2. Tema/Arrow baglantisi: her swipe bir yon kararidir
3. Juri bir level oynayabilir — telefonu verir
4. Checkpoint veya hint'i goster
5. Mobile-friendly'i vurgula
6. AI kullanildiysa belirt

**Sunum icin tavsiye:** Jurinin telefonu eline alip oynamasi en guclu demo. Swipe input bunu kolaylastirir.

---

## 13. Kural Eslestirme Matrisi

*(Orijinal matrisin degisen maddeleri asagida isaretlenmistir.)*

### 13.1 Tema, konsept, oyun kapsamı

| Kural | Ozet | Durum | Aksiyon |
|---|---|---|---|
| 2.3 | Tema Puzzle | Tam | Degisiklik yok |
| 8.1 | Dijital oyun | Tam | Unity ile uretilir |
| 8.3 | Mobile-friendly zorunlu | **Guclu Tam** | APK + swipe = native mobil |
| 8.6 | Oynanabilir prototip | Tam | Core loop + APK build |
| 9.2 | En az bir konsept | Tam | Arrow/Vector: swipe = yon karari |
| 9.4 | Konseptle gorunur bag | **Guclu Tam** | Swipe mekanigi + yonsel ok = Arrow konsepti cok net |
| 15.1-b | Mobile-friendly gorunum | **Guclu Tam** | APK ve swipe bu kriteri en guclu sekilde karsilar |
| 15.1-d | Oynanabilirlik | Tam | Core loop + swipe akici |

### 13.2 Build ve teslim

| Kural | Ozet | Durum | Aksiyon |
|---|---|---|---|
| 14.4 | Oynanabilir format | **Guncellendi** | **Birincil: APK. Yedek: WebGL** |
| 14.5 | Sadece kaynak dosya yetmez | README disi | Calisan APK uretilmeden teslim yapilmaz |
| 14.7 | Teslim saatine kadar | Kismi | **Build freeze saat 38-40 — geciktirme** |
| 16.3 | Sunumda calisir olmali | Tam | Telefonda son smoke test zorunlu |

### 13.3 Diger maddeler

Telif, AI, Code of Conduct, operasyonel kurallar orijinal dokumandaki gibi gecerlidir. Degisiklik yoktur.

---

## 14. README'de Eksik Kalan Noktalar

### 14.1 ⚠️ Input sistemi README'de guncellenmeli

README hala "cell'e tıklama/dokunma" diyor. Swipe karari kesinlestikten sonra README'deki su bolumlerin guncellenmesi gerekir:

- Bolum 3.3 "Oyuncu Path Cizimi"
- Bolum 3.4 "Possible Cell Highlight" (ok ikonlarına donusecek)
- Bolum 7.1 UI Layout (swipe gesture onboarding eklenmeli)
- Bolum 7.7 Animasyonlar tablosu (swipe feedback eklenmeli)

### 14.2 Build format README'de netlesmeli

README'deki Bolum 14.4'e "Birincil: Android APK" notu eklenmeli.

### 14.3 Submission dokumani

`README_Submission.md` ayrica hazirlanmali:
- Swipe kontrollerini acikca belirt
- "Ekrani herhangi bir yerine swipe yapin" notu kritik

### 14.4 AI seffaflik notu

Kullanilan AI araclari liste halinde tutulmali.

### 14.5 Demo onboarding

Ilk acilista kisa bir swipe gesture animasyonu goster (el ikonu + swipe hareketi). Oyretici metin olmadan mekaniği ogretir.

---

## 15. Ilk Kod Sprintinde Acilacak Gorevler

### Programmer
- Grid instantiate
- **SwipeInputController (on planda)**
- Path generator
- Validator
- **APK build pipeline (gun 1 kurulmali)**

### Designer / UI
- Hucre renk dili
- Sayac paneli
- **Yonsel ok ikonlari (swipe feedback)**
- Feedback animasyonlari
- **Swipe onboarding animasyonu**

### Game Designer
- Ilk 10 level progression
- Onboarding tuning
- Juri demo rotasi

### QA
- **Fiziksel telefonda swipe test (erken)**
- Sayac okunabilirligi
- Build smoke test

---

## 16. Sonuc

ChromePath, APPS GameJam 2026'nin beklentilerine uygundur. Swipe input karari:

- Mobile-friendly kriteri en guclu sekilde karsiliyor
- Arrow/Vector konseptiyle gorsel ve mekanik uyumu artiriyor
- Juri sunumunda telefonu eline alip deneyimletme imkani veriyor

**En dogru strateji:**
1. Swipe input + core puzzle loop (saat 0-22)
2. Undo + highlight + checkpoint/hint (saat 22-34)
3. Level progression (saat 34-38)
4. **APK build + fiziksel test (saat 38-40) — bu asla ertelenmemeli**
5. Polish + sunum (saat 40-48)
