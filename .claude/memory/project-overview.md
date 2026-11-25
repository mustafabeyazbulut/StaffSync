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

Durum (2026-11-25):
- Backend: Auth, Contact CRUD, sayfalı rehber, filtre seçenekleri, Changes (senkron), hatalı bilgi bildirimi, vCard/CSV dışa aktarım tamam.
- README "Redis" diyor ama kodda Redis yok — planlanan, yapılmadı.
- MAUI client MVVM + servis katmanıyla API'ye bağlandı (Antigravity yazdı), ama düzeltme bekliyor: `docs/superpowers/plans/2026-11-25-mobil-duzeltmeler.md`.

**Why:** Yeni oturumda projenin nerede kaldığını hızlıca bilmek için.
**How to apply:** Client işinde önce düzeltme planının ne kadarının yapıldığına bak (git log). Bkz. [[backend-feature-conventions]], [[backend-gotchas]].
