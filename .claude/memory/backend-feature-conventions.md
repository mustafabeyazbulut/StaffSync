---
name: backend-feature-conventions
description: Backend'e yeni özellik (command/query) eklerken izlenen klasör ve sınıf düzeni
metadata:
  type: project
---

Yeni bir özellik `StaffSync.Application/Features/` altında **türe göre** klasörlenir (özelliğe göre değil), her klasörde `<Alan>Commands`/`<Alan>Handlers` gibi alt klasör:
- `Commands/`, `Queries/` → `IRequest<T>` sınıfları
- `Handlers/` → `BaseHandler`'dan türer (mapper, unitOfWork, httpContextAccessor, `userId` claim'den hazır), `IRequestHandler<,>` uygular
- `Results/` → dönüş DTO'ları (`...QueryResult`, `...CommandResult`)
- `Validators/` → `AbstractValidator<TCommand>`; `FluentValidationBehevior` pipeline'ı otomatik çalıştırır
- `Rules/` → `BaseRules`'tan türeyen iş kuralları; ihlalde `Features/Exceptions/...` altındaki `BaseException` türevini fırlatır, `ExceptionMiddleware` yakalar
- Veri erişimi: `unitOfWork.GetReadRepository<T>()` / `GetWriteRepository<T>()` + `SaveAsync()` (metot adı yazım hatasıyla `AddAysnc`)
- Controller: `[Route("api/[controller]/[action]")]`, sadece `IMediator.Send`; yazma işlemleri `[Authorize(Roles = "admin")]`
- Entity'ler `EntityBase` (int Id, CreatedDate, IsDeleted — soft delete) türer.
- Klasör adı `Beheviors` (yazım hatası) — mevcut adla devam et, yeniden adlandırma isteme.

**Why:** Kullanıcı commit geçmişinde bu yapıyı adım adım kurdu (BaseRules, BaseHandler, pipeline behaviour); yeni kod buna uymalı.
**How to apply:** Yeni command/query eklerken bu 6 klasöre ilgili dosyaları koy, mevcut Contact/Auth örneklerini şablon al. Bkz. [[project-overview]].
