---
name: project-overview
description: StaffSync nedir, repo yapısı, teknoloji yığını ve 2026-11-24 itibarıyla geliştirme durumu
metadata:
  type: project
---

StaffSync: şirket içi personel rehberini (Contact) yönetip mobil cihazlara senkronlayan uygulama.

- `Backend/StaffSync/StaffSync.sln` — .NET 8 Web API, Onion Architecture:
  - Core: `StaffSync.Domain` (Contact, Identity User/Role — Guid key), `StaffSync.Application` (CQRS/MediatR, FluentValidation, Rules, Exceptions), `StaffSync.Mapper` (AutoMapper wrapper)
  - Infrastructure: `StaffSync.Persistence` (EF Core SqlServer, Generic Repo + UnitOfWork, Identity store, Migrations v1–v5), `StaffSync.Infrastructure` (JWT TokenService)
  - Presentation: `StaffSync.WebApi` (AuthController, ContactsController, Swagger + Bearer)
  - `Core/StaffSync` (Class1.cs) boş/kullanılmayan proje.
- `Client/StaffSync/StaffSync.sln` — .NET MAUI (android/ios/maccatalyst/windows). Login/Main/Profile sayfaları var.

Durum (2026-11-24):
- Backend: Auth (Login/Register/RefreshToken/Revoke/RevokeAll) ve Contact CRUD + sayfalama/filtre tamam.
- README "Redis" diyor ama kodda Redis yok — planlanan, yapılmadı.
- MAUI client henüz API'ye bağlı değil: `PersonViewModel` sabit örnek veri kullanıyor, HTTP/servis katmanı yok. Senkron ayarı `Utils/SyncSetting.cs` (Preferences key `AundeStaffSync_enabled`).

**Why:** Yeni oturumda projenin nerede kaldığını hızlıca bilmek için.
**How to apply:** Client tarafında iş istenirse API entegrasyonunun sıfırdan kurulacağını varsay. Bkz. [[backend-feature-conventions]], [[backend-gotchas]].
