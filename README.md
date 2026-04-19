# Acıkmış Patiler

APPS GameJam 2026 için 48 saat içinde geliştirilmiş, mobil odaklı bir puzzle oyun prototipi.

Oyuncu, acıkmış patiyi hedef yiyeceğe ulaştırmak için renkli hücrelerden oluşan bir tahtada doğru rotayı bulur. Her seviyede gizli çözüm yolu için gereken renk adetleri verilir; oyuncu bu sayıları aşmayacak şekilde swipe ile ilerler, yanlış hamleleri geri alır ve zamanı iyi kullanarak yıldız kazanır.

## Etkinlik Bilgileri

- Etkinlik: APPS GameJam 2026
- Tema: Puzzle
- Kullanılan konsept: Arrow / Vector
- Motor: Unity
- Unity sürümü: 6000.4.3f1
- Ana sahne: `Assets/Scenes/GameScene.unity`

## Tema ve Konsept Uyumu

Oyun, yön seçimi ve rota çözümü üzerine kurulu olduğu için Arrow / Vector konseptine doğrudan bağlanır. Oyuncunun her swipe hareketi bir yön kararı verir; amaç, verilen renk hedeflerine uyan yolu bulup hedefe ulaşmaktır.

Puzzle tarafında karar mekaniği şu şekilde çalışır:

- Tahtada farklı renkte hücreler bulunur.
- Üst panelde her renkten kaç tane toplanması gerektiği gösterilir.
- Oyuncu sadece yukarı, aşağı, sağ ve sol yönlerinde ilerleyebilir.
- Bir rengin hedef sayısı aşılırsa hamle geçersiz olur.
- Hedefe ulaşmak tek başına yeterli değildir; renk sayıları da hedefle eşleşmelidir.

## Oynanış

1. Ana menüden `Oyna` ile seviye haritasına geçilir.
2. Açık seviyelerden biri seçilir.
3. Oyuncu karakteri başlangıç hücresinden doğru rota boyunca ilerletilir.
4. Üstteki renk hedefleri takip edilir.
5. Hedef yiyeceğe ulaşıldığında seviye tamamlanır.
6. Bitirme süresine göre yıldız kazanılır ve en iyi süre kaydedilir.

## Kontroller

Mobil:

- Swipe yukarı: Bir hücre yukarı git
- Swipe aşağı: Bir hücre aşağı git
- Swipe sola: Bir hücre sola git
- Swipe sağa: Bir hücre sağa git
- Geri alma butonu: Son hamleyi geri al
- İpucu butonu: Mevcut ipucu hakkını kullan
- Harita butonu: Seviye haritasına dön

Editor / klavye testi:

- `W` veya `Yukarı Ok`: Yukarı git
- `S` veya `Aşağı Ok`: Aşağı git
- `A` veya `Sol Ok`: Sola git
- `D` veya `Sağ Ok`: Sağa git
- `Z` veya `Backspace`: Geri al

## Özellikler

- Mobil uyumlu portrait arayüz
- Swipe tabanlı hareket sistemi
- Renk hedeflerine dayalı rota bulma bulmacası
- Seviye haritası ve kilit açma akışı
- Zamanlayıcı, yıldız ve kişisel rekor sistemi
- Geri alma sistemi
- İpucu sistemi: Oyuncu doğru yol üzerindeyse kalan doğru hücrelerin yaklaşık %50'si gösterilir; yanlış yoldaysa önce son doğru hücreye geri döndürülür, ardından ipucu gösterilir.
- Karakter seçimi: köpek, kedi ve tavşan
- Karaktere göre hedef görseli değişimi
- Ayarlar paneli: müzik ve ses efekti seviyesi
- Oyun içi müzik, adım sesi ve seviye tamamlama sesi

## Kurulum ve Çalıştırma

1. Unity Hub üzerinden projeyi açın.
2. Unity sürümü olarak `6000.4.3f1` kullanın.
3. Ana sahneyi açın:

```text
Assets/Scenes/GameScene.unity
```

4. Unity Editor içinde `Play` tuşuna basın.

Gerekli Unity paketleri `Packages/manifest.json` içinde tanımlıdır. Proje açıldığında Unity paketleri otomatik olarak çözmelidir.

## Build Alma

Jam tesliminde yalnızca kaynak proje klasörü yeterli değildir; kurallara göre oynanabilir bir çıktı yüklenmelidir.

Önerilen çıktılar:

- Android için `.apk`
- Web için WebGL / HTML5 build
- PC için çalıştırılabilir build

Unity üzerinden build almak için:

1. `File > Build Profiles` menüsünü açın.
2. Hedef platformu seçin.
3. `Assets/Scenes/GameScene.unity` sahnesinin build listesinde olduğunu kontrol edin.
4. `Build` veya `Build And Run` seçeneğini kullanın.

## Proje Yapısı

```text
Assets/
  Scenes/
    GameScene.unity
  Scripts/
    Core/          Oyun akışı, ses, karakter, level ve input sistemleri
    Grid/          Grid oluşturma ve path üretimi
    Gameplay/      Hamle validasyonu ve ipucu sistemi
    UI/            Menü, harita, sayaç, buton ve responsive arayüz
    Progression/   Kalıcı ayarlar ve ilerleme verileri
  Resources/       Runtime yüklenen görsel ve ses varlıkları
  Images/          Görsel kaynak dosyaları
Packages/
  manifest.json
ProjectSettings/
  ProjectVersion.txt
```

## Ekip

- Ali Çağan Tanrıverdi
- Eren Gürbüz
- Baran Elkansu
- Kaan Uz

## Kullanılan Araçlar ve Kütüphaneler

- Unity 6000.4.3f1
- Unity UI / TextMeshPro
- Unity Input System paketi
- Universal Render Pipeline
- Unity 2D paketleri

Ek kurulum gerektiren harici runtime kütüphanesi yoktur.

## Asset, Ses ve AI Kullanımı

Kurallar gereği kullanılan tüm görsel, işitsel ve yazılı içeriklerin lisans sorumluluğu ekibe aittir.

Bu prototipte:

- Oyun içi arka plan, karakter, yiyecek, buton ve ikon görselleri proje assetleri olarak `Assets/Images` ve `Assets/Resources` altında tutulur.
- Müzik ve ses efektleri `Assets/Resources` altında tutulur.
- Arka plan ve UI görsel iterasyonlarında generative AI destekli üretimden yararlanılmıştır.
- Kod düzenleme, README hazırlama ve polish sürecinde AI destekli geliştirme araçları kullanılmıştır.

Teslim paketi hazırlanırken kullanılan tüm hazır/AI destekli varlıklar için lisans ve kaynak notları ayrıca kontrol edilmelidir.

## Jüriye Kısa Sunum Notu

Acıkmış Patiler, oyuncudan renk hedeflerine sadık kalarak doğru rotayı bulmasını isteyen mobil odaklı bir puzzle oyunudur. Her hareket bir yön kararı olduğu için Arrow / Vector konseptini oynanışın merkezine koyar. Oyuncu rotayı hızlı ve hatasız çözerse daha fazla yıldız kazanır; karakter seçimi, harita akışı, sesler ve bitiş ekranları oyunu jam prototipi seviyesinde tamamlanmış bir deneyime yaklaştırır.

## Bilinen Notlar

- Proje bir gamejam prototipidir; ticari ürün seviyesi tamamlanmışlık hedeflenmemiştir.
- En iyi süre ve yıldız bilgileri oturum içi tutulur.
- Mobil dostu arayüz hedeflenmiştir; teslimden önce seçilen hedef cihazda build testi yapılmalıdır.
