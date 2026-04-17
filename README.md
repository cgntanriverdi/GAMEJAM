# ChromePath — Game Design & Technical Specification

> APPS GameJam 2026 | Tema: Puzzle — Konsept: Arrow / Vector
> Süre: 48 saat | Engine: [Unity / Godot / Phaser — ekip kararına göre doldur]

---

## 1. Oyun Özeti

ChromePath, renkli bir grid üzerinde verilen **renk sayısı ipuçlarını** kullanarak gizli bir pathin başlangıçtan varış noktasına kadar adım adım bulunmasına dayanan bir mobil-dostu mantık bulmaca oyunudur.

**Core Loop:**
```
Grid görünür → Renk sayıları verilir → Oyuncu adım adım path çizer
→ Her adımda anlık sayaç güncellenir → Varışa ulaşılır → Level geçilir
```

---

## 2. Tema & Konsept Uyumu

- **Tema:** Puzzle
- **Konsept:** Arrow / Vector — oyun mekaniğinin özü rota bulma, yönlendirme ve path navigation'dır. Her adım bir yön kararıdır.

---

## 3. Temel Mekanikler
.
### 3.1 Grid Yapısı

- Grid boyutu level'a göre değişir (bkz. Bölüm 6 Zorluk Skalası).
- Her cell 4 ana renkten birini taşır: **Kırmızı, Yeşil, Mavi, Sarı**.
- Grid tamamen görünürdür; oyuncu tüm renkleri görür.

### 3.2 Path Generation (Arka Planda)

- Oyun başlarken **arka planda** start → varış arasında rastgele geçerli bir path üretilir.
- Bu path oyuncuya **gösterilmez**.
- Path'in geçtiği her cell'in rengi sayılır ve oyuncuya **renk sayıları** olarak verilir.
  - Örnek: `Mavi: 3 | Yeşil: 2 | Kırmızı: 2 | Sarı: 1`
- Path; köşegen hareketi yoktur, sadece **yukarı / aşağı / sağ / sol** yönlerde ilerler.
- Aynı renk sayısına uyan birden fazla path olabilir — bu kasıtlı bir tasarım kararıdır; hint ve checkpoint sistemleri bu belirsizliği yönetir.

### 3.3 Oyuncu Path Çizimi (Adım Adım)

- Oyuncu **start cell'den** başlar ve bir sonraki adım için komşu cell'e (4 yön) dokunarak ilerler.
- Her adımda:
  - Seçilen cell path üzerinde **highlight** edilir.
  - Alt paneldeki renk sayacı **anlık güncellenir**.
  - Eğer herhangi bir renk sayısı **aşılırsa** o rengin sayacı kırmızıya döner — oyuncu yanlış yönde olduğunu anlar, geri sarabilir.

### 3.4 Possible Cell Highlight

- Oyuncunun bulunduğu cell'den hareket edilebilecek **komşu hücreler** (maksimum 3, zaten geçilmiş hücreler hariç) hafifçe pulse/glow animasyonuyla belirtilir.
- Bu; oyuncunun tüm board'a değil **bir sonraki adıma** odaklanmasını sağlar.
- Geçilmiş hücreler tekrar seçilemez (döngü önlemi).

### 3.5 Renk Sayacı (Live Counter)

- Ekranın alt bölümünde her renk için anlık sayaç bulunur.
  ```
  🔴 Kırmızı: 2 / 3
  🔵 Mavi:    1 / 2
  🟢 Yeşil:   2 / 2  ✓
  🟡 Sarı:    0 / 1
  ```
- Yeşil: hedef sayıya ulaşıldı.
- Sarı/Turuncu: hedefe yaklaşıldı.
- Kırmızı: hedef sayı aşıldı — backtrack gerekli.

### 3.6 Undo Sistemi

- Ekranın köşesinde **Undo** butonu bulunur.
- Her basışta **bir adım geri** alınır.
- Sayaç da bir adım geri güncellenir.
- Undo sınırsızdır; oyuncuyu cezalandırmak yerine denemeye teşvik eder.

### 3.7 Level Tamamlama Koşulu

- Oyuncu varış noktasına ulaştığında **tüm renk sayaçları hedefle eşleşiyorsa** level tamamlanır.
- Eşleşmiyorsa varış noktasına ulaşmak yeterli değildir; path geçersizdir, devam etmesi gerekir.

---

## 4. Checkpoint Sistemi

- Her level'da path uzunluğunun **%50'si** tamamlandığında otomatik checkpoint tetiklenir.
- Checkpoint anında:
  - Ekranda kısa bir animasyon/mesaj gösterilir: `"Checkpoint! Yarı yoldasın 🎯"`
  - O ana kadarki doğru path **kilitlenir** (undo ile checkpoint öncesine geri dönülemez).
  - Oyuncu motivasyonu korunur; sıfırdan başlatılmaz.
- Checkpoint'e ulaşıp ulaşılmadığı level geçişinde **rozet** olarak gösterilir.

> **Not:** Checkpoint tamamlanan adım sayısına göre tetiklenir, doğruluğa göre değil. Yani oyuncu %50 adım atmış olmalı — bu adımların doğru path üzerinde olup olmadığı varışta anlaşılır.

---

## 5. Hint Sistemi

### 5.1 Hint Hakkı Kazanma

- Her **3 başarılı level** tamamlandığında oyuncuya **1 hint hakkı** verilir.
- Hint hakları birikebilir.

### 5.2 Hint Kullanımı

- Oyuncu hint butonuna bastığında pathin **ilk %50'si** (yarısı) grid üzerinde kısa süre (örn. 2 saniye) gösterilir, ardından kaybolur.
- Bu sayede:
  - Checkpoint'e ulaşamamış oyuncuya yol gösterilir.
  - Hafıza ve gözlem becerisi devreye girer (2 saniye sonra kaybolduğu için ezberlenmesi gerekir).

### 5.3 In-App Purchase Hook (İleride)

- Hint hakları ilerleyen versiyonlarda IAP ile satın alınabilir olacak şekilde sistem tasarlanmalı.
- Şimdilik sadece 3 level = 1 hint mantığıyla çalışsın.

---

## 6. Level & Zorluk Skalası

| Level Aralığı | Grid Boyutu | Path Uzunluğu | Renk Çeşidi |
|---------------|-------------|----------------|-------------|
| 1 – 3         | 4×4         | 5–7 adım       | 2 renk      |
| 4 – 6         | 5×5         | 8–10 adım      | 3 renk      |
| 7 – 10        | 6×6         | 11–14 adım     | 4 renk      |
| 11+           | 7×7         | 15+ adım       | 4 renk      |

- Zorluk artışı kademeli ve öngörülebilir olmalı.
- İlk 3 level kısa tutulmalı; oyuncunun mekaniği öğrenmesine zaman tanınmalı.
- İlk levellarda renk çeşidi az tutulursa sayaç okuma daha kolay olur.

---

## 7. UI / UX Gereksinimleri

### 7.1 Ana Ekran Bileşenleri

```
┌─────────────────────────────────┐
│  Level 5          [HINT 💡] [↩️] │  ← Header
├─────────────────────────────────┤
│                                 │
│         GRID ALANI              │  ← Merkez, büyük
│   (start 🟢 ... varış 🏁)       │
│                                 │
├─────────────────────────────────┤
│  🔴 3/4   🔵 2/2✓  🟢 1/3  🟡 0/1│  ← Renk Sayaçları
└─────────────────────────────────┘
```

### 7.2 Mobile-First Kurallar

- Grid cell'leri minimum **60×60px** olmalı (parmak dokunuşu için).
- Tüm butonlar thumb reach zone içinde olmalı.
- Portrait mod öncelikli.
- Landscape desteklenebilir ama zorunlu değil.

### 7.3 Animasyonlar

| Olay | Animasyon |
|------|-----------|
| Cell seçimi | Kısa pulse + renk parlama |
| Undo | Path son hücre kaybolur, smooth |
| Sayaç aşımı | Sayaç kırmızıya döner, titreşim |
| Checkpoint | Konfeti / glow burst |
| Level tamamlama | Kapı açılma animasyonu + skor |
| Hint | Path yarısı 2sn parlıyor, fade out |
| Possible cells | Sürekli hafif pulse |

---

## 8. Teknik Notlar

### 8.1 Path Generation Algoritması

```
1. Start ve end point belirlenir (köşeler veya kenar ortaları önerilir).
2. BFS / DFS veya random walk ile start'tan end'e geçerli bir path üretilir.
3. Path minimum uzunluk şartını karşılayana kadar tekrar üretilir.
4. Path hücreleri rastgele renklendirilir (veya board önce renklendirilir, path sonra bulunur).
5. Path üzerindeki renk frekansları sayılır → oyuncuya verilir.
```

### 8.2 Validasyon Mantığı

```
Her adımda:
- Mevcut sayaçlar güncellenir.
- Herhangi bir renk sayısı hedefi aşarsa → UI uyarısı (kırmızı sayaç).
- Oyuncu varışa ulaştığında: tüm sayaçlar == hedef mi? → level complete / geçersiz.
```

### 8.3 Checkpoint Tespiti

```
checkpoint_threshold = floor(path_length / 2)
if (current_step_count >= checkpoint_threshold && !checkpoint_achieved):
    trigger_checkpoint()
    checkpoint_achieved = True
    lock_undo_before_checkpoint()
```

### 8.4 Hint Mantığı

```
hint_path = ilk floor(path_length / 2) adım
göster(hint_path, süre=2sn)
fade_out()
hint_hakki -= 1
```

---

## 9. Dosya / Klasör Yapısı (Önerilen)

```
ChromePath/
├── Assets/
│   ├── Scenes/
│   │   ├── MainMenu.scene
│   │   ├── Game.scene
│   │   └── LevelComplete.scene
│   ├── Scripts/
│   │   ├── GridManager.cs        ← Grid üretimi ve render
│   │   ├── PathGenerator.cs      ← Gizli path algoritması
│   │   ├── PlayerInput.cs        ← Dokunmatik / tıklama input
│   │   ├── CounterUI.cs          ← Renk sayaçları UI
│   │   ├── CheckpointManager.cs  ← Checkpoint & undo lock
│   │   ├── HintManager.cs        ← Hint sistemi
│   │   ├── LevelManager.cs       ← Level config & zorluk
│   │   └── GameManager.cs        ← Genel oyun state
│   ├── Prefabs/
│   │   ├── Cell.prefab
│   │   └── PathHighlight.prefab
│   └── UI/
│       ├── CounterPanel.prefab
│       └── HintButton.prefab
└── README.md
```

> Engine olarak Godot kullanılıyorsa `.cs` → `.gd`, scene yapısı aynı mantıkla kurulabilir.

---

## 10. Geliştirme Öncelik Sırası (40 Saat)

### 🔴 Önce Bunlar (Core — ilk 16 saat)
1. Grid render + renk atama
2. Path generation algoritması
3. Oyuncu input (adım adım cell seçimi)
4. Renk sayacı (live counter)
5. Level tamamlama validasyonu

### 🟡 Sonra Bunlar (Systems — sonraki 14 saat)
6. Undo sistemi
7. Possible cell highlight
8. Checkpoint sistemi
9. Level skalası + LevelManager
10. Hint sistemi

### 🟢 En Son Bunlar (Polish — son 10 saat)
11. Animasyonlar (pulse, konfeti, kapı)
12. Ses efektleri
13. UI/UX düzenlemeleri
14. Mobile test & dokunmatik fine-tuning
15. Build & upload

---

## 11. Jüriye Sunum Notları

- **Tek cümle pitch:** "Renk sayısı ipuçlarını kullanarak gizli pathin üzerinden adım adım geç."
- **Arrow konsept bağlantısı:** Her adım bir yön kararıdır; oyunun özü rota bulma ve yön seçimidir.
- **Mobile-friendly:** Portrait modda, büyük cell boyutlarıyla tamamen dokunmatik oynandığını göster.
- **Demo için:** Level 1-2 çok hızlı geçilir, Level 5-6'dan jüriye göster — zorluk ve mekanik derinlik orada görünür.
- **AI kullanımı (varsa):** Sunum sırasında veya oyun sayfasında belirt.

---

## 12. Riskler & Yedek Planlar

| Risk | Plan B |
|------|--------|
| Path gen algoritması tıkanırsa | Sabit birkaç level hard-code et, procedural sonra ekle |
| Animasyonlara zaman kalmazsa | Sade renk değişimi yeterli, efekt zorunlu değil |
| Mobile build sorunları | WebGL / HTML5 build yedek olarak hazır tut |
| Hint sistemi yetişmezse | Sadece checkpoint yeterli, hint jam sonrası eklenebilir |

---

*Son güncelleme: APPS GameJam 2026 — ChromePath Ekibi*
