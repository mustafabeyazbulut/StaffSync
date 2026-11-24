# StaffSync — Mobil Uygulama ve Rehber Dağıtımı Tasarımı

Tarih: 2026-11-24
Mockup: https://claude.ai/artifact/PNCkj2XAMGQ2HJ3Y52VhKm

## 1. Amaç

Bugün şirket rehberi elle hazırlanıp bir "publish" klasörüne konuyor, oradan cihazlara elle aktarılıyor. StaffSync bu işi otomatikleştirir:

- **Akıllı telefonlar (iOS + Android):** StaffSync uygulaması rehberi gösterir ve telefonun kişiler listesine otomatik aktarır.
- **Tuşlu (dokunmatik olmayan) cep telefonları:** Backend rehberi her gün otomatik olarak vCard (`.vcf`) ve CSV dosyasına çevirip publish klasörüne yazar. Elle dosya hazırlama biter; cihaza yükleme adımı bugünkü gibi (Bluetooth / SD kart / USB) devam eder.

Rehber bilgisinin tek kaynağı StaffSync veritabanıdır (`Contacts` tablosu).

## 2. Kapsam

### Kapsamda
- Backend: .NET 10'a geçiş (yapıldı), AutoMapper'ın kaldırılması, rehber sorgusunun düzeltilmesi ve genişletilmesi, hata bildirimi, tuşlu telefon dışa aktarımı.
- Mobil (.NET MAUI 10): mockup'taki 8 ekran + açılış ekranı + uygulama ikonu, açık/koyu tema, API entegrasyonu, telefon rehberine senkron.
- Marka: AUNDE logosu ve brand book renkleri.

### Kapsam dışı
- Mobilde kişi ekleme/düzenleme/silme (admin işlemleri mobilde yok; backend'deki admin uçları olduğu gibi kalır).
- Masa (IP) telefonları ve santral entegrasyonu.
- Redis (README'de geçiyor ama bu iş kapsamında değil).
- Canlıya yayın (publish profili ayrı bir iş).

## 3. Backend değişiklikleri

### 3.1 Temizlik
- **AutoMapper kaldırılır.** `StaffSync.Mapper` projesi ve `IMapper` arayüzü silinir; 5 kullanım yeri (`RegisterCommandHandler`, `GetContactByIdQueryHandler`, `GetContactQueryHandler`, `GetContactsWithPaginationQueryHandler`, `UpdateContactCommandHandler`) elle eşlemeye çevrilir. Eşleme kodu her alan için tek yerde durur: `Contact` → sonuç DTO'ları için `ContactMappings` adlı static extension sınıfı (`Application/Features/Mappings/`). `BaseHandler` artık `IMapper` almaz.
- MediatR 12.5.0'da sabit kalır (lisans).
- **Mimari korunur:** Onion katmanları, CQRS/MediatR, FluentValidation pipeline, Rules, Generic Repository ve Unit of Work yerinde kalır. Repository'deki hatalar düzeltilir: sayfalamadaki `OrderBy(x => x)` (EF çeviremez) → `Id`'ye göre varsayılan sıralama; `CountAsync` filtreyi uygulamıyor; `Find` takip ayarını uygulamıyor. `IReadRepository`'ye `AsQueryable(bool enableTracking = false)` eklenir; gruplama, sayma ve DTO'ya `Select` gibi sorgular handler'da bunun üzerine kurulur.
- **Hata kodları:** İş kuralı hataları (`BaseException`) bugün 500 dönüyor; 400 dönecek, "bulunamadı" hataları (`NotFoundException`) 404. SendGrid'in hata sınıfları yerine projenin kendi sınıfları kullanılır.
- **Kişi silme soft delete olur** (bugün kayıt fiziksel siliniyor): `IsDeleted = true`. Bütün okuma sorguları silinmiş kişileri dışarıda bırakır.
- `BaseHandler` HTTP isteği dışında da (arka plan servisi) çalışabilsin diye `userId` null olabilir hale gelir.
- `.gitignore` standart .NET şablonuyla değiştirilir; `bin/`, `obj/`, `.vs/` git takibinden çıkarılır (dosyalar diskten silinmez).

### 3.2 Rehber sorgusu (`GetContactsWithPagination`)
Mevcut sorun: sıralama olmadan sayfalama yapılıyor (sayfalar arasında kişi tekrarlanabilir/kaybolabilir), toplam sayı dönmüyor.

Yeni istek:
| Alan | Açıklama |
|---|---|
| `PageNumber`, `PageSize` | `PageSize` en fazla 100 |
| `Search` | Ad, unvan, departman, e-posta, telefon, kısa kodda birlikte arar |
| `Departments` | Çoklu departman (liste) |
| `Company` | Tek şirket |
| `SortBy` | `Name` (varsayılan) / `Department` / `JobTitle`; ikincil sıralama her zaman `DisplayName`, sonra `Id` |

Yeni cevap: `{ items, totalCount, pageNumber, pageSize, hasMore }`. `IsDeleted = true` kayıtlar hiç dönmez.

### 3.3 Filtre seçenekleri
`GET /api/Contacts/FilterOptions` (projedeki `api/[controller]/[action]` düzeni korunur) → `{ departments: [{ name, count }], companies: [{ name, count }] }`. Sayısı 0 olan departman dönmez. Mobil, departman seçme ekranını bununla doldurur.

### 3.4 Senkron için değişiklik takibi
Mobil senkronun her seferinde bütün rehberi indirmemesi için:
- `Contact`'a `UpdatedDate` eklenir (oluşturma/güncelleme/silmede set edilir). Silme zaten soft delete (`IsDeleted`).
- `GET /api/Contacts/Changes?since=<tarih>` → o tarihten sonra eklenen/değişen/silinen kişiler + sunucu saati. `since` yoksa tam liste döner.
- EF migration: `v7_ContactUpdatedDate` (projede `v6` zaten var). Düzeltme tabloları ayrı migration: `v8_ContactCorrections`.

### 3.5 Hatalı bilgi bildirimi
- Yeni entity `ContactCorrection`: `Id`, `ContactId`, `ReportedByUserId`, `CreatedDate`, `Note`, `Status` (`Yeni`/`Uygulandı`/`Reddedildi`), ve satırları: `ContactCorrectionItem` (`FieldName`, `OldValue`, `NewValue`).
- `POST /api/Contacts/ReportCorrection/{id}` (giriş yapmış her kullanıcı). Kural: en az bir alan, her alanın `NewValue`'su dolu ve eski değerden farklı. Aynı kullanıcı aynı kişi için 1 saat içinde en fazla 5 bildirim gönderebilir.
- Kaydedildikten sonra `appsettings` → `Corrections:NotifyEmails` listesindeki adreslere e-posta gider (bildiren, kişi, eski → yeni değerler, not). E-posta gönderilemezse bildirim yine kaydedilir, hata loglanır, kullanıcıya başarı döner.
- E-posta, şirketin SMTP sunucusu üzerinden `System.Net.Mail` ile gönderilir (ayarlar `Smtp` bölümünde). Kullanılmayan SendGrid paketi kaldırılır.
- Admin uçları: `GET /api/Corrections/List?status=New`, `PUT /api/Corrections/UpdateStatus/{id}` (`Roles = "admin"`). Durum değerleri kodda `New` / `Applied` / `Rejected`. Bildirimi "Uygulandı" yapmak kişiyi otomatik değiştirmez; admin kişiyi mevcut `UpdateContact` ile günceller.

### 3.6 Tuşlu telefonlar için dışa aktarım
- `IDirectoryExporter` servisi aktif kişilerden iki dosya üretir:
  - `rehber.vcf` — vCard **2.1** (eski Nokia/Samsung tuşlu telefonların okuduğu sürüm). Türkçe karakterler için `CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE`. Her kişi: `N`, `FN`, `TEL;CELL`, `TEL;WORK` (kısa kod), `EMAIL`, `ORG` (şirket;departman), `TITLE`.
  - `rehber.csv` — UTF-8 BOM'lu, Excel ve telefon yöneticisi programlarının açabileceği düz liste.
- **Zamanlanmış yazma:** Her gün `Export:DailyAt` saatinde (varsayılan 06:00) ve her kişi değişikliğinden 5 dakika sonra (toplu değişiklikleri tek seferde yazmak için) dosyalar `Export:PublishFolder` yoluna yazılır. Önce geçici dosyaya yazılıp sonra yer değiştirilir; yarım dosya oluşmaz.
- **İndirme ucu:** `GET /api/Directory/Export?format=vcf|csv` (giriş yapmış kullanıcı) — publish klasörüne erişimi olmayanlar için.
- Bu iş `BackgroundService` olarak WebApi içinde çalışır.

## 4. Mobil uygulama

### 4.1 Ekranlar (mockup ile birebir)
0. Açılış ekranı — AUNDE Blue zemin, beyaz logo
1. Giriş — logo, e-posta, şifre
2. Rehber izni (ilk açılış) — neden izin istendiği; "Şimdi değil" seçilirse senkron kapalı başlar
3. Rehber — arama, filtre butonu (etkin filtre rozeti), etkin filtre çipleri (✕ ile kaldırılır), A-Z gruplu liste, hızlı arama butonu, sonsuz kaydırma
   - 3b. Filtrele — sıralama, departmanlar satırı, şirket
   - 3c. Departman seç — aramalı, seçilenler üstte, kişi sayıları
4. Kişi detayı — Ara / Mesaj / E-posta / WhatsApp, bilgiler, "Telefon rehberime kaydet", "Hatalı bilgi bildir"
5. Profil — kullanıcı, senkron aç/kapat (kapatırken onay), son senkron, "Şimdi senkronize et", tema (Sistem/Açık/Koyu), çıkış
6. Hatalı bilgi bildir — yanlış alanı seç, doğrusunu yaz, gönder, eski → yeni özeti

Mockup'taki tek fark: Rehber ekranındaki hızlı departman çipleri yerine **sadece etkin filtreler** çip olarak gösterilir (departman sayısı çok olabilir).

### 4.2 Yapı
- MVVM: `CommunityToolkit.Mvvm` (ObservableObject, RelayCommand), Shell navigasyonu (alt sekmeler: Rehber, Profil).
- Servisler (DI ile): `IAuthService` (login, refresh, token saklama `SecureStorage`), `IApiClient` (`HttpClient` + token ekleyen ve 401'de refresh deneyen handler), `IContactsService`, `IContactSyncService` (platforma özel), `ISettingsService` (`Preferences`).
- API adresi `appsettings` benzeri bir sabitte (Debug/Release ayrı).
- Eski sayfalar (`MainPage`, `ProfilePage`, `LoginPage`), `PersonViewModel` ve kullanılmayan PNG'ler kaldırılır.

### 4.3 Görünüm
- Renkler (brand book): AUNDE Blue `#233C82`, Deep Blue `#1E2D48` (metin), Light Blue `#E1EBFF`, griler. Koyu tema mockup'taki palet (`#0B1220` zemin, `#172033` kart, `#8FB0FF` vurgu).
- Font: telefonun sistem fontu (iOS SF Pro, Android Roboto). Mevcut OpenSans dosyaları kaldırılır.
- Uygulama ikonu: AUNDE Blue zemin + beyaz küre (`docs/brand/aunde-logo/aunde-globe-negative.svg`). Açılış: `aunde-logo-negative.svg`. Giriş: açık temada positive, koyu temada negative logo.
- Dokunma alanları en az 44 pt; ekran okuyucu etiketleri ikon butonlarında zorunlu.

### 4.4 Telefon rehberine senkron
Kurallar (değişmez):
1. Eklenen kişilerin StaffSync'ten geldiği belli olur.
2. Senkron kapatılınca StaffSync'in eklediği bütün kişiler silinir; kullanıcının kendi kişilerine dokunulmaz.

- **Android:** "StaffSync" hesap tipi (`AccountManager` + `SyncAdapter`). Kişiler bu hesabın altında; kapatınca hesap silinir. Arka planda günde bir kez + uygulama açılışında senkron.
- **iOS:** `CNContactStore`; kişiler "StaffSync" `CNGroup`'una eklenir, not alanına `StaffSync:<ContactId>` yazılır, eklenen `identifier`'lar uygulamada saklanır. Senkron uygulama açılışında ve "Şimdi senkronize et" ile yapılır (iOS arka planı garanti etmez). Kullanıcının elle birleştirdiği/sildiği kişi bulunamazsa sessizce atlanır.
- Senkron `Changes?since=` ucunu kullanır; ilk senkron tam liste.
- Hata durumunda (ağ yok, izin yok) Profil'de "Son senkron" satırı hatayı gösterir; uygulama çökmez.

## 5. Hata yönetimi
- API hataları mevcut `ExceptionMiddleware` formatında döner; mobil bunları kullanıcıya Türkçe, kısa mesajla gösterir.
- Ağ yokken rehber son indirilen veriden (cihazda saklanan kopya) gösterilir, üstte "Çevrimdışı" bandı çıkar.
- Token süresi dolunca refresh denenir; refresh de başarısızsa giriş ekranına dönülür.

## 6. Test
- Backend: handler'lar için birim testleri (xUnit) — sayfalama sırası, arama, çoklu departman, `Changes`, hata bildirimi kuralları, vCard/CSV üretimi (Türkçe karakterli örnek kişiyle). Testler gerçek DB açmaz.
- vCard dosyası en az bir gerçek tuşlu telefonda (BT'nin elindeki model) içe aktarılarak denenir.
- Mobil: Android emülatör + gerçek iPhone'da senkron aç → kişiler gelir → kapat → kişiler gider senaryosu.

## 7. Yapılış sırası
1. Temizlik: `.gitignore`, AutoMapper'ın kaldırılması.
2. Backend rehber sorgusu + filtre seçenekleri + `Changes` + migration.
3. Hatalı bilgi bildirimi + e-posta.
4. Tuşlu telefon dışa aktarımı (vCard/CSV + zamanlanmış yazma).
5. Mobil iskelet: tema, marka varlıkları, Shell, servisler, giriş.
6. Mobil ekranlar: Rehber + filtreler, Kişi detayı, Hatalı bilgi bildir, Profil.
7. Telefon rehberine senkron (Android, sonra iOS).
