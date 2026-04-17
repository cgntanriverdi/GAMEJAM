# ChromePath Unity Gelisim Plani ve APPS GameJam 2026 Kural Eslesmesi

## 1. Amac

Bu dokumanin amaci iki seyi tek yerde toplamak:

1. `README.md` icindeki ChromePath fikrini Unity ile 48 saat icinde oynanabilir bir prototipe cevirmek icin teknik ve operasyonel bir yol haritasi cikarmak.
2. `APPS_GameJam_2026_Rules.pdf` icindeki kurallarin hangisinin `README.md` icinde hangi spesifik bolum ve satirlarda karsilandigini gostermek; eksik kalan maddeler icin de net aksiyon tanimlamak.

Bu plan, yarismaya teslim edilebilir bir MVP uretmeye odaklidir. Ticari kalite hedeflenmez; calisan, anlasilir, mobile-friendly ve juriye rahat gosterilebilir bir prototip hedeflenir.

---

## 2. Kaynaklar

Bu dokuman su iki kaynaga dayanir:

- Proje tanimi: `README.md`
- Yarisma kurallari: `/Users/cagan/Downloads/APPS_GameJam_2026_Rules.pdf`

README referanslari satir bazinda verilmistir. Boylece hangi kuralin README'de tam olarak nereye oturdugu acikca gorulebilir.

---

## 3. Ana Karar: Engine ve Teslim Stratejisi

`README.md` satir 4'te engine secimi henuz acik birakilmis:

- `README.md:4` -> `Engine: [Unity / Godot / Phaser — ekip kararina gore doldur]`

Bu plan, projeyi **Unity** ile uygulamayi baz alir. Bunun nedeni:

- Grid tabanli 2D puzzle yapisi Unity'de cok hizli ayağa kalkar.
- Touch input, UI, animasyon ve WebGL export zinciri game jam kosullarinda pratiktir.
- Juri icin oynanabilir build alma sansi yuksektir.
- README'deki onerilen sinif yapisi zaten Unity/C# mantigiyla yazilmistir (`GridManager.cs`, `PathGenerator.cs`, `GameManager.cs` vb.).

### Onerilen teslim stratejisi

Birincil teslim cikti hedefi:

- **WebGL build**

Ikincil yedek teslim:

- **Android APK** veya **masaustu build**

Bunun nedeni kural 14.4 ve 14.5'tir:

- Oynanabilir cikti zorunlu
- Yalnizca Unity proje klasoru vermek yeterli degil

Dolayisiyla teknik planin son blogunda mutlaka build, smoke test ve paketleme zamani ayrilmalidir.

---

## 4. Oyunun Kisa Urun Tanimi

README'ye gore ChromePath'in cekirdek oyunu su:

- Oyuncu gorunen renkli bir grid ustunde ilerler.
- Oyuncuya gizli dogru path verilmez; yalnizca bu path uzerindeki renk frekanslari verilir.
- Oyuncu start'tan end'e giderken dogru rotayi renk sayaçlari ile cikarir.
- Fazla kullanilan renkler anlik hata sinyali verir.
- Undo, checkpoint ve hint sistemleri oyuncuya destek olur.

README referanslari:

- Oyun ozeti: `README.md:8-16`
- Tema ve Arrow/Vector baglantisi: `README.md:20-23`
- Temel mekanikler: `README.md:27-81`
- Checkpoint: `README.md:85-94`
- Hint: `README.md:98-115`
- Zorluk skalasi: `README.md:119-130`
- Mobile-first UI: `README.md:134-168`
- Teknik algoritma notlari: `README.md:172-210`

Bu tanim, yarisma temasina dogrudan uyuyor:

- Tema: Puzzle
- Konsept: Arrow / Vector

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

Bu nedenle Unity uygulamasi su sira ile optimize edilmelidir:

1. Mekanik netligi
2. Sorunsuz oynanabilirlik
3. Mobile-friendly UI
4. Juriye hizli anlatilabilirlik
5. Build alinabilirligi
6. Gorsel polish

ChromePath icin kritik nokta su: mekanik ilk 15 saniyede anlasilmiyorsa juri oyunun derinligini goremez. Bu nedenle onboarding, sayaç okunabilirligi ve cell highlight sistemi polish'ten once gelir.

---

## 6. README Referans Indeksi

Asagidaki kodlar, kural eslestirme tablosunda kisalik icin kullanilacaktir.

| Kod | README bolumu | Satir |
|---|---|---|
| R1 | Oyun Ozeti / Core Loop | 8-16 |
| R2 | Tema ve Konsept Uyumu | 20-23 |
| R3 | Grid Yapisi | 29-33 |
| R4 | Path Generation | 35-42 |
| R5 | Oyuncu Path Cizimi | 44-50 |
| R6 | Possible Cell Highlight | 52-56 |
| R7 | Renk Sayaci | 58-69 |
| R8 | Undo Sistemi | 71-76 |
| R9 | Level Tamamlama | 78-81 |
| R10 | Checkpoint Sistemi | 85-94 |
| R11 | Hint Sistemi | 98-115 |
| R12 | Level ve Zorluk Skalasi | 119-130 |
| R13 | UI/UX ve Mobile-First | 134-168 |
| R14 | Teknik Notlar / Algoritmalar | 172-210 |
| R15 | Onerilen Dosya Yapisi | 214-241 |
| R16 | Gelistirme Oncelik Sirasi | 245-266 |
| R17 | Juriye Sunum Notlari | 270-276 |
| R18 | Riskler ve Yedek Planlar | 280-287 |

---

## 7. Unity Teknik Uygulama Plani

## 7.1 Proje kapsamini dogru sabitleme

Ilk is olarak proje karari su sekilde kilitlenmeli:

- Engine: Unity
- Oyun tipi: 2D puzzle
- Hedef oran: portrait once
- Ana hedef build: WebGL
- Yedek build: Android veya masaustu
- MVP hedefi: 10 oynanabilir level

Neden 10 level?

- README'deki zorluk skalasi `1-10` araliginda anlamli bir progression tanimliyor.
- Juriye hem onboarding hem de ilerleyen derinligi gostermek icin yeterli.
- Procedural sistem yetismezse dahi 10 level elle curate edilebilir.

## 7.2 Unity proje yapisi

README'deki `R15` bolumu dogru yon veriyor ama biraz daha uygulanabilir hale getirilmeli.

Onerilen Unity klasor yapisi:

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

`Bootstrap.unity` eklenmesi faydali olur. Tek gorevi:

- temel servisleri ayaga kaldirmak
- save verisini yuklemek
- dogru sahneye gecmek

Jam kosullarinda sahneler arasi bug azaltir.

## 7.3 Sahne tasarimi

### Bootstrap

Gorevler:

- Save yukle
- Session init et
- Frame rate ve orientation ayarla
- MainMenu'ye gec

### MainMenu

Gorevler:

- Play
- Continue
- Hint sayisi gostergesi
- Belki kisa bir "Nasil oynanir?" butonu

### Game

Tek ana oynanis sahnesi. Tum cekirdek sistem burada olmali.

Icerik:

- Header
- Level bilgisi
- Hint butonu
- Undo butonu
- Grid
- Counter panel
- Checkpoint toast
- Win/fail overlay

### LevelComplete

Icerik:

- Level tamamlandi bilgisi
- Kullanilan hint
- Checkpoint rozeti
- Sonraki level butonu
- Ana menu butonu

Jam kisitinda istenirse bu sahne ayri olmayip `Game` icinde overlay olarak da cozulur.

## 7.4 Onerilen veri modeli

En cok hiz kazandiracak seylerden biri veriyi bastan duzgun ayirmaktir.

```csharp
public enum CellColor
{
    Red,
    Blue,
    Green,
    Yellow
}

public struct GridCoord
{
    public int X;
    public int Y;
}

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

Bu ayirim neden onemli?

- Grid data ile UI bagimliligi azalir.
- Test yazmak kolaylasir.
- Procedural ve hard-coded level ayni runtime uzerinden akabilir.
- Jamin son saatlerinde "procedural yetismedi, elle level girelim" karari daha ucuz olur.

## 7.5 Script sorumluluklari

README'deki script listesi iyi bir baslangic. Aşağıdaki dagilim daha saglam olur:

### `GameBootstrap`

- global init
- save yukleme
- sahne gecisi

### `GameManager`

- mevcut oyun state'i
- level baslatma
- kazanma / kaybetme / reset
- alt sistemler arasi orkestra

### `LevelManager`

- level progression
- level definition secimi
- generate mi, curated mi karar verme

### `GridManager`

- grid instantiate etme
- hucre data baglama
- path highlight render etme
- komsu hucreleri aktif/pasif gosterme

### `PathGenerator`

- gizli cozum pathini olusturma
- gerekli renk frekanslarini hesaplama
- gerekirse tekrar deneme

### `PlayerInputController`

- touch / mouse input
- secilebilir komsu kontrolu
- cell secimi, undo tetigi

### `RunValidator`

- secilen adimin kurallara uyup uymadigini dogrulama
- renk asimini hesaplama
- end'e geldiginde tum sayaçlar tutuyor mu bakma

### `CounterPanelUI`

- hedef ve mevcut sayilari gosterme
- renk durumlarini guncelleme

### `CheckpointManager`

- yarim yol esigini hesaplama
- kilitli segmenti belirleme
- undo alt limitini dayatma

### `HintManager`

- hint hakkini kontrol etme
- cozumun ilk yarisini gostermek
- fade out yonetmek

### `ProgressionService`

- tamamlanan level
- kazanilan hint
- save/load

### `AudioManager`

- tiklama
- warning
- checkpoint
- level complete

## 7.6 ScriptableObject kullanimi

Game jam'de asiri abstraction zararlidir; ama iki yerde ScriptableObject cok faydali olur:

1. `LevelDefinitionAsset`
2. `ColorThemeAsset`

Boylece:

- level tuning Inspector'dan hizli olur
- sahneye kod degistirmeden ayar cekilebilir
- yedek/hard-coded level gecisi hizlanir

## 7.7 Oynanis akis diyagrami

```text
Level load
-> Grid olustur
-> Gizli path uret / yukle
-> Hedef renk sayaçlarini hesapla
-> UI'ya bas
-> Oyuncu start cell'den ilerler
-> Her secimde:
   - komsuluk kontrolu
   - secilen path'e ekle
   - canli sayaç guncelle
   - possible cells guncelle
   - checkpoint esigi kontrol et
-> Oyuncu end'e ulasinca:
   - tum hedef sayilar tuttu mu?
   - evetse level tamamla
   - hayirsa devam / geri duzelt
```

## 7.8 Path generation icin pratik algoritma secimi

README `R14` icinde BFS/DFS/random walk oneriyor. Jam kosullarinda en guvenli yontem:

1. Grid'i olustur
2. Start ve end belirle
3. Self-avoiding random walk ile path arat
4. Belirlenen minimum path uzunlugunu saglamiyorsa yeniden dene
5. Maksimum deneme sayisini asarsa fallback curated level yukle

Neden tam procedural solver yazmiyoruz?

- 48 saatte "tek cozumlu bulmaca garantisi" pahali
- README zaten birden fazla gecerli path olabilecegini kabul ediyor (`R4`)
- Oyunun eglencesi de zaten bu kontrollu belirsizlikten geliyor

### Onerilen generator kosullari

- hareket sadece 4 yon
- ayni hucreye ikinci kez girilmez
- end'e erken varis istenmiyorsa min path uzunlugundan once end yasaklanir
- max iteration limiti olur
- son care olarak curated level fallback vardir

Bu fallback, README `R18` ile de uyumludur:

- `Path gen algoritmasi tikanirsa -> Sabit birkaç level hard-code et`

## 7.9 Dogrulama mantigi

Oyuncu input'i ile oyunun dogru/yanlis durumu birbirine karismamali.

Bu ayrim su sekilde kurulabilir:

- `PlayerInputController` sadece secim niyetini iletir
- `RunValidator` bunun gecerli adim olup olmadigini belirler
- `GameManager` sonucu uygular

### Her tiklamada yapilacak kontrol

1. Hucre komsu mu?
2. Hucre daha once secildi mi?
3. Checkpoint kilidinin gerisine undo yapilmaya calisiliyor mu?
4. Yeni secimden sonra renk sayaçlari ne oluyor?
5. End cell'e gelindiyse tum hedefler tamam mi?

## 7.10 Mobile-friendly tasarimi Unity'de nasil garanti ederiz

Kural 8.3 ve 15.1 icin en kritik alan burasi.

Uygulanacak kararlar:

- `Canvas Scaler` -> `Scale With Screen Size`
- referans cozunurluk portrait bazli
- grid hucre hitbox'i gorselden buyuk olabilir
- cell minimum 60x60 okunacak ve tiklanacak boyutta kalir
- text overflow olmamali
- tek elle kullanima uygun buton yerlesimi
- hint ve undo ekranin ust kisminda ama rahat tiklanabilir olacak

README eslesmesi:

- `R13` satir 151-156 bunu dogrudan destekliyor

## 7.11 UI yerlesimi

Tek sahnede kullanilacak minimum UI:

- ust bar:
  - level no
  - hint butonu
  - undo butonu
- merkez:
  - grid
- alt panel:
  - 4 renk sayaçlari
- overlay:
  - checkpoint toast
  - level complete panel
  - optional pause/how to play

### Tasarim ilkeleri

- renk sayaçlari buyuk ve tek bakista okunur olacak
- asim durumunda sadece renk degisikligi degil, kisa shake de verilecek
- secilebilir komsu hucre glow'u cok guclu olmayacak, yoksa board kirli gorunur
- baslangic ve bitis hucreleri ayirt edilebilir ikon ile isaretlenecek

## 7.12 Save sistemi

Minimum save alanlari:

- current unlocked level
- total cleared levels
- available hints
- audio on/off

Kayit icin `PlayerPrefs` yeterli. Jam icin daha agir bir save sistemi gereksiz.

## 7.13 Ses ve geri bildirim

Ses tasarimi juri etkisi icin kucuk ama onemli bir carpandir.

Minimum SFX listesi:

- cell select
- invalid/overflow warning
- undo
- checkpoint
- level complete
- hint reveal

Muzik yetismezse:

- tek loop ambience yeterli
- hatta tamamen sessiz ama temiz SFX'li bir deneyim, bozuk muzikten daha iyidir

## 7.14 Analitik degil, gozlenebilir debug

Jam icin tam analytics gerekmez. Ama inspector ve debug overlay faydalidir.

Debug modda gosterilebilecekler:

- gizli path toggle
- hedef renk sayaçlari
- checkpoint threshold
- mevcut selected path uzunlugu
- procedural seed

Bu sayede generator sorunlari hizli tespit edilir.

---

## 8. Kodlama Sirasi: 48 Saatlik Uygulama Takvimi

README `R16` zaten bir oncelik sirasi veriyor. Burayi Unity bazinda daha operasyonel hale getirmek gerekir.

## 8.1 Saat 0-4: Proje kurulum ve iskelet

Yapilacaklar:

- Unity proje olustur
- portrait ayari yap
- temel git reposu ve `.gitignore`
- scene iskeletlerini ac
- temel klasor yapisini kur
- 1 adet test grid prefab'i cikar

Teslim ciktisi:

- bos ama acilan proje
- Game scene'de dummy grid

## 8.2 Saat 4-10: Grid ve temel input

Yapilacaklar:

- `GridManager`
- `CellView`
- komsuluk hesaplari
- mouse/touch secimi
- start cell atama

Teslim ciktisi:

- oyuncu hucreleri secip path olusturabiliyor

## 8.3 Saat 10-16: Path generator ve hedef sayac

Yapilacaklar:

- `PathGenerator`
- start/end secimi
- path renk frekansi cikarimi
- alt panel sayaç UI

Teslim ciktisi:

- gizli solution uretiliyor
- oyuncuya hedef renkler gosteriliyor

## 8.4 Saat 16-22: Validasyon ve kazanma kosulu

Yapilacaklar:

- `RunValidator`
- renk asim kontrolu
- end cell ve success kontrolu
- gecersiz ama bitise ulasilan durumlarda feedback

Teslim ciktisi:

- oyunun ana bulmacasi calisiyor

Bu nokta MVP'nin gercek tamamlanma anidir.

## 8.5 Saat 22-28: Undo ve possible cells

Yapilacaklar:

- adim geri alma
- sayaç rollback
- secilebilir komsulari highlight etme

Teslim ciktisi:

- oyun artik okunur ve oynanabilir hale gelir

## 8.6 Saat 28-34: Checkpoint ve hint

Yapilacaklar:

- checkpoint threshold
- undo lock
- hint ekonomisi
- yarim path reveal

Teslim ciktisi:

- README'deki destekleyici sistemler oyuna eklenmis olur

## 8.7 Saat 34-40: Level progression ve balancing

Yapilacaklar:

- 10 level progression
- curated level fallback
- ilk 3 level onboarding
- 4-6 orta zorluk
- 7-10 daha yoğun karar alanlari

Teslim ciktisi:

- juriye gosterilecek akici dikey dilim

## 8.8 Saat 40-44: UI polish, ses, mini onboarding

Yapilacaklar:

- toast ve win panel
- kisa "Nasil oynanir?"
- basic SFX
- counter ve buttons polish

## 8.9 Saat 44-48: Build, smoke test, paketleme, sunum

Yapilacaklar:

- WebGL build al
- ikinci build varsa onu da al
- package icine README/aciklama koy
- kontrolleri ve oyun loop'unu test et
- juri sunum akisini prova et

Bu blok sakaya gelmez. Generator veya polish yetismezse bu bloktan zaman calmayin; onun yerine ozellik kirpin.

---

## 9. Kapsam Kirpma Kurallari

Jam'de en kritik disiplin, ozellik eklemek degil ozellik kesmektir.

Yetismezse su sira ile kirpin:

1. Gelismis animasyonlar
2. Ses cesitliligi
3. Ileri polish
4. Procedural level sayisi
5. Hint sistemi

Asla kirpmayin:

1. Oynanabilir build
2. Mobile-friendly grid
3. Tema ve Arrow/Vector baglantisi
4. Core puzzle loop
5. Juriye anlasilir sunum

README `R18` bunu zaten destekliyor.

---

## 10. Teknik Riskler ve Cozumler

## 10.1 Risk: Procedural generator sacmaliyor

Cozum:

- max attempt limiti koy
- seed logla
- curated level fallback kullan

## 10.2 Risk: Puzzle fazla belirsiz

Cozum:

- ilk levelleri elle tasarla
- active color count'i erken levellerde dusuk tut
- possible cells highlight'i guclendir

## 10.3 Risk: Mobile UI kucuk kaliyor

Cozum:

- safe area ve canvas scaler test et
- minimum hucre boyutu korunur
- gereksiz metin kaldirilir

## 10.4 Risk: Juri aninda build acilmiyor

Cozum:

- birincil WebGL
- yedek masaustu build
- sunum laptopunda son smoke test

## 10.5 Risk: Hint/checkpoint bug cikartiyor

Cozum:

- ilk teslim surumunde checkpoint'i koru
- hint bug'li ise disable et
- bozuk sistem gostermektense kapatmak daha guvenli

---

## 11. Teslim Paketi Checklist

Kural 14.3, 14.4, 14.5 ve 14.6 icin kritik checklist:

### Zorunlu

- oynanabilir build
- build'in acildiginin son dakika testi
- ekip uyeleri ve roller
- kisa oyun aciklamasi
- temel kontrol bilgisi

### Tavsiye edilen

- AI kullanim notu
- kullanilan asset ve lisans notu
- bilinen kisitlar
- tavsiye edilen ekran/orientation bilgisi

### Paket ornegi

```text
ChromePath_Submission/
  WebGL_Build/ veya ChromePath.exe / ChromePath.apk
  README_Submission.md
  Credits_And_Licenses.md
```

README'de bu teslim paketi acik tanimlanmiyor. Bu bir eksik ve dokuman/teslim tarafinda kapanmali.

---

## 12. Sunum Plani

Kural 16'ya gore takim fiziksel sunum yapacak ve oyun calisir durumda olmali.

Bu nedenle sunum akisi asagidaki gibi olmali:

1. Tek cumle pitch
2. Tema ve Arrow/Vector baglantisi
3. Oyuncu neye bakiyor? Renk sayaçlari
4. Bir kolay level hizli goster
5. Bir orta seviye level ac
6. Checkpoint veya hint'i goster
7. Mobile-friendly yerlesimi vurgula
8. AI kullanildiysa seffafca soyle

README `R17` bu sunum cizgisini iyi destekliyor.

---

## 13. Kural Eslestirme Matrisi

Durum alanlari:

- **Tam**: README'de acik ve yeterli karsilik var
- **Kismi**: README yon veriyor ama teslim/uygulama detayi eksik
- **README Disi**: Kural dogrudan ekip sureci, etik, lojistik veya organizasyon konusu

## 13.1 Tema, konsept, oyun kapsamı ve oynanabilirlik

| Kural | Kural ozeti | README eslesmesi | Durum | Gerekli aksiyon |
|---|---|---|---|---|
| 2.3 | Tema Puzzle | R2, R1 | Tam | Tema aynen tutulmali |
| 8.1 | Proje dijital oyun olmali | R1, R15 | Tam | Unity ile dijital oyun prototipi uretilir |
| 8.2 | Oyun 48 saat icinde tema dogrultusunda gelistirilmeli | R2, R16 | Kismi | Hazir template kullanilabilir ama onceden yapilmis oyun tasinmamali |
| 8.3 | Mobile-friendly tasarim zorunlu | R1, R13, R17 | Tam | Portrait ve buyuk hucreler korunmali |
| 8.4 | Onceden yapilmis oyunu az degisiklikle getirmek yasak | README'de dogrudan yok | README Disi | Sifirdan jam projesi acilmali |
| 8.5 | Onceden hazir library/helper script ve lisansli asset kullanilabilir | R15 dolayli, R17 AI notu dolayli | Kismi | Kullanimlar teslimde listelenmeli |
| 8.6 | Jam sonunda oynanabilir prototip olmali | R1, R9, R16, R18 | Tam | Core loop calisan build zorunlu |
| 9.1 | Tema Puzzle | R2 | Tam | Degisiklik yok |
| 9.2 | En az bir konsept icermeli | R2 | Tam | Arrow/Vector ana konsept olarak korunmali |
| 9.4 | Konseptle anlamli ve gorunur bag olmali | R2, R5, R6, R17 | Tam | Yonu ve rota kararini oyunda net hissettir |
| 9.5 | Baska ekibin fikrini/tasarimini izinsiz kopyalamak yasak | README'de yok | README Disi | Referans al ama clone yapma |
| 15.1-a | Tema/konsept uyumu degerlendirilir | R2, R17 | Tam | Pitch'te bu bag ozellikle anlatilmali |
| 15.1-d | Oynanabilirlik degerlendirilir | R5-R10, R16 | Tam | Bugli yerine dar ama temiz kapsam sec |
| 15.1-f | Teknik kalite degerlendirilir | R14-R18 | Kismi | Debug, fallback ve build smoke test eklenmeli |

## 13.2 Mobile-friendly, UI ve deneyim

| Kural | Kural ozeti | README eslesmesi | Durum | Gerekli aksiyon |
|---|---|---|---|---|
| 8.3 | Mobile-friendly zorunlu | R13, R17 | Tam | Minimum 60x60 hucre korunur |
| 14.4 | Oynanabilir cikti PC, APK veya HTML5/WebGL olabilir | R18 WebGL yedegi, R16 build/upload | Kismi | WebGL build birincil teslim olarak secilmeli |
| 15.1-b | Mobile-friendly gorunum ve tasarim | R13, R17 | Tam | UI buyuk, temiz, touch-uygun olmali |
| 15.1-c | Yaraticilik ve ozgunluk | R4, R7, R10, R11 | Tam | Renk-frekansi + path belirsizligi fark yaratir |
| 15.1-e | Gorsel ve isitsel tasarim | R13, R16, R18 | Kismi | En az temel SFX ve net bir renk dili eklenmeli |
| 16.3 | Sunumda temel oynanis gosterilmeli | R17 | Tam | Demo akisi hazir tutulmali |

## 13.3 Dokumantasyon, build ve teslim

| Kural | Kural ozeti | README eslesmesi | Durum | Gerekli aksiyon |
|---|---|---|---|---|
| 2.2 | 19 Nisan 2026 15:30 sonrasi upload kabul edilmeyebilir | R16 genel zamanlama | Kismi | Son 3-4 saat yalnizca build ve paketleme icin ayrilmali |
| 14.3 | Resmi platforma yukleme zorunlu | README'de yok | README Disi | Organizasyonun duyurdugu platform takip edilmeli |
| 14.4 | Oynanabilir format gerekir | R16, R18 | Kismi | Build ciktisi proje klasorunden ayri hazirlanmali |
| 14.5 | Sadece kaynak dosya yuklemek yetmez | README'de yok | README Disi | Calisan build uretilmeden teslim yapilmaz |
| 14.6 | Kontroller, ekip rolleri, gerekiyorsa kurulum bilgisi tavsiye edilir | R17 kismi | Kismi | Ayrica `README_Submission.md` hazirlanmali |
| 14.7 | Teslim saatine kadar yukleme tamamlanmali | R16 build/upload | Kismi | Final build freeze zamani tanimlanmali |
| 14.8 | Tercih edilen dil Turkce | README Turkce, R17 Turkce | Tam | Oyun ici metinler Turkce tutulabilir |
| 15.1-g | Sunum ve butunluk, aciklama ve kontroller | R17 | Kismi | Menude veya teslim dosyasinda kontrol ozeti olmali |
| 16.1 | Tum takimlar fiziksel sunum yapmali | R17 | Kismi | Sunumu kimin yapacagi onceden belirlenmeli |
| 16.2 | Sunum suresi/formati sonradan duyurulacak | README'de yok | README Disi | WhatsApp duyurulari takip edilmeli |
| 16.3 | Sunumda oyun calisir durumda olmali | R16, R17, R18 | Tam | Son smoke test zorunlu |
| 16.4 | Sunum saatinde hazir olmayanlara ek sure garanti degil | README'de yok | README Disi | Laptop ve build 30 dk once hazir olmali |

## 13.4 Telif, lisans ve AI kullanimi

| Kural | Kural ozeti | README eslesmesi | Durum | Gerekli aksiyon |
|---|---|---|---|---|
| 8.5 | Lisansli hazir asset kullanilabilir | README'de dogrudan yok | Kismi | Tum asset kaynaklari ayrica kaydedilmeli |
| 8.7 | Ucuncu taraf araclar lisans kosullarina uygun kullanilmali | README'de yok | README Disi | Font, ses, plugin, package lisans kontrolu yap |
| 12.1 | IP takima aittir | README'de yok | README Disi | Bilgi notu, teknik aksiyon gerektirmez |
| 12.2 | Tum iceriklerin telif ve lisansina uygunluk beyan edilir | README'de yok | README Disi | `Credits_And_Licenses.md` hazirla |
| 12.3 | Lisanssiz icerik yasak | README'de yok | README Disi | Google'dan rastgele asset indirme |
| 12.4 | APPS oyunu ticari olmayan tanitimlarda gosterebilir | README'de yok | README Disi | Sunum ve sayfa materyali buna uygun olmali |
| 12.5 | Istenirse acik kaynak yapilabilir | README'de yok | README Disi | Jam sonrasina birakilabilir |
| 13.1 | AI kullanimi serbest | R17 AI kullanimi varsa belirt | Kismi | AI kullanim alanlari liste halinde tutulmali |
| 13.2 | AI icerigi de telif kurallarina uygun olmali | R17 dolayli | Kismi | Uretilen assetlerin kaynak/sorumluluk notu tutulmali |
| 13.3 | AI kaynakli hukuki/etik sorumluluk takimdadir | README'de yok | README Disi | Riskli lisansli prompt/asset zincirlerinden kacin |
| 13.4 | AI kullanimi seffafca belirtilmesi tavsiye edilir | R17 | Tam | Sunumda 1 slayt veya 1 cumle ile belirt |

## 13.5 Juri beklentileri ile README uyumu

| Kural | Kural ozeti | README eslesmesi | Durum | Gerekli aksiyon |
|---|---|---|---|---|
| 11.1 | Projeler juri tarafindan degerlendirilir | R17 | Kismi | Demo akisini juri odakli kisa tut |
| 11.2 | Kriterler Madde 15'e gore | R2, R13, R16, R17 | Tam | Plan bu kriterlere gore optimize edilmeli |
| 11.3 | Juri karari nihai | README'de yok | README Disi | Teknik aksiyon yok |
| 11.4 | Juri degerlendirmesi fiziksel alanda | R17 | Kismi | Internet gerektirmeyen yedek build bulunsun |
| 11.5 | Sonuclar duyurulacak | README'de yok | README Disi | Teknik aksiyon yok |
| 15.1 | Genel degerlendirme kriterleri | R2, R5-R18 | Tam | Butun sistemi bu kriterlere gore dengele |

## 13.6 README disinda kalan ama takimin uymasi gereken operasyonel kurallar

Bu maddeler oyun tasarimindan cok ekip davranisi, fiziksel katilim ve organizasyon akisiyla ilgilidir. README'de dogrudan yer almasi gerekmez; ancak ekip icinde ayrica kontrol edilmelidir.

| Kural grubu | Kapsam | README durumu | Takim aksiyonu |
|---|---|---|---|
| 1.1-1.3 | Etkinligin amaci, fiziksel organizasyon, resmi cerceve | README disi | Bilgilendirme |
| 3.1-3.6 | Yas, ogrenci kimligi, takim boyutu, dogru bilgi | README disi | Basvuru uygunlugu kontrolu |
| 4.1-4.5 | Fiziksel katilim, masa, ekipman, internet | README disi | Laptop, sarj, kablo, hotspot planla |
| 5.1-5.3 | WhatsApp Community kullanimi | README disi | Announcements ve Help/Mentor takip et |
| 6.1-6.2 | Mentor destegi | README disi | Tikanilan yerde yardim iste |
| 7.1-7.5 | Mekan duzeni ve guvenlik | README disi | Etkinlik akisini aksatma |
| 10.1-10.5 | Code of Conduct | README disi | Takim ve etkinlik iletisiminde dikkat |
| 17.1-17.3 | Oduller ve sonuc duyurusu | README disi | Teknik etkisi yok |
| 18.1-18.2 | Kisisel veri ve fotograf/video kullanimi | README disi | Bilgilendirme |
| 19.1-19.2 | Ihlal yaptirimlari | README disi | Kural ihlalinden kacin |
| 20.1-20.4 | Degisiklik hakki ve mucbir sebep | README disi | Duyurulari takip et |

---

## 14. README'de Eksik Olan ve Mutlaka Kapatilmasi Gereken Noktalar

README iyi bir game design spesifikasyonu, ancak yarismaya uygun teslim icin asagidaki bosluklar var:

## 14.1 Build format karari eksik

README `R16` sadece `Build & upload` diyor ama:

- WebGL mi?
- APK mi?
- Desktop mu?

net degil.

Karar:

- Birincil teslim: WebGL
- Ikincil yedek: desktop build

## 14.2 Submission dokumani eksik

Kural 14.6 icin ayrica sunlar hazirlanmali:

- oyun aciklamasi
- kontroller
- ekip rolleri
- gerekiyorsa kurulum notu

## 14.3 Lisans ve kredi listesi eksik

Eger su kaynaklardan herhangi biri kullanilacaksa ayri liste tutun:

- font
- ses
- muzik
- icon
- particle pack
- AI uretilmis gorsel
- Unity Asset Store paketi

## 14.4 AI seffaflik notu eksik

README `R17` bunu tek satir not olarak soyluyor ama proje teslimine yansitilmali.

Onerilen sablon:

```text
AI Usage:
- Background texture concepts generated with AI and manually edited.
- No gameplay logic was generated directly by AI without developer review.
```

## 14.5 Demo onboarding eksik

Juri oyunu ilk kez oynayacak. Bu nedenle:

- ilk acilista 2-3 satirlik tutorial overlay
- veya ilk levelde isaretli onboarding

eklenmeli.

---

## 15. Ilk Kod Sprintinde Acilacak Gorevler

Ekip task dagitimi icin pratik backlog:

## Programmer

- grid instantiate sistemi
- path generator
- validator
- undo/checkpoint/hint
- build alma

## Designer / UI

- hucre renk dili
- sayaç paneli
- buton ikonlari
- feedback animasyonlari
- tutorial overlay

## Game Designer

- ilk 10 level progression
- onboarding tuning
- renk/pattern okunabilirligi
- juri demo rotasi

## General QA

- mobile hissi
- touch tolerance
- gereksiz zor level temizligi
- build smoke test

---

## 16. Sonuc

`README.md`, ChromePath'i APPS GameJam 2026'nin temel urun beklentilerine uyacak sekilde tanimliyor. Ozellikle:

- Puzzle temasina uyum var
- Arrow/Vector konsepti gorunur sekilde kurulmus
- Mobile-friendly yon net
- Oynanis mekanigi acik
- Checkpoint, hint ve progression gibi juriye derinlik gosterecek sistemler tanimli

Ancak yarismaya **gercekten uygun teslim** icin README disinda su uc seyin ayrica kapanmasi gerekir:

1. Oynanabilir build formatinin netlestirilmesi
2. Lisans/AI/dokumantasyon paketinin hazirlanmasi
3. Son saatlerin tamamen build, test ve sunuma ayrilmasi

En dogru strateji su:

- once oynanabilir cekirdek puzzle loop
- sonra mobile-friendly UI
- sonra checkpoint/hint
- en sonda polish

Jam kazanmak icin "daha cok ozellik" degil, "daha okunur, daha temiz, daha sorunsuz bir dikey dilim" lazim.
