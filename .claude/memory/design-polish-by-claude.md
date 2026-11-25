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
- **Durum çubuğu:** Sayfa zemin rengine uyuyor (`MainActivity.UpdateStatusBar` + `AppShell.OnNavigated`). Modal sheet'lerde hâlâ beyaz, çözülmedi.
- **Global stiller:** BoxView stili Transparent; Switch açıkken marka rengi.
- **Senkron hatası düzeltildi:**
  - Kişiler rehbere yazılıyordu (120 kişi) ama `Settings.UNGROUPED_VISIBLE=0` olduğu için Rehber uygulamasında görünmüyordu.
  - `AndroidContactSyncService.EnsureContactsVisible` eklendi; `EnsureAccount` içinden çağrılıyor.
- **Uygulama adı "AUNDE Rehber"** (kullanıcı "Aunde Rehber" dedi, marka kuralı gereği büyük harf AUNDE). Değişen yerler:
  - csproj ApplicationTitle, AndroidManifest label, strings.xml account_label, AppShell Title
  - Login ve Permission metinleri, ProfileViewModel mesajları
  - Eski "StaffSync" hesap/grup geçiş kodu 2026-11-25 akşamı **kaldırıldı** (uygulama canlıda değil, emülatör zaten taşındı). `EnsureAccount` artık "hesap yeni açıldıysa true" döner → tam senkron.
- Ana ekran adı **"Rehber"** (Android: MainActivity `Label`, uygulama tam adı Ayarlar'da "AUNDE Rehber"; iOS: `ApplicationTitle` ios koşullu "Rehber").

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

## YARIM KALANLAR (devam buradan)
1. **Push edilmedi:** `feature/backend-rehber` dalı sadece yerelde commit'li; push/PR kullanıcıya sorulacak.
2. Modal (uyarı kartı, sheet'ler) açıkken durum çubuğu beyaz kalıyor.
3. Gerçek Android cihazda (Samsung) Kişiler uygulamasında hesap görünüyor mu; iOS hiç denenmedi.
4. Çevrimdışı mod testi yapılmadı.
5. Kısa kod 1001 çeviricide kişiyle eşleşmiyor ("Create new contact" öneriyor) — Android kısa numaraları eşleştirmiyor olabilir, araştırılmadı.
6. Fotoğraf yükleme ucu sadece Swagger'dan; admin ekranı yok (mobil kapsam dışı).

Bkz. [[design-decisions-2026-11]], [[contact-sync-rules]], [[aunde-brand]].
