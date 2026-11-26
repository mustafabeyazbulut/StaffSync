---
name: design-polish-by-claude
description: Tasarım rötuşlarını kullanıcı doğrudan Claude'a yaptırıyor; 2026-11-25 oturumunda yapılanlar ve YARIM KALANLAR (devam noktası)
metadata:
  type: project
---

Kullanıcı 2026-11-25'te: "antigravity değil senden istiyorum tasarımsal olarak". Görsel/UX rötuşlarını Claude kendisi yapar; plan yazıp Antigravity'ye devretmez.

**Why:** Antigravity mockup'ı uyguladı ama "dokunma geri bildirimi" diye sadece titreşim ekledi; kullanıcı tıklamanın anlaşılmadığından şikâyetçiydi.
**How to apply:** Tasarım isteklerinde doğrudan kodla ve emülatörde doğrula. Basılı tutmak için `adb shell input swipe x y x y 1200`, eleman koordinatı için `uiautomator dump` kullan; `MSYS_NO_PATHCONV=1` gerekli. Emülatör pixel_5_-_api_33, API 5000 portunda.

## Yapılanlar (2026-11-26'da commit edildi)
- **Basılma efekti:** `Controls/Touch.cs`, `controls:Touch.Feedback="True"` ile kullanılıyor; `PressedScale` ve `CornerRadius` alıyor. `CornerRadius` verilmezse otomatik: yuvarlak kartın kenarına göre köşe alır. Android'de ripple + küçülme, iOS'ta saydamlaşma. Tüm sayfalara ve SegmentedControl'e eklendi. `Platforms/Android/Resources/values/ids.xml` (touch_feedback_tag).
- **Rehber ekranı:**
  - Yüklenirken iskelet satırlar (`ShowSkeleton` + nabız animasyonu).
  - Harf çubuğunda harf balonu.
  - **Departman çipleri kaldırıldı** (kullanıcı istedi). XAML'den ve VM'den silindi; FilterOptions isteği de kalktı.
- **Durum çubuğu (2026-11-26 çözüldü):** `MainActivity.UpdateStatusBar`, `App.PageAppearing` + `ModalPushed` (modal sayfa `Loaded` olunca) + `ModalPopped` + tema değişiminde `App.RefreshStatusBar()` ile çağrılır. Modallar Android'de ayrı DialogFragment penceresinde açılır: ana pencere en üstteki **opak** sayfanın rengini alır, modal pencerelerin durum çubuğu **saydam** bırakılır (yoksa uyarı kartının karartması iki kez biner). Simge açık/koyu seçimi karartılmış renge göre.
- **Global stiller:** BoxView stili Transparent; Switch açıkken marka rengi.
- **Senkron hatası düzeltildi:**
  - Kişiler rehbere yazılıyordu (120 kişi) ama `Settings.UNGROUPED_VISIBLE=0` olduğu için Rehber uygulamasında görünmüyordu.
  - `AndroidContactSyncService.EnsureContactsVisible` eklendi; `EnsureAccount` içinden çağrılıyor.
- **Uygulama adı her yerde "StaffSync"** (2026-11-26). **Repo herkese açık (GitHub public): içinde hiçbir şirkete ait marka/isim/logo/ekran görüntüsü olmaz.** Şirkete özel kopyayı kullanıcı ayrı repoda yapacak; marka dosyaları, planlar ve mockup'lar repodan çıkarılıp kullanıcının masaüstüne taşındı.
  - Yerler: csproj ApplicationTitle (iOS'a özel kısa ad kaldırıldı), AndroidManifest label, MainActivity Label, strings.xml account_label, AppShell Title, Login/Permission metinleri, ProfileViewModel, Android AccountName, iOS GroupName.
  - Android hesap tipi `com.companyname.staffsync` (ApplicationId ile aynı). Eski kurulum kaldırılıp yeniden kurulmalı.
  - Giriş ekranında `staffsync_mark.svg`, ikon/açılış ekranında genel kişi simgesi. Renk paleti (#233C82 vb.) kaldı.
  - Kişi şirketi boşsa telefona boş yazılıyor. Testlerde örnek şirket "ACME".

## 2026-11-25 akşam oturumu (emülatörde test edildi, commit edildi)
- Senkron kilidi: ikinci çağrı atlanıp 0 dönmüyor, bekliyor. `SyncStateChangedMessage` (Messages/) ile Profil + Rehber "Son senkron"u anında güncelleniyor; `IContactSyncService.IsSyncRunning` eklendi. 0 değişiklikte "Rehberiniz güncel" mesajı.
- `Utils/DisplayText` — baş harf / son senkron / kullanıcı adı tek yerde (3 kopya vardı). Rehber'deki sabit "MB" ve tema adını e-posta sanan kod kaldırıldı.
- Önbellek (`ContactCacheService`): atomik yazma (.tmp + Move) + bozuk dosyada kendini sıfırlayıp tam çekme. Emülatörde 0 baytlık bozuk dosya kendiliğinden düzeldi.
- Oturum: refresh 5xx'te artık çıkış yapılmıyor (DB kapalıyken kullanıcı atılıyordu). Backend refresh token DB ile eşleşme kontrolü + testleri (64/64).
- `CONTACTS_STRUCTURE` (xml/contacts.xml) eklendi. **Google Kişiler uygulaması üçüncü taraf hesabı listelemiyor** (sadece Google + Cihaz); Telefon uygulaması aramada/arayan kimliğinde kişileri buluyor. Samsung vb. Kişiler uygulamaları tüm hesapları gösterir — gerçek cihazda bakılmalı.
- Ölü `Utils/SyncSetting.cs` silindi; `SetStatusBarColor` (API 35 obsolete) sürüme göre ayrıldı. Mobil derleme 0 uyarı.

## 2026-11-26 gece oturumu (emülatörde test edildi, commit edildi: ee3dd69, 4083a2e, 9f2bc7e)
- **Arama hiç çalışmıyordu:** Android 11+ paket görünürlüğü; manifeste `<queries>` (DIAL/SENDTO sms-mail/VIEW https) eklendi. VIBRATE izni eklendi (Antigravity `catch {}` ile yutuyordu → titreşim de hiç çalışmıyordu). `Utils/Haptics`.
- Şirket hattı / kısa kod → bkz. [[company-line-shortcode]].
- **Fotoğraflar:** backend `UploadPhoto`/`RemovePhoto` (admin), `Photo/{id}` (giriş yapmış herkes); `IContactPhotoStore` → `FileContactPhotoStore` (App_Data/contact-photos, gitignore'da). ImageUrl = `api/Contacts/Photo/{id}?v={ticks}`. Mobil `IContactPhotoService` indirip önbelleğe alır; `Controls/ContactAvatar` (fotoğraf ya da marka tonunda baş harf). Telefon rehberine de fotoğraf yazılıyor (Android DisplayPhoto akışı, iOS ImageData). Test için 6 kişiye (Id 1,2,11,100,102,104) çizim portre kondu, DB'de ImageUrl güncellendi.
- **Uyarılar:** `IDialogService` (Toast / Alert / Confirm) + `Controls/AppDialogPage` (saydam modal). Tüm DisplayAlertAsync'ler taşındı, metinler kibarlaştırıldı. Sadece Login'deki sunucu adresi prompt'u sistem penceresi.
- Liste satırı tek şablon (`ContactRowTemplate`), ara düğmesi yumuşak marka tonunda hap.
- Ölü kod: SettingsService içindeki ikinci önbellek (contacts_cache.json) silindi.
- **Yayın notu:** App_Data/contact-photos sunucuda çalışma verisi → CanliIIS.pubxml'de App_Data atlama kuralı GEREKİR (global kural). Profil henüz yok.

- **Açılış (2026-11-26):** Shell'in ilk öğesi `startup` (`Pages/StartupPage`, açılış ekranıyla aynı: marka zemini + `splash_mark` simgesi). `App` oturuma göre `//login`, `//permission-intro` ya da `//main/contacts`'a tek seferde gider; rehber arada görünüp girişe atlamaz.

## YARIM KALANLAR (devam buradan)
1. `feature/backend-rehber` 2026-11-26da main.e birleştirildi (fast-forward) ve push edildi; çalışma artık main üzerinde. PR yok ([[solo-developer-no-pr]]).
2. ~~Modal açıkken durum çubuğu beyaz~~ — çözüldü (2026-11-26).
3. Gerçek Android cihazda (Samsung) Kişiler uygulamasında hesap görünüyor mu; iOS hiç denenmedi.
4. Çevrimdışı mod testi yapılmadı.
5. Kısa kod 1001 çeviricide kişiyle eşleşmiyor ("Create new contact" öneriyor) — Android kısa numaraları eşleştirmiyor olabilir, araştırılmadı.
6. Fotoğraf yükleme ucu sadece Swagger'dan; admin ekranı yok (mobil kapsam dışı).

Bkz. [[design-decisions-2026-11]], [[contact-sync-rules]].
