---
name: backend-gotchas
description: Backend dikkat noktaları — JWT/şifre düzeltmeleri (refresh istisnası), Program.cs auth, DB, Swagger
metadata:
  type: project
---

- JWT/şifre/config 2026-11-24'te düzeltildi: `ValidateLifetime/Issuer/Audience = true`, token süresi `DateTime.UtcNow`; `GetPrincipalFromExpiredToken` içinde `ValidateLifetime = false` BİLEREK kalır (refresh akışı). Config anahtarı artık `JWT:TokenValidityInMinutes`. Şifre: min 8, rakam + küçük + büyük harf (Identity + `RegisterCommandValidator` birlikte). Eski kısa şifreli kullanıcılar giriş yapabilir, sadece yeni kayıt/şifre değişikliği etkilenir.
- `Program.cs` sadece `UseAuthorization` çağırıyor; .NET 8 `AddAuthentication` kayıtlıyken authentication middleware'i otomatik ekliyor, o yüzden çalışıyor.
- DB: `appsettings*.json` → `127.0.0.1,1433`, `StaffSyncDb` (SQL Server, sa). **SQL Server Docker konteynerinde** (`staffsync-sql`, mssql/server:2022) — Docker Desktop kapalıysa DB yok, API her istekte 500 verir. Makinedeki SQLEXPRESS örnekleri bu projeyle ilgisiz. Docker Desktop açılınca konteyner kendiliğinden kalkıyor.
- API'yi emülatör için başlatma: `ASPNETCORE_ENVIRONMENT=Development`, `ASPNETCORE_URLS=http://0.0.0.0:5000`, `dotnet run --project Presentation/StaffSync.WebApi --no-launch-profile` (emülatör `10.0.2.2:5000`'e gider; launchSettings 5001/30840 kullanır, uymaz). `--environment` argümanı işe yaramadı, Swagger 404 = Development değil demek.
- Refresh token 2026-11-25'te sıkılaştırıldı: gönderilen refresh token DB'dekiyle eşleşmeli (`AuthRules.RefreshTokenShouldBeValid`), bozuk access token 500 değil 400 döner. Mobil: refresh 400/401 → çıkış; 5xx → oturum korunur (`ApiOfflineException`). Deneme için bu mevcut DB kullanılır, yeni DB açılmaz (global kural). appsettings'te secret/şifre açık halde repoda.
- Swagger sadece Development ortamında açık.

**Why:** Bunlar koddan ilk bakışta "hata" gibi görünüp gereksiz düzeltmeye yol açabilir.
**How to apply:** İlgili alana dokunulmadıkça değiştirme; dokunulacaksa kullanıcıya not düş. Bkz. [[project-overview]].
