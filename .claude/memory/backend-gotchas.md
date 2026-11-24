---
name: backend-gotchas
description: Backend dikkat noktaları — JWT/şifre düzeltmeleri (refresh istisnası), Program.cs auth, DB, Swagger
metadata:
  type: project
---

- JWT/şifre/config 2026-11-24'te düzeltildi: `ValidateLifetime/Issuer/Audience = true`, token süresi `DateTime.UtcNow`; `GetPrincipalFromExpiredToken` içinde `ValidateLifetime = false` BİLEREK kalır (refresh akışı). Config anahtarı artık `JWT:TokenValidityInMinutes`. Şifre: min 8, rakam + küçük + büyük harf (Identity + `RegisterCommandValidator` birlikte). Eski kısa şifreli kullanıcılar giriş yapabilir, sadece yeni kayıt/şifre değişikliği etkilenir.
- `Program.cs` sadece `UseAuthorization` çağırıyor; .NET 8 `AddAuthentication` kayıtlıyken authentication middleware'i otomatik ekliyor, o yüzden çalışıyor.
- DB: `appsettings*.json` → `127.0.0.1,1433`, `StaffSyncDb` (SQL Server, sa). Deneme için bu mevcut DB kullanılır, yeni DB açılmaz (global kural). appsettings'te secret/şifre açık halde repoda.
- Swagger sadece Development ortamında açık.

**Why:** Bunlar koddan ilk bakışta "hata" gibi görünüp gereksiz düzeltmeye yol açabilir.
**How to apply:** İlgili alana dokunulmadıkça değiştirme; dokunulacaksa kullanıcıya not düş. Bkz. [[project-overview]].
