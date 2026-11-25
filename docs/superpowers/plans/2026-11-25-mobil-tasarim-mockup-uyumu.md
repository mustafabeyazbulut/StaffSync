# Mobil Uygulama — Mockup'a Uyum (Görsel Yenileme) Planı

> **Uygulayan ajan için:** Bu plan sadece **görünümü** değiştirir; servisler, API, senkron mantığı
> **değişmez**. Görevleri sırayla yap, her görev ayrı commit (Türkçe mesaj, AI imzası YOK).
> Her görevden sonra Android'de derle **ve emülatörde ekran görüntüsü alıp mockup ile karşılaştır**
> (aşağıda "Doğrulama"). Backend'e dokunma, yeni veritabanı açma.

## Kaynaklar

- **Mockup (birebir referans):** `docs/design/mockup/*.dc.html` — her ekranın HTML kaynağı. Tüm ölçüler
  (px = MAUI birimi), renkler, yazı boyutları, boşluklar ve metinler bu dosyalardaki inline `style`'lardan
  alınır. `renderVals()` içindeki `c = dark ? {...} : {...}` blokları açık/koyu renkleri verir.
  Ekran → dosya: Giriş `Login`, İzin `Permission`, Rehber `Main`, Kişi Detayı `Detail`, Profil `Profile`,
  Hatalı bilgi `Report`, Filtre `Filter`, Departman seç `DeptPicker`, Açılış `Splash`, Koyu tema `DarkTheme`.
- **Uygulamanın şu anki hali:** `docs/design/current-app/01–08.png` (rehber, detay, detay alt, bildirim,
  filtre, departman sıralı liste, arama, profil).
- Logolar: `Resources/Images/aunde_logo_positive.svg` / `aunde_logo_negative.svg` (zaten projede).

## Değişmeyen kararlar

- Font: **telefonun sistem fontu** (mockup Inter kullanıyor; boyut/kalınlıkları al, aileyi alma).
- Kişi ekleme/düzenleme yok; mockup'ta olmayan özellik ekleme.
- Harf çubuğu (A-Z index) mockup'ta yok ama spec'te var: **kalsın**, sade stilde (Task 3).
- Hatalı bilgi bildirimindeki alanlar backend'e göre kalır (Ad soyad, Şirket, Diğer dahil); sadece görünüm mockup'a uyar.

## Neden amatör duruyor (düzeltilecek kök sebepler)

1. **Emoji ikonlar** (🔍 ⚙️ 📞 📱 ☎️ 📧 🏢 📍 👤 ⚠️ ☀️ 🌙 🟢 💬 ✉️) → mockup'ta ince çizgili (stroke 2) ikonlar.
2. **Gölgeli/kenarlıklı kartlar** her satırda → mockup'ta rehber düz liste + ince ayırıcı; gruplu ekranlarda gölgesiz beyaz kart (radius 14) gri zemin üstünde.
3. **Android nav bar başlıkları** ("Kişi Detayı", "Filtrele ve Sırala") → mockup'ta iOS tarzı: büyük başlık (34 bold) veya "‹ Rehber" geri linki; filtre/bildirim/departman **alttan açılan sheet**.
4. **Radyo butonlar / ayrı tema butonları** → mockup'ta **segmented control**.
5. **Mor durum çubuğu** (MAUI şablon rengi `#512BD4`) → sayfa zemin rengi.
6. Tab bar'da ikon yok; Entry'lerde Android alt çizgisi var.

---

### Task 1: Tasarım altyapısı — renk token'ları, ikon fontu, durum çubuğu, Entry stili

**Renk token'ları** (`Resources/Styles/Colors.xaml`): aşağıdaki isimlerle tanımla, ekranlarda **sadece bunları**
kullan (`AppThemeBinding Light=…, Dark=…`). Eski kullanılmayan renkleri temizle.

| Token | Açık | Koyu |
|---|---|---|
| `PageGrouped` (detay, profil, sheet'ler) | `#F2F4F8` | `#0B1220` (sheet: `#0F1729`) |
| `PagePlain` (rehber, giriş, izin) | `#FFFFFF` | `#0B1220` |
| `Card` | `#FFFFFF` | `#172033` |
| `Surface` (arama kutusu, filtre butonu) | `#F1F3F7` | `#172033` |
| `InputBg` (giriş alanları) | `#F7F8FB` | `#172033` |
| `SegTrack` / `SegOn` | `#E4E7EE` / `#FFFFFF` | `#0B1220` / `#2A3650` |
| `TextPrimary` | `#1E2D48` | `#EEF2F8` |
| `TextMuted` | `#5B667A` | `#9AA6BA` |
| `Line` | `#E6E9EF` | `#25304A` |
| `InputBorder` | `#DDE1E8` | `#25304A` |
| `Brand` | `#233C82` | `#233C82` |
| `BrandSoft` | `#E1EBFF` | `#1A2A4D` |
| `BrandText` (link, seçili ikon) | `#233C82` | `#8FB0FF` |
| `TabBarBg` | `#FAFBFD` | `#0F1729` |
| `Danger` | `#C62828` | `#FF8A80` |
| `Ok` / `OkSoft` | `#1B7F4B` / `#E3F4EA` | `#5FD39A` / `#123526` |
| `Ring` (boş seçim halkası) | `#B4BBC8` | `#4A5570` |
| `SelectedTint` (seçili satır zemini) | `#F3F6FC` | `#16223D` |
| `Disabled` / `DisabledText` | `#D5DAE3` / `#5B667A` | `#2A3650` / `#7C879C` |

**İkonlar:** Mockup'taki ikonlar Feather/Lucide setinden. **Lucide ikon fontunu** (`lucide.ttf`, ISC lisans,
npm `lucide-static` paketindeki `font/lucide.ttf`) indirip `Resources/Fonts/` altına koy, `MauiFont` olarak kaydet
(alias `Lucide`). İkonlar `Label FontFamily="Lucide"` veya `FontImageSource` ile, renk token'ıyla.
CDN'den yükleme YOK, dosya projede olacak. Kullanılacaklar: search, sliders-horizontal, phone, message-square,
mail, message-circle (WhatsApp), chevron-left, chevron-right, check, user-plus, info, refresh-cw, users, user,
eye, eye-off, loader. **Hiçbir yerde emoji kalmayacak** (`grep` ile doğrula).

**Durum çubuğu:** Sayfa zemin rengiyle aynı olsun (açık temada beyaz zemin + koyu ikonlar, koyu temada `#0B1220` + açık ikonlar), tema değişince güncellensin.
`CommunityToolkit.Maui` (MIT) ekleyip `StatusBarBehavior` kullanabilirsin; ya da `Platforms/Android/Resources/values/colors.xml`
içindeki şablon renklerini (`#512BD4`, `#2B0B98`) AUNDE renkleriyle değiştirip `MainActivity`'de temaya göre ayarla.

**Entry/Editor:** Android'deki alt çizgiyi handler mapping ile kaldır (`EntryHandler.Mapper.AppendToMapping` →
`BackgroundTintList` şeffaf). Kenarlık/zemin, Entry'yi saran `Border` ile verilir.

**Ortak stiller** (`Styles.xaml`): `LargeTitle` (34, Bold, TextPrimary), `SectionHeader` (13, Bold, BÜYÜK HARF,
TextMuted, sol boşluk 16), `CardBorder` (Card zemin, `StrokeShape RoundRectangle 14`, **Stroke yok, Shadow yok**),
`PrimaryButton` (Brand zemin, beyaz yazı, 52 yükseklik, radius 14, 17 semibold), `SecondaryButton` (Card zemin,
BrandText yazı, 52, radius 14), `Separator` (1px Line).

**Segmented control:** yeniden kullanılabilir bir `SegmentedControl` (ContentView): `SegTrack` zemin, radius 10, padding 2;
seçili segment `SegOn` zemin + hafif gölge, radius 8, 34 yükseklik, 14 medium yazı. Task 4 ve 7'de kullanılır.

**Tab bar:** Rehber = `users`, Profil = `user` ikonu; seçili `BrandText`, seçili değil `TextMuted`; zemin `TabBarBg`, üstte 1px `Line`.

**Shell:** Tüm sayfalarda `Shell.NavBarIsVisible="False"`; başlıklar sayfanın içinde çizilir (aşağıdaki ekranlar).

### Task 2: Giriş, İzin ve Açılış ekranları (`Login`, `Permission`, `Splash`)

- **Giriş:** `PagePlain`, yatay padding 24.
  - Üstte ortalanmış blok: AUNDE logosu (176×32, sola hizalı; açıkta positive, koyuda negative), 28 boşluk,
    "StaffSync" (32, Bold), "Şirket rehberine erişmek için kurumsal hesabınla giriş yap." (16, Muted).
  - Altta: etiketler alanın **üstünde** (13 semibold Muted: "E-posta", "Şifre").
  - Alanlar 52 yükseklik, radius 12, 1px `InputBorder`, `InputBg`.
  - Şifrede sağda göz ikonu (göster/gizle, 44×44).
  - "Giriş yap" `PrimaryButton`. Altında "Şifreni mi unuttun? Bilgi Teknolojileri birimine başvur." (13, Muted, ortalı).
  - Hata mesajı alanın altında `Danger` renkte, küçük.
  - Sunucu adresi değiştirme sadece `#if DEBUG` ve göze batmayan bir yerde kalsın (örn. logoya uzun basma).
- **İzin:** mockup'taki metinler birebir.
  - 88×88 radius 26 `BrandSoft` kutu içinde `refresh-cw` ikonu (44, BrandText).
  - "Gelen aramalarda kim olduğunu gör" (28 Bold) + açıklama paragrafı.
  - 3 madde: 28'lik `BrandSoft` daire içinde check + başlık (15 semibold) + açıklama (14 Muted).
  - Altta "Rehbere erişime izin ver" `PrimaryButton` + "Şimdi değil" (BrandText, düz).
- **Açılış:** zemin `#233C82`, ortada beyaz AUNDE logosu (208×38). `MauiSplashScreen` ayarını kontrol et.

### Task 3: Rehber ekranı (`Main`)

- `PagePlain`. **Başlık bloğu** (üst 56 padding, yatay 20):
  - Solda "Rehber" (`LargeTitle`), altında "Son senkron: bugün 09:42" (13 Muted). Gerçek değer önbellek/senkron zamanından gelsin, "bugün/dün/tarih" formatında.
  - Sağda 44'lük daire avatar (Brand zemin, beyaz baş harfler; kullanıcının adından), dokununca Profil sekmesi.
- **Arama satırı:**
  - Arama kutusu: 44 yükseklik, radius 12, `Surface` zemin, kenarlık ve alt çizgi yok, solda `search` ikonu (18, Muted), placeholder "Ad, unvan veya departman ara".
  - Yanında 44×44 radius 12 `Surface` filtre butonu (`sliders-horizontal`, BrandText); etkin filtre varsa sağ üstte 18'lik `Brand` rozet içinde sayı.
- **Departman çipleri:** yatay kaydırmalı satır; "Tümü" + `FilterOptions`'tan gelen departmanlar.
  - Çip: 34 yükseklik, radius 17, 14 medium.
  - Seçili: Brand zemin + beyaz yazı; seçili değil: `Surface` zemin + TextPrimary.
  - Dokununca o departmanla filtreler (filtre ekranındaki departman seçimiyle aynı state). Ayrı "etkin filtre çipi" satırı kalkar.
- **Liste:** üstte "N kişi gösteriliyor · Ada göre sıralı" (13 Muted, sıralamaya göre metin değişir).
  - **Kart yok, gölge yok.** Satır: yatay padding 20, dikey 10.
    - 44'lük daire avatar (`BrandSoft` zemin, BrandText baş harfler, 15 semibold).
    - Ad (16 semibold, tek satır, `…`) ve altında "Unvan · Departman" (13 Muted).
    - Sağda 44'lük daire `BrandSoft` ara butonu (phone ikonu 18).
    - Satırın altında, avatardan başlayan 1px `Line` ayırıcı.
  - Dahili satırı listede gösterilmez (detayda var).
  - **Grup başlığı:** harf, 13 semibold Muted, letter-spacing; padding 14/20/6. **Gri bant yok.**
  - **Harf çubuğu:** kalır ama sade: 11px BrandText, kalın değil, zemin yok. Her harf için dokunma alanı en az 44 genişlik, ince sütun.
  - Alt yükleniyor satırı: loader ikonu + "Daha fazla kişi yükleniyor" (14 Muted).
  - Boş durum: "Sonuç bulunamadı" (17 semibold) + "Farklı bir ad veya departman deneyin." (14 Muted).
  - Çevrimdışı bandı da aynı dilde olsun: ince, `BrandSoft` zemin, BrandText metin.

### Task 4: Kişi Detayı (`Detail`)

- `PageGrouped`. Üstte nav bar yok; sol üstte "‹ Rehber" (`chevron-left` + metin, 17, BrandText, 44 yükseklik) geri gider.
- **Kimlik bloğu** (ortalı):
  - 96'lık daire avatar: Brand zemin, beyaz baş harfler, 34 semibold.
  - 10 boşluk, ad (26 Bold), unvan (15 Muted).
  - Departman hapı: 26 yükseklik, radius 13, `BrandSoft` zemin + BrandText, 13 medium.
- **Hızlı aksiyonlar:** 4 eşit sütun (`Grid ColumnDefinitions="*,*,*,*"`; **her öğeye `Grid.Column` ver**, şu an hepsi üst üste duruyor), aralık 8.
  - Her biri 68 yükseklikte `Card` (radius 14): ikon (22, BrandText) + etiket (12 medium, BrandText).
  - Ara=phone, Mesaj=message-square, E-posta=mail, WhatsApp=message-circle.
  - Telefon/e-posta yoksa ilgili buton soluk (opacity 0.4) ve pasif.
- **Bilgi kartı:** tek `Card`, satırlar: etiket (13 Muted) üstte, değer (16) altta, padding 12/16, aralarda 1px `Line`. **Emoji ikon yok.**
  - Satırlar: Cep telefonu, Dahili / kısa kod, E-posta, Ofis, Şirket, Yönetici.
  - Dokunulabilir değerler (telefon, dahili, e-posta) BrandText renkte ve dokununca ilgili aksiyonu açar; diğerleri TextPrimary. Boş alan satırı gösterilmez.
- **"Telefon rehberime kaydet":** `SecondaryButton` (Card zemin, BrandText), solda `user-plus` ikonu.
- **"Hatalı bilgi bildir":** 44 yükseklik, ortalı, `info` ikonu (16) + metin (15 medium, **Muted**). Kırmızı ve ⚠️ kalkar.

### Task 5: Hatalı bilgi bildir — sheet (`Report`)

- **Sheet olarak açılır:**
  - iOS: `Shell.PresentationMode="ModalAnimated"` + `ios:Page.ModalPresentationStyle="PageSheet"`.
  - Android: modal tam sayfa, üstte 36×5 gri tutamak (grabber) çizgisi.
  - Zemin `PageGrouped` (koyu: `#0F1729`).
- **Başlık:** 3 sütun; solda "Vazgeç" (17, BrandText), ortada "Düzeltme öner" (17 semibold), sağ boş.
- **Kişi satırı:** 48'lik Brand avatar + ad (17 semibold) + "Yanlış olan bilgiye dokun, doğrusunu yaz." (14 Muted).
- **Alanlar:** tek `Card` içinde satırlar (checkbox/çerçeveli kart YOK). Satır en az 58 yükseklikte: etiket (13 Muted) + mevcut değer (16); sağda 24'lük seçim halkası (2px `Ring`).
  - Seçilince: satır zemini `SelectedTint`; mevcut değer üstü çizili (`TextDecorations="Strikethrough"`) ve Muted; halka Brand dolu + beyaz check.
  - Seçilince satırın altında inline giriş açılır: 44 yükseklik, radius 10, 1.5px BrandText kenarlık, solda "Doğrusu" (13 semibold BrandText), placeholder alana göre ("Yeni numara", "Doğru e-posta"…).
- **Not:** "NOT (İSTEĞE BAĞLI)" başlığı + `Card` içinde 2 satırlık Editor, placeholder "Örn. Ocak ayında departman değiştirdi".
- **Alt çubuk:** üstünde 1px `Line`.
  - Gönder butonu metni dinamik: hiç seçim yoksa "Düzeltme seç" (`Disabled` zemin, pasif), 1 seçimde "1 düzeltmeyi gönder", daha fazlasında "N düzeltmeyi gönder" (Brand).
  - Altında "Önerin, rehberi yöneten birime adınla iletilir." (12 Muted).
- **Başarı durumu** (DisplayAlert YERİNE aynı sheet içinde):
  - 72'lik `OkSoft` daire içinde `Ok` renk check, "Önerin iletildi" (22 Bold), "Onaylandığında rehber herkeste kendiliğinden güncellenecek." (15 Muted).
  - Altında gönderilenlerin kartı: etiket + ~~eski~~ → **yeni**.
  - En altta "Tamam" `PrimaryButton` sheet'i kapatır.

### Task 6: Filtre ve Departman seç — sheet (`Filter`, `DeptPicker`)

- İkisi de Task 5'teki gibi sheet.
- **Filtre:**
  - Başlık: "Temizle" (sol, BrandText) · "Filtrele" (orta, 17 semibold) · "Bitti" (sağ, 17 semibold BrandText).
  - **SIRALAMA:** `SegmentedControl` [Ad | Departman | Unvan]. Radyo butonlar kalkar.
  - **DEPARTMAN:** `Card` içinde tek satır, en az 52 yükseklik:
    - "Departmanlar" (16) + altında seçilenlerin özeti (14 Muted, tek satır `…`).
    - Sağda sayı hapı (`BrandSoft`/BrandText, 24 yükseklik; seçim yoksa "Tümü") + `chevron-right` (Muted).
  - **ŞİRKET:** sarmalanan çipler (Tümü + şirketler). Picker kalkar.
    - Çip: 36 yükseklik, radius 18.
    - Seçili: `BrandSoft` zemin + BrandText yazı + BrandText kenarlık; seçili değil: `Card` zemin + `Line` kenarlık.
  - Alt çubuk: "Sonuçları göster" / "Sonuçları göster · N filtre" `PrimaryButton`.
- **Departman seç:**
  - Başlık: "‹ Filtrele" · "Departman" · "Temizle".
  - Arama kutusu: 40 yükseklik, radius 10, `SegTrack` zemin, search ikonu, placeholder "Departman ara".
  - **SEÇİLENLER** bölümü (varsa) ve **TÜM DEPARTMANLAR** (aramada "SONUÇLAR") bölümü, ikisi de `Card` içinde 48'lik satırlar:
    - Ad (16) + "N kişi" (13 Muted); aralarda 1px `Line`.
    - Sağda 22'lik seçim dairesi: seçiliyse Brand dolu + beyaz check, değilse 2px `Ring`.
  - Eşleşme yoksa "Bu adla bir departman yok."
  - Alt buton: "N departman seçildi" / "Tümünü göster".

### Task 7: Profil (`Profile`)

- `PageGrouped`, üst padding 56, yatay 16, bölümler arası 24.
- "Profil" `LargeTitle`.
- **Kullanıcı kartı** (`Card`, padding 16):
  - 60'lık Brand avatar (beyaz baş harfler, 22 semibold).
  - Ad (18 semibold), e-posta (14 Muted).
  - Departman hapı (`BrandSoft`, 22 yükseklik, 12 semibold).
  - Ad ve departman, e-postaya göre önbellekteki rehber kaydından bulunsun; bulunamazsa e-postanın @ öncesi. "user" rol rozeti gösterilmez (admin ise "Yönetici" hapı).
- **SENKRONİZASYON** başlığı + `Card`:
  - Satır 1: "Telefon rehberine aktar" (16) + "Şirket kişileri cihaz rehberinde görünür" (13 Muted) + sağda Switch (açıkken Brand).
  - Satır 2: "Son senkron" solda, değer sağda (15 Muted).
  - Satır 3: `refresh-cw` ikonu + "Şimdi senkronize et" (16, BrandText, 48 yükseklik; senkron sürerken ikon döner).
  - Kartın altında açıklama: "iPhone'da senkron, uygulama her açıldığında yapılır. Android'de arka planda günde bir kez de çalışır." (13 Muted). Hata varsa bu satır `Danger` renkte hata metnini gösterir.
- **GÖRÜNÜM** başlığı + `Card`: "Tema" + `SegmentedControl` [Sistem | Açık | Koyu]; **seçili olan görünür şekilde işaretli** (şu an belli değil).
- **Son kart:** "Sürüm" + `AppInfo.VersionString` (sağda Muted) | ayırıcı | "Çıkış yap" (16 medium, `Danger`).
- Alttaki "AUNDE Teknik Tekstil A.S." yazısı kaldırılır.

### Task 8: Koyu tema ve son kontrol

- `docs/design/mockup/DarkTheme.dc.html` ve her dosyadaki `dark` renkleri ile tüm ekranları koyu temada kontrol et; Task 1 token'ları dışında renk kalmasın.
- `grep` ile kontrol et:
  - XAML/C# içinde emoji kalmadı.
  - Sabit renk kalmadı (`#` ile başlayan değerler sadece `Colors.xaml`'da).
- Dokunma alanları ≥ 44; ikon butonlarında `SemanticProperties.Description` var.

## Doğrulama (her task sonunda)

1. Backend'i çalıştır: `docker compose up -d`, sonra `Backend/StaffSync/Presentation/StaffSync.WebApi` içinde
   `dotnet run --no-launch-profile --urls http://0.0.0.0:5000` (`ASPNETCORE_ENVIRONMENT=Development`). Uygulama `10.0.2.2:5000`'e bağlanır.
2. `dotnet build Client/StaffSync/StaffSync/StaffSync.csproj -t:Run -f net10.0-android` ile emülatöre yükle
   (giriş bilgisini kullanıcıdan al; repoya yazma).
3. Değişen her ekranın görüntüsünü `adb exec-out screencap -p > ekran.png` ile al. Açık ve koyu temada, ilgili
   `docs/design/mockup/*.dc.html` ile yan yana karşılaştır.
4. Rapor: her ekran için önce/sonra görüntüsü ve mockup'tan bilerek ayrıldığın noktalar (gerekçesiyle).
   Ekran görüntülerini repoya commit etme.
