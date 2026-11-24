# Mobil Uygulama (.NET MAUI 10) — Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Her görevden sonra projeyi derle (`dotnet build Client/StaffSync/StaffSync/StaffSync.csproj -f net10.0-windows10.0.19041.0` veya `-f net10.0-android`); hata varsa sonraki göreve geçmeden düzelt. Her görev ayrı commit olsun.

**Goal:** StaffSync mobil uygulamasını (iOS ve Android) tasarım spec'i (`docs/superpowers/specs/2026-11-24-staffsync-mobil-ve-rehber-dagitimi-design.md`) ve AUNDE marka kimliğine uygun olarak baştan sona işlevsel hale getirmek.

**Architecture:** MVVM (`CommunityToolkit.Mvvm`), Dependency Injection, AppShell navigasyonu, HTTP ApiClient (Bearer token & refresh flow), SQLite/Preferences yerel önbellek, Platform-native Contact Sync (Android Account/ContactsContract, iOS CNContactStore).

**Tech Stack:** .NET 10 MAUI, CommunityToolkit.Mvvm 8.4+, System.Net.Http, Microsoft.Maui.Essentials.

---

## Kurallar
1. **Commit mesajlarında AI/asistan imzası olmaz** (Co-Authored-By yok).
2. **Kişi ekleme/düzenleme/silme mobilde YOKTUR.** Sadece okuma, arama/filtreleme, senkron ve "Hatalı bilgi bildir" vardır.
3. **Senkron kuralları (Değişmez):**
   - Eklenen kişilerin StaffSync'ten geldiği belli olur.
   - Senkron kapatılınca StaffSync'in eklediği tüm kişiler silinir; kullanıcının kendi kişilerine dokunulmaz.
4. **Marka:** AUNDE Blue `#233C82`, Deep Blue `#1E2D48`, Light Blue `#E1EBFF`, Dark Theme `#0B1220` / `#172033`. Sistem fontu (SF Pro / Roboto).
5. **Dokunma alanları en az 44 pt**, erişilebilirlik etiketleri zorunlu.

---

### Task 1: Paketler, Marka Renkleri, Logolar ve Font Temizliği
- `CommunityToolkit.Mvvm` paketini ekle.
- `docs/brand/aunde-logo/` altındaki SVG'leri Resources/Splash ve Resources/AppIcon'a yerleştir.
- `Colors.xaml` ve `Styles.xaml` dosyalarını AUNDE marka rehberine ve açık/koyu tema paletine göre güncelle.
- `Resources/Fonts/` altındaki OpenSans fontlarını kaldır, sistem fontunu (`SystemFont`) varsayılan yap.
- Eski kullanılmayan PNG'leri (`Resources/Images/` içindeki maina, profilp vb.) temizle.

### Task 2: DTO Modelleri ve İstemci Konfigürasyonu
- Backend ile uyumlu DTO'lar: `ContactDto`, `PagedResult<T>`, `FilterOptionDto`, `ContactFilterOptionsResult`, `ContactChangesResult`, `CreateContactCorrectionCommand`, `CorrectionItemInput`.
- `ApiConfig` ve ortam ayarları (BaseUrl: geliştirme / canlı adresi).

### Task 3: Servis Katmanı ve HTTP Pipeline
- `ISettingsService` / `SettingsService` (Preferences: tema seçimi, senkron açık/kapalı, son senkron tarihi, çevrimdışı önbellek).
- `IAuthService` / `AuthService` (Login, Refresh token, SecureStorage token saklama, oturum kontrolü).
- `IApiClient` / `ApiClient` (DelegatingHandler ile Bearer token ekleme, 401 yanıtında otomatik RefreshToken deneme, başarısızsa Login'e yönlendirme).
- `IContactsService` / `ContactsService` (GetContactsWithPagination, FilterOptions, Changes, ReportCorrection).
- `IContactSyncService` (Platform soyutlaması).
- `MauiProgram.cs` servis ve ViewModel DI kayıtları.

### Task 4: Shell Navigasyonu, Tema ve Giriş / İzin Akışı
- `AppShell.xaml`: Rehber ve Profil alt sekmeleri (tab bar).
- `LoginPage.xaml` & `LoginViewModel`: AUNDE logo (açıkta positive, koyuda negative), e-posta, şifre, hata mesajı, yükleniyor göstergesi.
- `PermissionPage.xaml` & `PermissionViewModel`: İlk açılışta rehber senkron izni tanıtım ekranı ("Şimdi değil" seçilirse senkron kapalı başlar).

### Task 5: Rehber Ekranı (Rehber / Contacts)
- `ContactsPage.xaml` & `ContactsViewModel`:
  - Arama çubuğu (metin değiştikçe debounce ile arama).
  - Filtre butonu (etkin filtre sayısı rozeti).
  - Etkin filtre çipleri (seçili departman/şirket çipi, ✕ ile kaldırma).
  - A-Z gruplu liste ve sağ kenar hızlı harf çubuğu.
  - Sonsuz kaydırma (`RemainingItemsThreshold` ile bir sonraki sayfayı çekme).
  - Çevrimdışı desteği: Ağ yoksa son çekilen veriyi gösterme ve üstte "Çevrimdışı" uyarı bandı.
  - Hızlı arama butonu.

### Task 6: Filtreleme ve Departman Seçim Sayfaları
- `FilterBottomSheet.xaml` (veya `FilterPage`):
  - Sıralama seçenekleri: Ad (varsayılan), Departman, Unvan.
  - Departman satırı (seçilen departman sayısı ve seçim sayfasına geçiş).
  - Şirket seçici (Picker / Radio).
  - "Filtreleri Temizle" ve "Uygula" butonları.
- `DepartmentPickerPage.xaml` & `DepartmentPickerViewModel`:
  - Aramalı departman listesi.
  - Kişi sayılarıyla birlikte gösterim.
  - Seçilenler üstte, çoklu seçim onay kutuları.

### Task 7: Kişi Detayı Ekranı
- `ContactDetailPage.xaml` & `ContactDetailViewModel`:
  - Başlık: Ad soyad, unvan, şirket/departman, avatar.
  - Hızlı aksiyon butonları: Ara (`PhoneDialer`), Mesaj (`Sms`), E-posta (`Email`), WhatsApp (`Launcher.OpenAsync`).
  - İletişim bilgileri: Cep telefonu, dahili/kısa kod, e-posta, ofis, yönetici.
  - "Telefon rehberime kaydet" butonu (tekil kişiyi cihaz rehberine ekleme).
  - "Hatalı bilgi bildir" aksiyon linki.

### Task 8: Hatalı Bilgi Bildir Ekranı
- `ReportCorrectionPage.xaml` & `ReportCorrectionViewModel`:
  - Kişi adı ve unvanı.
  - "Hangi bilgi yanlış?" çoklu seçim çipleri (Telefon, Kısa Kod, E-posta, Unvan, Departman, Ofis, Şirket, Yönetici, Diğer).
  - Seçilen her alan için "Mevcut Değer" (salt okunur) ve "Doğru Değer" (giriş kutusu).
  - İsteğe bağlı açıklama/not alanı.
  - "Bildirimi Gönder" butonu (validasyonlar: en az bir alan, değer farklı olmalı).
  - Başarılı gönderim popup'ı / onay ekranı.

### Task 9: Profil Ekranı
- `ProfilePage.xaml` & `ProfileViewModel`:
  - Giriş yapan kullanıcının adı, e-postası ve rolü.
  - "Telefon rehberine senkronize et" toggle'ı.
    - Kapatırken onay modalı ("StaffSync tarafından eklenen N kişi rehberinizden silinecek. Onaylıyor musunuz?").
  - "Son senkron: [tarih/saat]" bilgisi (hata durumunda kırmızı hata uyarısı).
  - "Şimdi senkronize et" butonu ve döner çark.
  - Tema seçimi: Sistem Varsayılanı / Açık / Koyu.
  - "Çıkış Yap" butonu.

### Task 10: Platform Native Senkronizasyon (Android & iOS)
- Android: `AccountManager` & `ContactsContract` üzerinden "StaffSync" hesabı oluşturma ve kişileri bu hesap altına yazma; kapatıldığında hesabı silerek kişileri toplu temizleme.
- iOS: `CNContactStore` üzerinden "StaffSync" `CNGroup` oluşturma, `StaffSync:<ContactId>` işaretleme; kapatıldığında grup kişilerini silme.
- `Changes?since=` ile artımlı (incremental) senkron entegrasyonu.

### Task 11: Tam Derleme, Testler ve Doğrulama
- Windows / Android hedefli derleme testleri.
- UI akışlarının ve mock API testlerinin doğrulanması.
- Dokümantasyon ve final raporu.
