# Mobil Uygulama — Düzeltme Planı (İnceleme Sonrası)

> **Uygulayan ajan için:** Görevleri sırayla yap. Her görevden sonra derle:
> `dotnet build Client/StaffSync/StaffSync/StaffSync.csproj -f net10.0-android` (ve mümkünse `-f net10.0-windows10.0.19041.0`).
> Hata/uyarı (CS0618 dahil) bırakmadan sonraki göreve geç. **Her görev ayrı commit.**

**Bağlam:** `docs/superpowers/plans/2026-11-24-mobil-uygulama.md` (Task 1–10) uygulandı, derleniyor; ama
gerçek backend'e karşı çalışmıyor ve senkron kuralları karşılanmıyor. Spec:
`docs/superpowers/specs/2026-11-24-staffsync-mobil-ve-rehber-dagitimi-design.md`.

## Kurallar (değişmez)

1. Commit mesajlarında AI/asistan imzası yok (`Co-Authored-By`, "Generated with" vb. YASAK).
2. **Backend'e dokunma.** Bu plan sadece `Client/` altını değiştirir. Backend sözleşmesi aşağıda verildi; istemci ona uyar.
3. Yeni/test veritabanı açma. Deneme gerekiyorsa mevcut `docker-compose.yml` MSSQL'i ve `appsettings` DB'si (`StaffSyncDb`) kullanılır.
4. Mobilde kişi ekleme/düzenleme/silme yok (sadece okuma, senkron, hatalı bilgi bildirimi).
5. Senkron kuralları: (a) telefona eklenen kişilerin StaffSync'ten geldiği belli olur, (b) senkron kapatılınca
   StaffSync'in eklediği kişilerin hepsi silinir, kullanıcının kendi kişilerine **asla** dokunulmaz.
6. Hataları sessizce yutma (`catch { }`). En azından `Debug.WriteLine` + kullanıcıya anlamlı mesaj / `LastSyncError`.
7. CDN/harici kaynak yok; yeni NuGet paketi eklemeden önce gerekçesini commit mesajına yaz.

## Backend sözleşmesi (doğru hali)

| İş | Uç | Not |
|---|---|---|
| Giriş | `POST api/Auth/Login` body `{email,password}` → `{token, refreshToken, expiration}` | |
| Token yenile | `POST api/Auth/RefreshToken` body `{accessToken, refreshToken}` → **`{accessToken, refreshToken}`** | `token` ve `expiration` **YOK**. Refresh token her çağrıda değişir (rotation). |
| Rehber (sayfalı) | **`POST api/Contacts/GetContactsWithPagination`** body: `{pageNumber, pageSize, search, departments: string[], company: string, sortBy}` → `{items, totalCount, pageNumber, pageSize, hasMore}` | `sortBy` string enum: `"Name"`, `"Department"`, `"JobTitle"`. **Tek** şirket (`company`). |
| Filtre seçenekleri | `GET api/Contacts/FilterOptions` → `{departments:[{name,count}], companies:[{name,count}]}` | şu an doğru |
| Değişiklikler | `GET api/Contacts/Changes?since=<ISO-8601 UTC>` → `{serverTimeUtc, upserted:[ContactDto], deletedIds:[int]}` | `since` yoksa tüm kişiler gelir ve `deletedIds` **boştur**. |
| Hatalı bilgi | `POST api/Contacts/ReportCorrection/{id}` body `{note, items:[{fieldName,newValue}]}` → 201 | şu an doğru |

Kaynak: `Backend/StaffSync/Presentation/StaffSync.WebApi/Controllers/ContactsController.cs`, `AuthController.cs`.

---

### Task 1: API istemcisi ve rehber sorgusu

**Dosyalar:** `Services/IApiClient.cs`, `Services/IContactsService.cs`, `ViewModels/ContactsViewModel.cs`, `ViewModels/FilterViewModel.cs`, `Pages/FilterPage.xaml(.cs)`, `MauiProgram.cs`

- `GetContactsAsync` → `POST Contacts/GetContactsWithPagination`, gövdede yukarıdaki alanlar. Yeni `ContactsPageRequest` modeli.
  İmzadaki `List<string>? companies` → `string? company`.
- Sıralama değerleri her yerde backend enum adlarıyla: `"Name" | "Department" | "JobTitle"` (şu an `"name"/"department"/"title"`).
- `ApiClient` hata türlerini ayırsın; her şeyi `null`'a çevirmesin:
  - Ağ hatası / zaman aşımı → `HttpRequestException` / `TaskCanceledException` yukarı çıksın (veya `ApiOfflineException`).
  - HTTP hata kodu → `ApiException(StatusCode, mesaj)` (backend `ExceptionMiddleware` gövdesindeki mesajı oku).
  - Çağıranlar buna göre: **sadece ağ hatasında** "Çevrimdışı" bandı + önbellek; 400/404/500'de hata mesajı göster.
- `HttpClient` için `Timeout = 30 sn` (DI kaydında).

**Kabul:** Emülatörde giriş sonrası rehber listesi gelir, kaydırınca 2. sayfa yüklenir, departman + şirket + sıralama filtreleri sonuç değiştirir.

### Task 2: Token yenileme

**Dosyalar:** `Services/IAuthService.cs`, `Services/IApiClient.cs`, `Models/AuthModels.cs`

- `RefreshTokenResponse { AccessToken, RefreshToken }` ekle; refresh cevabını bununla oku.
- Token bitiş zamanını cevaptaki `expiration` yerine **JWT'nin `exp` claim'inden** al (login ve refresh için aynı yol).
  Unix saniye olarak sakla; `DateTime.TryParse` kullanma (şu an UTC/yerel kayması var).
- **Tek seferde bir yenileme:** `SemaphoreSlim(1,1)`. 401 alan istek kilidi bekler; kilit alındığında token, isteğin
  gönderildiği token'dan farklıysa yenileme yapmadan yeni token ile tekrar dener. (Refresh token rotation yüzünden
  paralel yenileme oturumu düşürüyor.)
- Refresh **ağ hatasıyla** başarısız olursa çıkış yapma, login'e yönlendirme. Sadece refresh ucu 400/401 dönerse
  `LogoutAsync` + `//login`.
- `IsAuthenticatedAsync`: token süresi dolmuş + refresh ağ hatası → refresh token varsa `true` dön (çevrimdışı açılış çalışsın).

**Kabul:** Lokal denemede `appsettings.Development.json` içinde `JWT:TokenValidityInMinutes` = 1 yapıp (bu değişikliği
**commit etme**, sonra geri al) 2 dk bekle; liste yenilenince oturum düşmez, yeni token alınır.

### Task 3: Uygulama açılışı (commit edilmemiş değişiklikler dahil)

**Dosyalar:** `App.xaml.cs`, `AppShell.xaml(.cs)`, `Properties/launchSettings.json`, `StaffSync.csproj`

- Çalışma alanındaki commit edilmemiş değişikliklerden şunlar **kalır**: `login` ve `permission-intro`'nun `ShellContent`
  olması (Routing.RegisterRoute'tan çıkarılması), csproj'daki `WindowsAppSDKSelfContained`, launchSettings.
- `App.xaml.cs` geri düzeltilir: `MainPage = ...` **kaldırılır**, `CreateWindow` override'ı geri gelir:
  `new Window(new AppShell())` → `ApplyTheme()` → oturum yoksa `//login`, intro tamamlanmadıysa `//permission-intro`, aksi halde rehberde kal.
- `AppShell.xaml`'da `TabBar` (main) ilk öğe olsun; giriş yapmış kullanıcı login ekranını görmeden rehbere düşsün.
- Açılışta (oturum varsa) arka planda: Task 6'daki önbellek tazeleme + senkron açıksa telefon senkronu (spec: "uygulama açılışında senkron").

**Kabul:** Giriş yap → uygulamayı kapat/aç → doğrudan Rehber açılır. Koyu tema seçili ise açılışta koyu gelir. CS0618 uyarısı yok.

### Task 4: Android senkronu — gerçek hesap + SyncAdapter

**Dosyalar:** `Platforms/Android/Services/AndroidContactSyncService.cs` + yeni dosyalar, `AndroidManifest.xml`, `Platforms/Android/Resources/xml/`

Spec: "StaffSync hesap tipi (`AccountManager` + `SyncAdapter`). Kişiler bu hesabın altında; kapatınca hesap silinir.
Arka planda günde bir kez + uygulama açılışında senkron."

- Hesap tipi: `com.aunde.staffsync`, hesap adı: `StaffSync`.
- `AuthenticatorService` (`[Service(Exported = true)]`, intent-filter `android.accounts.AccountAuthenticator`,
  meta-data `@xml/authenticator`) + boş `AbstractAccountAuthenticator`. `authenticator.xml`: accountType, label "StaffSync", icon.
- `SyncService` + `AbstractThreadedSyncAdapter` (`@xml/syncadapter`: `contentAuthority="com.android.contacts"`,
  accountType, `supportsUploading="false"`, `userVisible="true"`). `OnPerformSync` paylaşılan senkron mantığını çağırır
  (servislere `IPlatformApplication.Current.Services` ile eriş).
- Senkron açılırken: `AccountManager.AddAccountExplicitly`, `ContentResolver.SetIsSyncable(…, 1)`,
  `SetSyncAutomatically(…, true)`, `AddPeriodicSync(…, 86400)`.
- Tüm yazma/silme URI'lerine `CallerIsSyncadapter=true` + `AccountName`/`AccountType` ekle. Kişi eşleştirmesi `SourceId` (= ContactId) ile.
- Upsert'ler tek tek değil **toplu** `ApplyBatch` (örn. 100 kişilik parçalar).
- **Tam senkronda** (`since` = null, `deletedIds` boş gelir): cihazdaki StaffSync hesabında olup `upserted`'da olmayan kişileri sil (uzlaştırma).
- Kapatma: `AccountManager.RemoveAccountExplicitly` (kişiler hesapla birlikte gider) + garanti için hesap tipine göre silme.
- Manifest: `READ_CONTACTS`, `WRITE_CONTACTS`, `READ_SYNC_SETTINGS`, `WRITE_SYNC_SETTINGS` (gereksizse `GET_ACCOUNTS`'u çıkar).
- `catch { }` blokları kaldırılır; hata `LastSyncError`'a yazılır.

**Kabul:** Senkron aç → Kişiler uygulamasında kişiler "StaffSync" hesabı altında görünür; Ayarlar > Hesaplar'da StaffSync var.
Senkron kapat → sadece bu kişiler silinir, önceden elle eklenmiş bir test kişisi yerinde kalır. Tekrar senkronda mükerrer kişi yok.

### Task 5: iOS senkronu — grup + identifier eşlemesi (not alanı OKUNMAZ)

**Dosya:** `Platforms/iOS/Services/IosContactSyncService.cs`

Neden: iOS 13+'ta `CNContactKey.Note` okumak/yazmak Apple onaylı `com.apple.developer.contacts.notes` yetkisi ister;
bu olmadan şu anki kod hata verir, hata yutulur → silme hiç çalışmaz, her senkronda mükerrer kişi oluşur.

- **Not alanını hiç kullanma** (okuma da yazma da).
- "StaffSync" adlı `CNGroup` bir kez oluşturulur, identifier'ı `Preferences`'ta saklanır (silinmişse yeniden oluşturulur).
- `ContactId → CNContact.Identifier` eşlemesi `FileSystem.AppDataDirectory/ios_contact_map.json` içinde tutulur.
- Upsert: eşleme varsa identifier ile çek (gerekli key'ler: isim, telefon, e-posta, kurum, departman, unvan), `MutableCopy` + `UpdateContact`;
  bulunamazsa (kullanıcı silmiş/birleştirmiş) yeni ekle. Yeni kişi `AddMember` ile gruba eklenir.
- Silme / kapatma: sadece eşlemedeki identifier'lar silinir; bulunamayan sessizce atlanır (spec). Kapatmada grup ve eşleme dosyası da silinir.
- Tüm rehberi `EnumerateContacts` ile tarama **yok**. Birden çok değişiklik tek `CNSaveRequest`'te toplanır.
- Tam senkronda Android'deki gibi uzlaştırma.
- Ad: `DisplayName`'i ilk boşluğa göre `GivenName`/`FamilyName`'e böl (son kelime soyad).

**Kabul:** (Mac/iOS cihaz yoksa derleme yeterli, raporda belirt.) `dotnet build -f net10.0-ios` hatasız.

### Task 6: İzin akışı, senkron tetikleme ve çevrimdışı önbellek

**Dosyalar:** `ViewModels/PermissionViewModel.cs`, `ViewModels/ProfileViewModel.cs`, `ViewModels/ContactsViewModel.cs`, `Services/*`

- **İzin isteği ana thread'de:** Sync servisleri `Permissions.RequestAsync` **çağırmaz**, sadece `CheckStatusAsync` yapar.
  İzin isteme `PermissionViewModel.AllowSync` ve Profil'deki toggle'da `MainThread.InvokeOnMainThreadAsync` içinde yapılır.
  Reddedilirse `IsSyncEnabled = false` + kullanıcıya açıklama. Sonra senkron arka planda başlar.
- Profil: senkron sonrası hata varsa "N kişi senkronize edildi" yerine hata gösterilir. Kapatma onayında sayı yazılır:
  "Rehberden N kişi kaldırılacak" (N = Android hesabındaki / iOS eşlemedeki kişi sayısı).
- **Çevrimdışı önbellek ayrı bir servis olsun** (`IContactCacheService`): `Changes` ucu ile kendi `since` anahtarını
  (telefon senkronunun `LastSyncTime`'ından **ayrı**) kullanarak tam listeyi tutar. Açılışta ve rehberde aşağı çekip yenilemede tazelenir.
  `ContactsViewModel` önbelleğe **ilk sayfayı yazmaz** (şu an tam listeyi 25 kişiyle eziyor). Telefon senkron servisleri de önbelleği bu servis üzerinden günceller.
- **Arama yarışı:** `ReloadContactsAsync` içindeki `if (IsBusy) return;` kaldırılır; her yükleme bir `CancellationToken` /
  sürüm numarası alır, eski cevaplar listeye yazılmaz. Böylece son yazılan arama her zaman uygulanır.

**Kabul:** İlk açılışta "İzin ver" → sistem izin penceresi çıkar → kişiler telefona yazılır. Uçak modunda uygulama açılınca
**tüm** kişiler (25 değil) çevrimdışı bandıyla görünür, arama çalışır. Hızlı yazılan aramada son harf de uygulanır.

### Task 7: Arayüz eksikleri

**Dosyalar:** `Pages/ContactsPage.xaml`, `Pages/FilterPage.xaml(.cs)`, `ViewModels/ContactDetailViewModel.cs`, `Converters/`

- **Sıralama radyoları:** string'i `InverseBoolConverter`'a bağlamak yanlış ("Ad" hep seçili görünüyor). VM'de `IsSortByName/IsSortByDepartment/IsSortByJobTitle` bool'ları veya `RadioButton.Value` + `RadioButtonGroup.SelectedValue` kullan.
- **A-Z gruplu liste + harf çubuğu** (planda vardı, yapılmadı): sıralama `Name` iken `CollectionView IsGrouped="True"`, grup anahtarı
  Türkçe kültürle baş harf (`tr-TR`: Ç, Ğ, İ, Ö, Ş, Ü ayrı harf). Sağda harf çubuğu; dokununca `ScrollTo` ile o gruba gider.
  Sonsuz kaydırmada yeni sayfa mevcut gruplara eklenir.
- **"Telefon rehberime kaydet"** şu an sadece soru soruyor. Sistemin yeni kişi ekranını önceden doldurulmuş açsın:
  Android `Intent(ContactsContract.Intents.Insert.Action)` (ad, telefon, e-posta, şirket, unvan);
  iOS `CNContactViewController.FromNewContact`. Bu kişi kullanıcının kendi kişisidir, StaffSync işareti/hesabı **almaz**.
- **Erişilebilirlik:** ikon butonlara `SemanticProperties.Description`; dokunma alanları en az 44×44 (filtre rozeti, çip ✕, harf çubuğu dahil).

**Kabul:** Sıralama değiştirince doğru radyo seçili görünür. Ad sıralamasında harf başlıkları ve çubuk çalışır. "Kaydet" sistem ekranını açar.

### Task 8: Son kontrol ve rapor

- Android + Windows (+ mümkünse iOS) derlemesi, sıfır kod uyarısı.
- Backend'i çalıştır (`docker compose up -d` + WebApi), Android emülatörde baştan sona: giriş → izin → rehber/arama/filtre/sayfalama →
  detay → hatalı bilgi bildir (201) → profil senkron aç/kapat → çıkış → tekrar açılış.
- Kod içinde `catch { }` kalmadığını `grep` ile göster.
- Rapor: her task için ne yapıldı, neyin cihazda test edildiği, neyin sadece derlendiği (özellikle iOS), açık kalanlar.
