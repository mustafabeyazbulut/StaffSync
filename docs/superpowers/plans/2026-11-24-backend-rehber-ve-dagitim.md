# Backend: Rehber, Hata Bildirimi ve Tuşlu Telefon Dağıtımı — Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** StaffSync backend'ini mobil uygulamanın ve tuşlu telefon rehber dosyalarının ihtiyaç duyduğu hale getirmek: düzgün sayfalama/filtreleme, filtre seçenekleri, değişiklik takibi, hatalı bilgi bildirimi ve otomatik vCard/CSV üretimi.

**Architecture:** Mevcut Onion + CQRS (MediatR 12) + FluentValidation + Rules + Generic Repository/UnitOfWork yapısı korunur. AutoMapper kaldırılır, yerine static eşleme sınıfları gelir. Test edilebilir saf mantık (sorgu kuralları, vCard/CSV üretimi, zamanlama, e-posta metni) static sınıflarda durur ve veritabanı açmadan `List<T>.AsQueryable()` ile test edilir.

**Tech Stack:** .NET 10, ASP.NET Core Web API, EF Core 10 (SQL Server), MediatR 12.5.0, FluentValidation 12, xUnit, System.Net.Mail.

**Spec:** `docs/superpowers/specs/2026-11-24-staffsync-mobil-ve-rehber-dagitimi-design.md`

---

## Önemli kurallar (her görevde geçerli)

- **Yeni/test veritabanı açılmaz.** EF InMemory, LocalDB, `*_Test` DB yok. Testler sadece saf mantığı `List<T>.AsQueryable()` üzerinde test eder. Veritabanı gerektiren doğrulama `appsettings`'teki mevcut `StaffSyncDb` ile, **kullanıcıya sorulduktan sonra** yapılır.
- **Commit mesajlarında Claude/Anthropic imzası olmaz** (`Co-Authored-By` satırı yok).
- **MediatR 12.x'te kalır**, 13+'a yükseltilmez. Yeni kodda mapper kütüphanesi kullanılmaz.
- Komutlar repo kökünden değil, `Backend/StaffSync` klasöründen çalıştırılır (aksi belirtilmedikçe). Kısaltma: `B=Backend/StaffSync`.
- Namespace'ler klasör yolunu izler (projedeki düzen: `StaffSync.Application.Features.Handlers.ContactHandlers` gibi).

## Dosya haritası

**Yeni dosyalar**
| Dosya | Sorumluluk |
|---|---|
| `Tests/StaffSync.Application.Tests/` | xUnit test projesi (saf mantık) |
| `Core/StaffSync.Application/Bases/NotFoundException.cs` | 404 dönen hataların temeli |
| `Core/StaffSync.Application/Exceptions/ExceptionStatusCodes.cs` | Hata → HTTP kodu eşlemesi |
| `Core/StaffSync.Application/Features/Results/ContactResults/ContactDto.cs` | Kişi cevap modeli (eski 3 aynı DTO'nun yerine) |
| `Core/StaffSync.Application/Features/Results/Common/PagedResult.cs` | Sayfalı cevap |
| `Core/StaffSync.Application/Features/Mappings/ContactMappings.cs` | Contact ↔ DTO/komut eşlemesi |
| `Core/StaffSync.Application/Features/Queries/ContactQueries/ContactQueryBuilder.cs` | Arama/filtre/sıralama/sayfa kuralları |
| `Core/StaffSync.Application/Features/Queries/ContactQueries/ContactSortBy.cs` | Sıralama seçenekleri |
| `Core/StaffSync.Application/Features/Queries/ContactQueries/ContactFilterOptions.cs` | Departman/şirket sayıları sorgusu |
| `Core/StaffSync.Application/Features/Queries/ContactQueries/ContactChanges.cs` | Değişen/silinen kişi sorguları |
| `Core/StaffSync.Application/Features/Corrections/*` | Hata bildirimi: alanlar, e-posta metni, kurallar |
| `Core/StaffSync.Application/Features/DirectoryExport/*` | vCard 2.1, QP kodlama, CSV, zamanlama |
| `Core/StaffSync.Application/Interfaces/Email/IEmailSender.cs` | E-posta gönderme arayüzü |
| `Core/StaffSync.Application/Interfaces/Directory/IDirectoryChangeNotifier.cs` | "Rehber değişti" sinyali |
| `Core/StaffSync.Domain/Entities/ContactCorrection.cs`, `ContactCorrectionItem.cs`, `CorrectionStatus.cs` | Bildirim entity'leri |
| `Infrastructure/StaffSync.Infrastructure/Email/*` | SMTP gönderici + ayarlar |
| `Infrastructure/StaffSync.Infrastructure/DirectoryExport/*` | Arka plan dosya yazma servisi, sinyal, ayarlar |
| `Presentation/StaffSync.WebApi/Controllers/CorrectionsController.cs`, `DirectoryController.cs` | Yeni uçlar |

**Silinen:** `Core/StaffSync.Mapper/` (proje), `Interfaces/AutoMapper/IMapper.cs`, `GetContactQueryResult.cs`, `GetContactByIdQueryResult.cs`, `GetContactsWithPaginationQueryResult.cs`.

---

### Task 0: Dal aç ve mevcut .NET 10 geçişini kaydet

**Files:** (yalnızca git)

- [ ] **Step 1: Dal aç**

```bash
cd <repo kökü>
git switch -c feature/backend-rehber
```

- [ ] **Step 2: .NET 10 geçişini, spec'i, marka dosyalarını ve hafızayı commit et** (`obj/` dosyalarını ekleme)

```bash
git add Backend/StaffSync/Core/StaffSync/StaffSync.csproj \
        Backend/StaffSync/Core/StaffSync.Application/StaffSync.Application.csproj \
        Backend/StaffSync/Core/StaffSync.Domain/StaffSync.Domain.csproj \
        Backend/StaffSync/Core/StaffSync.Mapper/StaffSync.Mapper.csproj \
        Backend/StaffSync/Infrastructure/StaffSync.Infrastructure/StaffSync.Infrastructure.csproj \
        Backend/StaffSync/Infrastructure/StaffSync.Persistence/StaffSync.Persistence.csproj \
        Backend/StaffSync/Presentation/StaffSync.WebApi/StaffSync.WebApi.csproj \
        Backend/StaffSync/Presentation/StaffSync.WebApi/Program.cs \
        Client/StaffSync/StaffSync/StaffSync.csproj
git commit -m "Backend ve mobil projeleri .NET 10'a taşı, paketleri güncelle"
git add docs .claude/memory
git commit -m "Tasarım spec'i, AUNDE logo SVG'leri ve proje hafızası"
```

Expected: iki commit; `git status` sadece `bin/` `obj/` değişikliklerini gösterir.

---

### Task 1: .gitignore'u düzelt, derleme çıktılarını takipten çıkar

**Files:**
- Modify: `.gitignore` (repo kökü)

- [ ] **Step 1: Standart .NET .gitignore'u yaz**

```bash
cd <repo kökü>
dotnet new gitignore --force
```

Expected: `.gitignore` artık `[Bb]in/`, `[Oo]bj/`, `.vs/` satırlarını içerir.

- [ ] **Step 2: Takip edilen derleme çıktılarını git'ten çıkar (diskten silinmez)**

```bash
git ls-files -z -- '*/bin/*' '*/obj/*' '*/.vs/*' | xargs -0 git rm --cached -q
git status --short | head
```

Expected: çok sayıda `D` (index'ten silinen) satır; diskteki dosyalar yerinde.

- [ ] **Step 3: Commit**

```bash
git add .gitignore
git commit -m ".gitignore'u .NET şablonuyla değiştir, bin/obj takibini kaldır"
```

---

### Task 2: Test projesi

**Files:**
- Create: `Backend/StaffSync/Tests/StaffSync.Application.Tests/StaffSync.Application.Tests.csproj`

- [ ] **Step 1: Projeyi oluştur ve bağla**

```bash
cd Backend/StaffSync
dotnet new xunit -n StaffSync.Application.Tests -o Tests/StaffSync.Application.Tests -f net10.0
rm Tests/StaffSync.Application.Tests/UnitTest1.cs
dotnet sln StaffSync.sln add Tests/StaffSync.Application.Tests/StaffSync.Application.Tests.csproj
dotnet add Tests/StaffSync.Application.Tests reference Core/StaffSync.Application/StaffSync.Application.csproj
```

`-f net10.0` şablonda yoksa: oluşturduktan sonra `.csproj` içindeki `<TargetFramework>` değerini `net10.0` yap.

- [ ] **Step 2: Boş projenin çalıştığını doğrula**

Run: `dotnet test Tests/StaffSync.Application.Tests`
Expected: build başarılı, "No test is available" ya da 0 test.

- [ ] **Step 3: Commit**

```bash
git add Tests StaffSync.sln
git commit -m "Application için xUnit test projesi ekle"
```

---

### Task 3: Repository hatalarını düzelt, AsQueryable ekle

DB açılmadan test edilemez; doğrulama build + Task 15'teki manuel kontrol ile yapılır.

**Files:**
- Modify: `Core/StaffSync.Application/Interfaces/Repositories/IReadRepository.cs`
- Modify: `Infrastructure/StaffSync.Persistence/Repositories/ReadRepository.cs`

- [ ] **Step 1: Arayüze `AsQueryable` ekle, `GetAsync` dönüşünü nullable yap**

`IReadRepository.cs` içinde `GetAsync` ve `Find` satırlarını şu hale getir, `AsQueryable`'ı ekle:

```csharp
        Task<T?> GetAsync(Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
            bool enableTracking = false);

        IQueryable<T> Find(Expression<Func<T, bool>> predicate, bool enableTracking = false);

        // Gruplama, sayma, Select ile DTO'ya çevirme gibi sorgular için ham sorgu.
        IQueryable<T> AsQueryable(bool enableTracking = false);
```

- [ ] **Step 2: `ReadRepository.cs`'i düzelt**

Dosyanın tamamını şununla değiştir:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using StaffSync.Application.Interfaces.Repositories;
using StaffSync.Domain.Common;
using StaffSync.Persistence.Context;
using System.Linq.Expressions;

namespace StaffSync.Persistence.Repositories
{
    public class ReadRepository<T> : IReadRepository<T> where T : class, IEntityBase, new()
    {
        private readonly StaffSyncContext _context;

        public ReadRepository(StaffSyncContext context)
        {
            _context = context;
        }

        private DbSet<T> Table => _context.Set<T>();

        public IQueryable<T> AsQueryable(bool enableTracking = false)
            => enableTracking ? Table : Table.AsNoTracking();

        public async Task<IList<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderby = null, bool enableTracking = false)
        {
            IQueryable<T> queryable = AsQueryable(enableTracking);
            if (include is not null) queryable = include(queryable);
            if (predicate is not null) queryable = queryable.Where(predicate);
            if (orderby is not null) return await orderby(queryable).ToListAsync();

            return await queryable.ToListAsync();
        }

        public async Task<IList<T>> GetAllByPagingAsync(
                            Expression<Func<T, bool>>? predicate = null,
                            Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
                            Func<IQueryable<T>, IOrderedQueryable<T>>? orderby = null,
                            bool enableTracking = false,
                            int currentPage = 1,
                            int pageSize = 3)
        {
            IQueryable<T> queryable = AsQueryable(enableTracking);
            if (include is not null) queryable = include(queryable);
            if (predicate is not null) queryable = queryable.Where(predicate);

            // Sıralamasız sayfalama sayfalar arasında kayıt tekrarlatır/kaybettirir.
            // Sıralama verilmediyse Id'ye göre sırala (tüm entity'ler EntityBase.Id taşır).
            IOrderedQueryable<T> ordered = orderby is not null
                ? orderby(queryable)
                : queryable.OrderBy(x => EF.Property<int>(x, "Id"));

            return await ordered.Skip((currentPage - 1) * pageSize)
                                .Take(pageSize)
                                .ToListAsync();
        }

        public async Task<T?> GetAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, bool enableTracking = false)
        {
            IQueryable<T> queryable = AsQueryable(enableTracking);
            if (include is not null) queryable = include(queryable);

            return await queryable.FirstOrDefaultAsync(predicate);
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            IQueryable<T> queryable = AsQueryable();
            if (predicate is not null) queryable = queryable.Where(predicate);

            return await queryable.CountAsync();
        }

        public IQueryable<T> Find(Expression<Func<T, bool>> predicate, bool enableTracking = false)
            => AsQueryable(enableTracking).Where(predicate);
    }
}
```

- [ ] **Step 3: Derle**

Run: `dotnet build StaffSync.sln -v q`
Expected: `0 Error(s)` (nullable uyarıları zaten vardı).

- [ ] **Step 4: Commit**

```bash
git add Core/StaffSync.Application/Interfaces/Repositories/IReadRepository.cs Infrastructure/StaffSync.Persistence/Repositories/ReadRepository.cs
git commit -m "ReadRepository: sayfalama sıralaması, CountAsync filtresi ve Find takip ayarı düzeltildi; AsQueryable eklendi"
```

---

### Task 4: Hata kodları — kendi exception sınıfları, SendGrid'in kaldırılması

**Files:**
- Create: `Core/StaffSync.Application/Bases/NotFoundException.cs`
- Create: `Core/StaffSync.Application/Exceptions/ExceptionStatusCodes.cs`
- Modify: `Core/StaffSync.Application/Exceptions/ExceptionMiddleware.cs`
- Modify: `Core/StaffSync.Application/StaffSync.Application.csproj` (SendGrid satırı silinir)
- Test: `Tests/StaffSync.Application.Tests/Exceptions/ExceptionStatusCodesTests.cs`

- [ ] **Step 1: Failing test**

```csharp
using FluentValidation;
using FluentValidation.Results;
using StaffSync.Application.Bases;
using StaffSync.Application.Exceptions;

namespace StaffSync.Application.Tests.Exceptions;

public class ExceptionStatusCodesTests
{
    private class SomeRuleException : BaseException { public SomeRuleException() : base("kural") { } }
    private class SomeNotFoundException : NotFoundException { public SomeNotFoundException() : base("yok") { } }

    [Fact]
    public void Business_rule_exception_is_400() =>
        Assert.Equal(400, ExceptionStatusCodes.For(new SomeRuleException()));

    [Fact]
    public void Not_found_exception_is_404() =>
        Assert.Equal(404, ExceptionStatusCodes.For(new SomeNotFoundException()));

    [Fact]
    public void Validation_exception_is_422() =>
        Assert.Equal(422, ExceptionStatusCodes.For(new ValidationException(new[] { new ValidationFailure("A", "hata") })));

    [Fact]
    public void Unknown_exception_is_500() =>
        Assert.Equal(500, ExceptionStatusCodes.For(new InvalidOperationException()));
}
```

- [ ] **Step 2: Testi çalıştır, derlenmediğini gör**

Run: `dotnet test Tests/StaffSync.Application.Tests`
Expected: FAIL — `NotFoundException` / `ExceptionStatusCodes` bulunamadı.

- [ ] **Step 3: Sınıfları yaz**

`Bases/NotFoundException.cs`:

```csharp
namespace StaffSync.Application.Bases
{
    // Aranan kayıt yoksa fırlatılır; ExceptionMiddleware 404 döner.
    public class NotFoundException : BaseException
    {
        public NotFoundException(string message) : base(message) { }
    }
}
```

`Exceptions/ExceptionStatusCodes.cs`:

```csharp
using FluentValidation;
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Bases;

namespace StaffSync.Application.Exceptions
{
    public static class ExceptionStatusCodes
    {
        // Sıra önemli: NotFoundException bir BaseException'dır, önce kontrol edilmeli.
        public static int For(Exception exception) => exception switch
        {
            ValidationException => StatusCodes.Status422UnprocessableEntity,
            NotFoundException => StatusCodes.Status404NotFound,
            BaseException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
```

`ExceptionMiddleware.cs`: `using SendGrid.Helpers.Errors.Model;` satırını sil; `int statusCode = GetStatusCode(exception);` satırını `int statusCode = ExceptionStatusCodes.For(exception);` yap; dosyanın sonundaki `GetStatusCode` metodunu tamamen sil.

`StaffSync.Application.csproj`: `<PackageReference Include="SendGrid" ... />` satırını sil.

- [ ] **Step 4: Testleri çalıştır**

Run: `dotnet test Tests/StaffSync.Application.Tests`
Expected: 4 test PASS.

- [ ] **Step 5: Commit**

```bash
git add -A Core/StaffSync.Application Tests
git commit -m "İş kuralı hataları 400, bulunamadı hataları 404 dönsün; SendGrid kaldırıldı"
```

---

### Task 5: AutoMapper'ı kaldır — ContactDto, ContactMappings, handler'lar

**Files:**
- Create: `Core/StaffSync.Application/Features/Results/ContactResults/ContactDto.cs`
- Create: `Core/StaffSync.Application/Features/Mappings/ContactMappings.cs`
- Create: `Core/StaffSync.Application/Features/Exceptions/ContactExceptions/ContactNotFoundException.cs`
- Modify: `Core/StaffSync.Application/Features/Rules/ContactRules/ContactRules.cs`
- Modify: `Core/StaffSync.Application/Bases/BaseHandler.cs`
- Modify: tüm handler'lar (liste aşağıda)
- Modify: `Core/StaffSync.Application/Features/Queries/ContactQueries/GetContactQuery.cs`, `GetContactByIdQuery.cs`
- Delete: `Core/StaffSync.Application/Interfaces/AutoMapper/IMapper.cs`, `Features/Results/ContactResults/GetContactQueryResult.cs`, `GetContactByIdQueryResult.cs`
- Delete: `Core/StaffSync.Mapper/` (proje)
- Modify: `Presentation/StaffSync.WebApi/StaffSync.WebApi.csproj`, `Program.cs`
- Test: `Tests/StaffSync.Application.Tests/Mappings/ContactMappingsTests.cs`

- [ ] **Step 1: Failing test**

```csharp
using StaffSync.Application.Features.Commands.ContactCommands;
using StaffSync.Application.Features.Mappings;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Tests.Mappings;

public class ContactMappingsTests
{
    private static Contact Sample() => new()
    {
        Id = 7, DisplayName = "Ayşe Kaya", ImageUrl = "img", Email = "ayse@x.com", TelephoneNumber = "+905320000000",
        Home = "2145", Office = "GM", JobTitle = "İK Uzmanı", Department = "İnsan Kaynakları", Company = "AUNDE", ManagerName = "Mehmet Kurt"
    };

    [Fact]
    public void ToDto_copies_every_field()
    {
        var dto = Sample().ToDto();
        Assert.Equal(7, dto.Id);
        Assert.Equal("Ayşe Kaya", dto.DisplayName);
        Assert.Equal("img", dto.ImageUrl);
        Assert.Equal("ayse@x.com", dto.Email);
        Assert.Equal("+905320000000", dto.TelephoneNumber);
        Assert.Equal("2145", dto.Home);
        Assert.Equal("GM", dto.Office);
        Assert.Equal("İK Uzmanı", dto.JobTitle);
        Assert.Equal("İnsan Kaynakları", dto.Department);
        Assert.Equal("AUNDE", dto.Company);
        Assert.Equal("Mehmet Kurt", dto.ManagerName);
    }

    [Fact]
    public void AsDto_expression_works_in_queries()
    {
        var list = new List<Contact> { Sample() }.AsQueryable().Select(ContactMappings.AsDto).ToList();
        Assert.Equal("Ayşe Kaya", list.Single().DisplayName);
    }

    [Fact]
    public void ApplyTo_overwrites_fields_but_keeps_id_and_created_date()
    {
        var contact = Sample();
        var created = contact.CreatedDate;
        new UpdateContactCommand
        {
            Id = 99, DisplayName = "Yeni Ad", ImageUrl = "i2", Email = "e2", TelephoneNumber = "t2", Home = "h2",
            Office = "o2", JobTitle = "j2", Department = "d2", Company = "c2", ManagerName = "m2"
        }.ApplyTo(contact);

        Assert.Equal(7, contact.Id);
        Assert.Equal(created, contact.CreatedDate);
        Assert.Equal("Yeni Ad", contact.DisplayName);
        Assert.Equal("m2", contact.ManagerName);
    }

    [Fact]
    public void ToEntity_builds_contact_from_create_command()
    {
        var c = new CreateContactCommand
        {
            DisplayName = "Can Öztürk", ImageUrl = "i", Email = "e", TelephoneNumber = "t", Home = "h",
            Office = "o", JobTitle = "j", Department = "d", Company = "c", ManagerName = "m"
        }.ToEntity();
        Assert.Equal("Can Öztürk", c.DisplayName);
        Assert.False(c.IsDeleted);
    }
}
```

- [ ] **Step 2: Testi çalıştır**

Run: `dotnet test Tests/StaffSync.Application.Tests`
Expected: FAIL — `ContactMappings` / `ContactDto` yok.

- [ ] **Step 3: DTO ve eşleme sınıfını yaz**

`Features/Results/ContactResults/ContactDto.cs`:

```csharp
namespace StaffSync.Application.Features.Results.ContactResults
{
    public class ContactDto
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = "";
        public string? ImageUrl { get; set; }
        public string? Email { get; set; }
        public string? TelephoneNumber { get; set; }
        public string? Home { get; set; } // Kısa kod / dahili
        public string? Office { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public string? Company { get; set; }
        public string? ManagerName { get; set; }
    }
}
```

`Features/Mappings/ContactMappings.cs`:

```csharp
using StaffSync.Application.Features.Commands.ContactCommands;
using StaffSync.Application.Features.Results.ContactResults;
using StaffSync.Domain.Entities;
using System.Linq.Expressions;

namespace StaffSync.Application.Features.Mappings
{
    public static class ContactMappings
    {
        // EF sorgularında Select(ContactMappings.AsDto) olarak SQL'e çevrilir; sadece gereken kolonlar okunur.
        public static readonly Expression<Func<Contact, ContactDto>> AsDto = c => new ContactDto
        {
            Id = c.Id,
            DisplayName = c.DisplayName,
            ImageUrl = c.ImageUrl,
            Email = c.Email,
            TelephoneNumber = c.TelephoneNumber,
            Home = c.Home,
            Office = c.Office,
            JobTitle = c.JobTitle,
            Department = c.Department,
            Company = c.Company,
            ManagerName = c.ManagerName
        };

        private static readonly Func<Contact, ContactDto> toDto = AsDto.Compile();

        public static ContactDto ToDto(this Contact contact) => toDto(contact);

        public static Contact ToEntity(this CreateContactCommand command) => new()
        {
            DisplayName = command.DisplayName,
            ImageUrl = command.ImageUrl,
            Email = command.Email,
            TelephoneNumber = command.TelephoneNumber,
            Home = command.Home,
            Office = command.Office,
            JobTitle = command.JobTitle,
            Department = command.Department,
            Company = command.Company,
            ManagerName = command.ManagerName
        };

        // Id ve CreatedDate'e dokunmaz (eski AutoMapper kodu CreatedDate'i eziyordu).
        public static void ApplyTo(this UpdateContactCommand command, Contact contact)
        {
            contact.DisplayName = command.DisplayName;
            contact.ImageUrl = command.ImageUrl;
            contact.Email = command.Email;
            contact.TelephoneNumber = command.TelephoneNumber;
            contact.Home = command.Home;
            contact.Office = command.Office;
            contact.JobTitle = command.JobTitle;
            contact.Department = command.Department;
            contact.Company = command.Company;
            contact.ManagerName = command.ManagerName;
        }
    }
}
```

- [ ] **Step 4: "Kişi bulunamadı" hatası ve kuralı**

`Features/Exceptions/ContactExceptions/ContactNotFoundException.cs`:

```csharp
using StaffSync.Application.Bases;

namespace StaffSync.Application.Features.Exceptions.ContactExceptions
{
    public class ContactNotFoundException : NotFoundException
    {
        public ContactNotFoundException() : base("Kişi bulunamadı.") { }
    }
}
```

`Features/Rules/ContactRules/ContactRules.cs` — sınıfa ekle (mevcut metot kalır):

```csharp
        public Task ContactMustExist(Contact? contact)
        {
            if (contact is null) throw new ContactNotFoundException();
            return Task.CompletedTask;
        }
```

- [ ] **Step 5: BaseHandler'dan IMapper'ı çıkar, userId'yi null-güvenli yap**

`Bases/BaseHandler.cs` tamamı:

```csharp
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Interfaces.UnitOfWorks;
using System.Security.Claims;

namespace StaffSync.Application.Bases
{
    public class BaseHandler
    {
        public readonly IUnitOfWork unitOfWork;
        public readonly IHttpContextAccessor httpContextAccessor;
        // HTTP isteği dışında (arka plan servisi) null'dır.
        public readonly string? userId;

        public BaseHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            this.unitOfWork = unitOfWork;
            this.httpContextAccessor = httpContextAccessor;
            userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}
```

- [ ] **Step 6: Sadece constructor'ı değişen handler'lar**

Her dosyada `using StaffSync.Application.Interfaces.AutoMapper;` satırını sil ve constructor'ı aşağıdakiyle değiştir:

`Features/Handlers/AuthHandlers/LoginCommandHandler.cs`:
```csharp
        public LoginCommandHandler(UserManager<User> userManager, IConfiguration configuration, ITokenService tokenService, AuthRules authRules,
            IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
```

`Features/Handlers/AuthHandlers/RefreshTokenCommandHandler.cs`:
```csharp
        public RefreshTokenCommandHandler(AuthRules authRules, UserManager<User> userManager, ITokenService tokenService,
            IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, httpContextAccessor)
```

`Features/Handlers/AuthHandlers/RevokeCommandHandler.cs`:
```csharp
        public RevokeCommandHandler(UserManager<User> userManager, AuthRules authRules,
            IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, httpContextAccessor)
```

`Features/Handlers/AuthHandlers/RevokeAllCommandHandler.cs`:
```csharp
        public RevokeAllCommandHandler(UserManager<User> userManager,
            IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, httpContextAccessor)
```

(Gövdeler aynı kalır; constructor gövdesindeki atamalar değişmez.)

- [ ] **Step 7: RegisterCommandHandler**

Constructor:
```csharp
        public RegisterCommandHandler(AuthRules authRules, UserManager<User> userManager, RoleManager<Role> roleManager,
            IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, httpContextAccessor)
```
`User user = mapper.Map<User, RegisterCommand>(request);` satırını şununla değiştir:
```csharp
            User user = new() { FullName = request.FullName, Email = request.Email };
```

- [ ] **Step 8: Contact handler'ları**

`Features/Queries/ContactQueries/GetContactQuery.cs` — dönüş tipi:
```csharp
using MediatR;
using StaffSync.Application.Features.Results.ContactResults;

namespace StaffSync.Application.Features.Queries.ContactQueries
{
    public class GetContactQuery : IRequest<List<ContactDto>> { }
}
```

`Features/Queries/ContactQueries/GetContactByIdQuery.cs` — `IRequest<GetContactByIdQueryResult>` → `IRequest<ContactDto>`.

`Features/Handlers/ContactHandlers/GetContactQueryHandler.cs` tamamı:
```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Mappings;
using StaffSync.Application.Features.Queries.ContactQueries;
using StaffSync.Application.Features.Results.ContactResults;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class GetContactQueryHandler : BaseHandler, IRequestHandler<GetContactQuery, List<ContactDto>>
    {
        public GetContactQueryHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
           : base(unitOfWork, httpContextAccessor) { }

        public async Task<List<ContactDto>> Handle(GetContactQuery request, CancellationToken cancellationToken)
        {
            return await unitOfWork.GetReadRepository<Contact>().AsQueryable()
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.DisplayName).ThenBy(c => c.Id)
                .Select(ContactMappings.AsDto)
                .ToListAsync(cancellationToken);
        }
    }
}
```

`Features/Handlers/ContactHandlers/GetContactByIdQueryHandler.cs` tamamı:
```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Mappings;
using StaffSync.Application.Features.Queries.ContactQueries;
using StaffSync.Application.Features.Results.ContactResults;
using StaffSync.Application.Features.Rules.ContactRules;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class GetContactByIdQueryHandler : BaseHandler, IRequestHandler<GetContactByIdQuery, ContactDto>
    {
        private readonly ContactRules contactRules;

        public GetContactByIdQueryHandler(ContactRules contactRules, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
           : base(unitOfWork, httpContextAccessor)
        {
            this.contactRules = contactRules;
        }

        public async Task<ContactDto> Handle(GetContactByIdQuery request, CancellationToken cancellationToken)
        {
            Contact? contact = await unitOfWork.GetReadRepository<Contact>().GetAsync(x => x.Id == request.ContactId && !x.IsDeleted);
            await contactRules.ContactMustExist(contact);
            return contact!.ToDto();
        }
    }
}
```

`Features/Handlers/ContactHandlers/CreateContactCommandHandler.cs` — constructor ve gövde:
```csharp
        public CreateContactCommandHandler(ContactRules contactRules, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
           : base(unitOfWork, httpContextAccessor)
        {
            _contactRules = contactRules;
        }

        public async Task<Unit> Handle(CreateContactCommand request, CancellationToken cancellationToken)
        {
            IList<Contact> sameNumber = await unitOfWork.GetReadRepository<Contact>()
                .GetAllAsync(x => !x.IsDeleted && x.TelephoneNumber == request.TelephoneNumber);
            await _contactRules.ContactTelephoneNumberMustNotBeSame(sameNumber, request.TelephoneNumber);

            await unitOfWork.GetWriteRepository<Contact>().AddAysnc(request.ToEntity());
            await unitOfWork.SaveAsync();
            return Unit.Value;
        }
```
(`using StaffSync.Application.Features.Mappings;` ekle, AutoMapper using'ini sil.)

`Features/Handlers/ContactHandlers/UpdateContactCommandHandler.cs` tamamı:
```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.ContactCommands;
using StaffSync.Application.Features.Mappings;
using StaffSync.Application.Features.Rules.ContactRules;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class UpdateContactCommandHandler : BaseHandler, IRequestHandler<UpdateContactCommand>
    {
        private readonly ContactRules contactRules;

        public UpdateContactCommandHandler(ContactRules contactRules, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
           : base(unitOfWork, httpContextAccessor)
        {
            this.contactRules = contactRules;
        }

        public async Task Handle(UpdateContactCommand request, CancellationToken cancellationToken)
        {
            Contact? contact = await unitOfWork.GetReadRepository<Contact>()
                .GetAsync(x => x.Id == request.Id && !x.IsDeleted, enableTracking: true);
            await contactRules.ContactMustExist(contact);

            request.ApplyTo(contact!);
            await unitOfWork.SaveAsync();
        }
    }
}
```

`Features/Handlers/ContactHandlers/RemoveContactCommandHandler.cs` tamamı (soft delete):
```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.ContactCommands;
using StaffSync.Application.Features.Rules.ContactRules;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class RemoveContactCommandHandler : BaseHandler, IRequestHandler<RemoveContactCommand>
    {
        private readonly ContactRules contactRules;

        public RemoveContactCommandHandler(ContactRules contactRules, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
             : base(unitOfWork, httpContextAccessor)
        {
            this.contactRules = contactRules;
        }

        public async Task Handle(RemoveContactCommand request, CancellationToken cancellationToken)
        {
            Contact? contact = await unitOfWork.GetReadRepository<Contact>()
                .GetAsync(x => x.Id == request.Id && !x.IsDeleted, enableTracking: true);
            await contactRules.ContactMustExist(contact);

            // Soft delete: mobil senkron ve tuşlu telefon dosyası silinen kişiyi bu sayede öğrenir.
            contact!.IsDeleted = true;
            await unitOfWork.SaveAsync();
        }
    }
}
```

`GetContactsWithPaginationQueryHandler.cs` Task 7'de baştan yazılır; bu adımda sadece derlenmesi için constructor'ını şu yap ve `mapper.Map` satırını `contacts.Select(c => c.ToDto())` ile değiştir:
```csharp
        public GetContactsWithPaginationQueryHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor) { }
```
ve dönüş tipini geçici olarak `List<GetContactsWithPaginationQueryResult>` bırakmak yerine: `GetContactsWithPaginationQueryResult` sınıfının tamamını sil, sorgu ve handler'da tipi `List<ContactDto>` yap, son satırı `return contacts.Select(c => c.ToDto()).ToList();` yap.

- [ ] **Step 9: Eski dosyaları ve Mapper projesini sil**

```bash
git rm Core/StaffSync.Application/Interfaces/AutoMapper/IMapper.cs \
       Core/StaffSync.Application/Features/Results/ContactResults/GetContactQueryResult.cs \
       Core/StaffSync.Application/Features/Results/ContactResults/GetContactByIdQueryResult.cs \
       Core/StaffSync.Application/Features/Results/ContactResults/GetContactsWithPaginationQueryResult.cs
dotnet sln StaffSync.sln remove Core/StaffSync.Mapper/StaffSync.Mapper.csproj
git rm -r Core/StaffSync.Mapper
```

`Presentation/StaffSync.WebApi/StaffSync.WebApi.csproj`: `<ProjectReference Include="..\..\Core\StaffSync.Mapper\StaffSync.Mapper.csproj" />` satırını sil.

`Program.cs`: `using StaffSync.Mapper;` ve `builder.Services.AddCustomMapper();` satırlarını sil.

- [ ] **Step 10: Derle ve test et**

Run: `dotnet build StaffSync.sln -v q` → Expected: `0 Error(s)`, `NU1903` (AutoMapper açığı) uyarısı artık yok.
Run: `dotnet test Tests/StaffSync.Application.Tests` → Expected: tüm testler PASS.
Run: `grep -rn "AutoMapper\|IMapper" --include=*.cs --include=*.csproj . | grep -v /obj/` → Expected: çıktı yok.

- [ ] **Step 11: Commit**

```bash
git add -A .
git commit -m "AutoMapper kaldırıldı, elle eşleme; kişi silme soft delete; bulunamayan kişi 404"
```

---

### Task 6: Contact.UpdatedDate ve migration v7

**Files:**
- Modify: `Core/StaffSync.Domain/Entities/Contact.cs`
- Modify: `Infrastructure/StaffSync.Persistence/Configurations/ContactConfiguration.cs`
- Modify: `Infrastructure/StaffSync.Persistence/Context/StaffSyncContext.cs`
- Create: `Infrastructure/StaffSync.Persistence/Migrations/<timestamp>_v7_ContactUpdatedDate.cs` (EF üretir)

- [ ] **Step 1: Alanı ekle**

`Contact.cs` içine (ManagerName'in altına):
```csharp
        // UTC. Her ekleme/güncelleme/silmede StaffSyncContext tarafından set edilir; mobil senkron buna bakar.
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
```

`ContactConfiguration.cs` `Configure` içine:
```csharp
            builder.Property(x => x.UpdatedDate).HasDefaultValueSql("GETUTCDATE()");
            builder.HasIndex(x => x.UpdatedDate);
```

`StaffSyncContext.cs` sınıfına ekle (`using Microsoft.EntityFrameworkCore;` zaten var):
```csharp
        public override int SaveChanges()
        {
            StampContacts();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            StampContacts();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void StampContacts()
        {
            DateTime now = DateTime.UtcNow;
            foreach (var entry in ChangeTracker.Entries<Contact>())
                if (entry.State is EntityState.Added or EntityState.Modified)
                    entry.Entity.UpdatedDate = now;
        }
```

- [ ] **Step 2: Migration üret (DB'ye bağlanmaz)**

```bash
dotnet ef migrations add v7_ContactUpdatedDate --project Infrastructure/StaffSync.Persistence --startup-project Presentation/StaffSync.WebApi
```

- [ ] **Step 3: Migration içeriğini kontrol et**

Üretilen `*_v7_ContactUpdatedDate.cs` dosyasının `Up` metodunda **sadece** şunlar olmalı: `Contacts` tablosuna `UpdatedDate` (`datetime2`, `defaultValueSql: "GETUTCDATE()"`) kolonu ve `IX_Contacts_UpdatedDate` index'i. EF 8→10 geçişinden kaynaklı başka değişiklikler (Identity tabloları vb.) görürsen **dur ve kullanıcıya göster**.

- [ ] **Step 4: Derle**

Run: `dotnet build StaffSync.sln -v q` → Expected: `0 Error(s)`.

- [ ] **Step 5: Commit**

```bash
git add -A Core/StaffSync.Domain Infrastructure/StaffSync.Persistence
git commit -m "Contact.UpdatedDate eklendi (migration v7)"
```

DB'ye uygulama (`dotnet ef database update`) Task 15'te kullanıcı onayıyla yapılır.

---

### Task 7: Rehber sorgusu — arama, çoklu departman, sıralama, toplam sayı

**Files:**
- Create: `Core/StaffSync.Application/Features/Queries/ContactQueries/ContactSortBy.cs`
- Create: `Core/StaffSync.Application/Features/Queries/ContactQueries/ContactQueryBuilder.cs`
- Create: `Core/StaffSync.Application/Features/Results/Common/PagedResult.cs`
- Create: `Core/StaffSync.Application/Features/Validators/ContactValidators/GetContactsWithPaginationQueryValidator.cs`
- Modify: `Core/StaffSync.Application/Features/Queries/ContactQueries/GetContactsWithPaginationQuery.cs`
- Modify: `Core/StaffSync.Application/Features/Handlers/ContactHandlers/GetContactsWithPaginationQueryHandler.cs`
- Modify: `Presentation/StaffSync.WebApi/Program.cs` (enum'lar JSON'da metin)
- Test: `Tests/StaffSync.Application.Tests/Contacts/ContactQueryBuilderTests.cs`, `Tests/StaffSync.Application.Tests/Common/PagedResultTests.cs`

- [ ] **Step 1: Failing testler**

`Contacts/ContactQueryBuilderTests.cs`:
```csharp
using StaffSync.Application.Features.Queries.ContactQueries;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Tests.Contacts;

public class ContactQueryBuilderTests
{
    private static IQueryable<Contact> Data() => new List<Contact>
    {
        new() { Id = 1, DisplayName = "Burak Demir", JobTitle = "Sistem Uzmanı", Department = "Bilgi Teknolojileri", Company = "AUNDE", TelephoneNumber = "111", Home = "2001", Email = "burak@x.com" },
        new() { Id = 2, DisplayName = "Ayşe Kaya", JobTitle = "İK Uzmanı", Department = "İnsan Kaynakları", Company = "AUNDE", TelephoneNumber = "222", Home = "2002", Email = "ayse@x.com" },
        new() { Id = 3, DisplayName = "Ahmet Yılmaz", JobTitle = "Satış Müdürü", Department = "Satış", Company = "Diğer AŞ", TelephoneNumber = "333", Home = "2003", Email = "ahmet@x.com" },
        new() { Id = 4, DisplayName = "Silinmiş Kişi", JobTitle = "X", Department = "Satış", Company = "AUNDE", TelephoneNumber = "444", Home = "2004", Email = "s@x.com", IsDeleted = true },
        new() { Id = 5, DisplayName = "Ahmet Yılmaz", JobTitle = null!, Department = null!, Company = null!, TelephoneNumber = null!, Home = null!, Email = null! },
    }.AsQueryable();

    [Fact]
    public void Deleted_contacts_are_never_returned() =>
        Assert.DoesNotContain(Data().ApplyFilters(null, null, null).ToList(), c => c.Id == 4);

    [Theory]
    [InlineData("Ayşe", 2)]       // ad
    [InlineData("Satış Müd", 3)]  // unvan
    [InlineData("İnsan", 2)]      // departman
    [InlineData("burak@", 1)]     // e-posta
    [InlineData("333", 3)]        // telefon
    [InlineData("2002", 2)]       // kısa kod
    public void Search_matches_any_field(string search, int expectedId) =>
        Assert.Equal(new[] { expectedId }, Data().ApplyFilters(search, null, null).Select(c => c.Id).ToArray());

    [Fact]
    public void Search_survives_null_fields() =>
        Assert.Empty(Data().ApplyFilters("yok-boyle-bir-sey", null, null).ToList());

    [Fact]
    public void Multiple_departments_are_ORed() =>
        Assert.Equal(new[] { 1, 3 }, Data().ApplyFilters(null, new[] { "Bilgi Teknolojileri", "Satış" }, null).Select(c => c.Id).OrderBy(x => x).ToArray());

    [Fact]
    public void Company_filter_is_exact() =>
        Assert.Equal(new[] { 3 }, Data().ApplyFilters(null, null, "Diğer AŞ").Select(c => c.Id).ToArray());

    [Fact]
    public void Sort_by_name_breaks_ties_by_id() =>
        Assert.Equal(new[] { 3, 5, 2, 1 }, Data().ApplyFilters(null, null, null).ApplySort(ContactSortBy.Name).Select(c => c.Id).ToArray());

    [Fact]
    public void Sort_by_department_then_name() =>
        Assert.Equal(new[] { 5, 1, 2, 3 }, Data().ApplyFilters(null, null, null).ApplySort(ContactSortBy.Department).Select(c => c.Id).ToArray());

    [Fact]
    public void Page_skips_and_takes() =>
        Assert.Equal(new[] { 2, 1 }, Data().ApplyFilters(null, null, null).ApplySort(ContactSortBy.Name).Page(2, 2).Select(c => c.Id).ToArray());
}
```

Not: LINQ-to-objects `OrderBy` kültüre duyarlı varsayılan karşılaştırmayı kullanır ("İnsan" → "Satış"'tan önce); `null` departman en başa gelir ("5, 1, 2, 3"). SQL Server'da da `NULL` en başta sıralanır.

`Common/PagedResultTests.cs`:
```csharp
using StaffSync.Application.Features.Results.Common;

namespace StaffSync.Application.Tests.Common;

public class PagedResultTests
{
    [Theory]
    [InlineData(1, 30, 31, true)]
    [InlineData(2, 30, 60, false)]
    [InlineData(1, 30, 0, false)]
    public void HasMore_is_true_only_when_items_remain(int page, int size, int total, bool expected) =>
        Assert.Equal(expected, new PagedResult<int>(new List<int>(), total, page, size).HasMore);
}
```

- [ ] **Step 2: Testleri çalıştır**

Run: `dotnet test Tests/StaffSync.Application.Tests` → Expected: FAIL (tipler yok).

- [ ] **Step 3: Kodu yaz**

`ContactSortBy.cs`:
```csharp
namespace StaffSync.Application.Features.Queries.ContactQueries
{
    public enum ContactSortBy { Name, Department, JobTitle }
}
```

`ContactQueryBuilder.cs`:
```csharp
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Queries.ContactQueries
{
    // Saf sorgu kuralları: hem EF (SQL'e çevrilir) hem testlerdeki List.AsQueryable() ile çalışır.
    public static class ContactQueryBuilder
    {
        public static IQueryable<Contact> ApplyFilters(this IQueryable<Contact> query, string? search,
            IReadOnlyCollection<string>? departments, string? company)
        {
            query = query.Where(c => !c.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                // ?? "" null alanlarda LINQ-to-objects'in patlamasını önler; EF bunu COALESCE'e çevirir.
                query = query.Where(c =>
                    (c.DisplayName ?? "").Contains(s) ||
                    (c.JobTitle ?? "").Contains(s) ||
                    (c.Department ?? "").Contains(s) ||
                    (c.Email ?? "").Contains(s) ||
                    (c.TelephoneNumber ?? "").Contains(s) ||
                    (c.Home ?? "").Contains(s));
            }

            if (departments is { Count: > 0 })
                query = query.Where(c => departments.Contains(c.Department));

            if (!string.IsNullOrWhiteSpace(company))
                query = query.Where(c => c.Company == company);

            return query;
        }

        public static IOrderedQueryable<Contact> ApplySort(this IQueryable<Contact> query, ContactSortBy sortBy) => sortBy switch
        {
            ContactSortBy.Department => query.OrderBy(c => c.Department).ThenBy(c => c.DisplayName).ThenBy(c => c.Id),
            ContactSortBy.JobTitle => query.OrderBy(c => c.JobTitle).ThenBy(c => c.DisplayName).ThenBy(c => c.Id),
            _ => query.OrderBy(c => c.DisplayName).ThenBy(c => c.Id)
        };

        public static IQueryable<T> Page<T>(this IOrderedQueryable<T> query, int pageNumber, int pageSize)
            => query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
    }
}
```

Testteki sıralama beklentileri LINQ-to-objects'in varsayılan string karşılaştırmasına (kültüre duyarlı) dayanır; "Ahmet" < "Ayşe" < "Burak" her kültürde aynıdır. Test bu haliyle geçmezse beklenen diziyi değiştirme, `OrderBy`'a dokunma — önce farkı raporla.

`Features/Results/Common/PagedResult.cs`:
```csharp
namespace StaffSync.Application.Features.Results.Common
{
    public class PagedResult<T>
    {
        public PagedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }

        public IReadOnlyList<T> Items { get; }
        public int TotalCount { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        public bool HasMore => PageNumber * PageSize < TotalCount;
    }
}
```

`GetContactsWithPaginationQuery.cs` tamamı:
```csharp
using MediatR;
using StaffSync.Application.Features.Results.Common;
using StaffSync.Application.Features.Results.ContactResults;

namespace StaffSync.Application.Features.Queries.ContactQueries
{
    public class GetContactsWithPaginationQuery : IRequest<PagedResult<ContactDto>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 30;
        public string? Search { get; set; }
        public List<string>? Departments { get; set; }
        public string? Company { get; set; }
        public ContactSortBy SortBy { get; set; } = ContactSortBy.Name;
    }
}
```

`Validators/ContactValidators/GetContactsWithPaginationQueryValidator.cs`:
```csharp
using FluentValidation;
using StaffSync.Application.Features.Queries.ContactQueries;

namespace StaffSync.Application.Features.Validators.ContactValidators
{
    public class GetContactsWithPaginationQueryValidator : AbstractValidator<GetContactsWithPaginationQuery>
    {
        public GetContactsWithPaginationQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.Search).MaximumLength(100);
        }
    }
}
```

`GetContactsWithPaginationQueryHandler.cs` tamamı:
```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Mappings;
using StaffSync.Application.Features.Queries.ContactQueries;
using StaffSync.Application.Features.Results.Common;
using StaffSync.Application.Features.Results.ContactResults;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class GetContactsWithPaginationQueryHandler : BaseHandler, IRequestHandler<GetContactsWithPaginationQuery, PagedResult<ContactDto>>
    {
        public GetContactsWithPaginationQueryHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor) { }

        public async Task<PagedResult<ContactDto>> Handle(GetContactsWithPaginationQuery request, CancellationToken cancellationToken)
        {
            IQueryable<Contact> filtered = unitOfWork.GetReadRepository<Contact>().AsQueryable()
                .ApplyFilters(request.Search, request.Departments, request.Company);

            int total = await filtered.CountAsync(cancellationToken);
            List<ContactDto> items = await filtered
                .ApplySort(request.SortBy)
                .Page(request.PageNumber, request.PageSize)
                .Select(ContactMappings.AsDto)
                .ToListAsync(cancellationToken);

            return new PagedResult<ContactDto>(items, total, request.PageNumber, request.PageSize);
        }
    }
}
```

`Program.cs`: `builder.Services.AddControllers();` satırını şununla değiştir, en üste `using System.Text.Json.Serialization;` ekle:
```csharp
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
```

- [ ] **Step 4: Testleri çalıştır**

Run: `dotnet test Tests/StaffSync.Application.Tests` → Expected: PASS.
Run: `dotnet build StaffSync.sln -v q` → Expected: `0 Error(s)`.

- [ ] **Step 5: Commit**

```bash
git add -A .
git commit -m "Rehber sorgusu: genel arama, çoklu departman, sıralama, toplam sayı ve hasMore"
```

---

### Task 8: Filtre seçenekleri (departman/şirket + kişi sayısı)

**Files:**
- Create: `Core/StaffSync.Application/Features/Queries/ContactQueries/ContactFilterOptions.cs`
- Create: `Core/StaffSync.Application/Features/Queries/ContactQueries/GetContactFilterOptionsQuery.cs`
- Create: `Core/StaffSync.Application/Features/Results/ContactResults/ContactFilterOptionsResult.cs`
- Create: `Core/StaffSync.Application/Features/Handlers/ContactHandlers/GetContactFilterOptionsQueryHandler.cs`
- Modify: `Presentation/StaffSync.WebApi/Controllers/ContactsController.cs`
- Test: `Tests/StaffSync.Application.Tests/Contacts/ContactFilterOptionsTests.cs`

- [ ] **Step 1: Failing test**

```csharp
using StaffSync.Application.Features.Queries.ContactQueries;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Tests.Contacts;

public class ContactFilterOptionsTests
{
    private static IQueryable<Contact> Data() => new List<Contact>
    {
        new() { Id = 1, Department = "Satış", Company = "AUNDE" },
        new() { Id = 2, Department = "Satış", Company = "AUNDE" },
        new() { Id = 3, Department = "Bilgi Teknolojileri", Company = "Diğer AŞ" },
        new() { Id = 4, Department = "Üretim", Company = "AUNDE", IsDeleted = true },
        new() { Id = 5, Department = "", Company = null! },
    }.AsQueryable();

    [Fact]
    public void Departments_are_counted_without_deleted_or_empty()
    {
        var result = ContactFilterOptions.Departments(Data()).ToList();
        Assert.Equal(new[] { ("Bilgi Teknolojileri", 1), ("Satış", 2) }, result.Select(x => (x.Name, x.Count)).ToArray());
    }

    [Fact]
    public void Companies_are_counted()
    {
        var result = ContactFilterOptions.Companies(Data()).ToList();
        Assert.Equal(new[] { ("AUNDE", 2), ("Diğer AŞ", 1) }, result.Select(x => (x.Name, x.Count)).ToArray());
    }
}
```

- [ ] **Step 2: Çalıştır** → `dotnet test Tests/StaffSync.Application.Tests` → FAIL.

- [ ] **Step 3: Kod**

`Features/Results/ContactResults/ContactFilterOptionsResult.cs`:
```csharp
namespace StaffSync.Application.Features.Results.ContactResults
{
    public class FilterOptionDto
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    public class ContactFilterOptionsResult
    {
        public List<FilterOptionDto> Departments { get; set; } = new();
        public List<FilterOptionDto> Companies { get; set; } = new();
    }
}
```

`Features/Queries/ContactQueries/ContactFilterOptions.cs`:
```csharp
using StaffSync.Application.Features.Results.ContactResults;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Queries.ContactQueries
{
    public static class ContactFilterOptions
    {
        public static IQueryable<FilterOptionDto> Departments(IQueryable<Contact> contacts) =>
            contacts.Where(c => !c.IsDeleted && c.Department != null && c.Department != "")
                .GroupBy(c => c.Department)
                .Select(g => new FilterOptionDto { Name = g.Key, Count = g.Count() })
                .OrderBy(x => x.Name);

        public static IQueryable<FilterOptionDto> Companies(IQueryable<Contact> contacts) =>
            contacts.Where(c => !c.IsDeleted && c.Company != null && c.Company != "")
                .GroupBy(c => c.Company)
                .Select(g => new FilterOptionDto { Name = g.Key, Count = g.Count() })
                .OrderBy(x => x.Name);
    }
}
```

`GetContactFilterOptionsQuery.cs`:
```csharp
using MediatR;
using StaffSync.Application.Features.Results.ContactResults;

namespace StaffSync.Application.Features.Queries.ContactQueries
{
    public class GetContactFilterOptionsQuery : IRequest<ContactFilterOptionsResult> { }
}
```

`GetContactFilterOptionsQueryHandler.cs`:
```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Queries.ContactQueries;
using StaffSync.Application.Features.Results.ContactResults;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class GetContactFilterOptionsQueryHandler : BaseHandler, IRequestHandler<GetContactFilterOptionsQuery, ContactFilterOptionsResult>
    {
        public GetContactFilterOptionsQueryHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor) { }

        public async Task<ContactFilterOptionsResult> Handle(GetContactFilterOptionsQuery request, CancellationToken cancellationToken)
        {
            IQueryable<Contact> contacts = unitOfWork.GetReadRepository<Contact>().AsQueryable();
            return new ContactFilterOptionsResult
            {
                Departments = await ContactFilterOptions.Departments(contacts).ToListAsync(cancellationToken),
                Companies = await ContactFilterOptions.Companies(contacts).ToListAsync(cancellationToken)
            };
        }
    }
}
```

`ContactsController.cs` — sınıfa ekle:
```csharp
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> FilterOptions()
            => Ok(await _mediator.Send(new GetContactFilterOptionsQuery()));
```

- [ ] **Step 4: Test + build** → PASS, `0 Error(s)`.

- [ ] **Step 5: Commit**

```bash
git add -A .
git commit -m "Filtre seçenekleri ucu: departman ve şirket listesi kişi sayılarıyla"
```

---

### Task 9: Değişiklik takibi ucu (mobil senkron için)

**Files:**
- Create: `Core/StaffSync.Application/Features/Queries/ContactQueries/ContactChanges.cs`
- Create: `Core/StaffSync.Application/Features/Queries/ContactQueries/GetContactChangesQuery.cs`
- Create: `Core/StaffSync.Application/Features/Results/ContactResults/ContactChangesResult.cs`
- Create: `Core/StaffSync.Application/Features/Handlers/ContactHandlers/GetContactChangesQueryHandler.cs`
- Modify: `Presentation/StaffSync.WebApi/Controllers/ContactsController.cs`
- Test: `Tests/StaffSync.Application.Tests/Contacts/ContactChangesTests.cs`

- [ ] **Step 1: Failing test**

```csharp
using StaffSync.Application.Features.Queries.ContactQueries;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Tests.Contacts;

public class ContactChangesTests
{
    private static readonly DateTime T0 = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    private static IQueryable<Contact> Data() => new List<Contact>
    {
        new() { Id = 1, DisplayName = "Eski", UpdatedDate = T0.AddDays(-1) },
        new() { Id = 2, DisplayName = "Yeni", UpdatedDate = T0.AddHours(1) },
        new() { Id = 3, DisplayName = "Eskiden silinmiş", IsDeleted = true, UpdatedDate = T0.AddDays(-1) },
        new() { Id = 4, DisplayName = "Yeni silinmiş", IsDeleted = true, UpdatedDate = T0.AddHours(2) },
    }.AsQueryable();

    [Fact]
    public void Without_since_returns_all_active_and_no_deletions()
    {
        Assert.Equal(new[] { 1, 2 }, ContactChanges.Upserted(Data(), null).Select(c => c.Id).OrderBy(x => x).ToArray());
        Assert.Empty(ContactChanges.DeletedIds(Data(), null).ToList());
    }

    [Fact]
    public void With_since_returns_only_changes_after_it()
    {
        Assert.Equal(new[] { 2 }, ContactChanges.Upserted(Data(), T0).Select(c => c.Id).ToArray());
        Assert.Equal(new[] { 4 }, ContactChanges.DeletedIds(Data(), T0).ToArray());
    }
}
```

- [ ] **Step 2: Çalıştır** → FAIL.

- [ ] **Step 3: Kod**

`ContactChanges.cs`:
```csharp
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Queries.ContactQueries
{
    public static class ContactChanges
    {
        public static IQueryable<Contact> Upserted(IQueryable<Contact> contacts, DateTime? sinceUtc) =>
            sinceUtc is null
                ? contacts.Where(c => !c.IsDeleted)
                : contacts.Where(c => !c.IsDeleted && c.UpdatedDate > sinceUtc);

        // since yoksa istemci tam liste alır, silinen listesine ihtiyacı yoktur.
        public static IQueryable<int> DeletedIds(IQueryable<Contact> contacts, DateTime? sinceUtc) =>
            sinceUtc is null
                ? Enumerable.Empty<int>().AsQueryable()
                : contacts.Where(c => c.IsDeleted && c.UpdatedDate > sinceUtc).Select(c => c.Id);
    }
}
```

`Features/Results/ContactResults/ContactChangesResult.cs`:
```csharp
namespace StaffSync.Application.Features.Results.ContactResults
{
    public class ContactChangesResult
    {
        // İstemci bir sonraki istekte since olarak bunu gönderir.
        public DateTime ServerTimeUtc { get; set; }
        public List<ContactDto> Upserted { get; set; } = new();
        public List<int> DeletedIds { get; set; } = new();
    }
}
```

`GetContactChangesQuery.cs`:
```csharp
using MediatR;
using StaffSync.Application.Features.Results.ContactResults;

namespace StaffSync.Application.Features.Queries.ContactQueries
{
    public class GetContactChangesQuery : IRequest<ContactChangesResult>
    {
        public DateTime? Since { get; set; }
    }
}
```

`GetContactChangesQueryHandler.cs`:
```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Mappings;
using StaffSync.Application.Features.Queries.ContactQueries;
using StaffSync.Application.Features.Results.ContactResults;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.ContactHandlers
{
    public class GetContactChangesQueryHandler : BaseHandler, IRequestHandler<GetContactChangesQuery, ContactChangesResult>
    {
        public GetContactChangesQueryHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor) { }

        public async Task<ContactChangesResult> Handle(GetContactChangesQuery request, CancellationToken cancellationToken)
        {
            // Sorgudan ÖNCE alınır: sorgu sırasında olan değişiklik bir sonraki senkronda kaçmaz.
            DateTime serverTime = DateTime.UtcNow;
            DateTime? since = request.Since?.ToUniversalTime();
            IQueryable<Contact> contacts = unitOfWork.GetReadRepository<Contact>().AsQueryable();

            return new ContactChangesResult
            {
                ServerTimeUtc = serverTime,
                Upserted = await ContactChanges.Upserted(contacts, since).Select(ContactMappings.AsDto).ToListAsync(cancellationToken),
                DeletedIds = since is null ? new() : await ContactChanges.DeletedIds(contacts, since).ToListAsync(cancellationToken)
            };
        }
    }
}
```

`ContactsController.cs` — ekle:
```csharp
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Changes([FromQuery] DateTime? since)
            => Ok(await _mediator.Send(new GetContactChangesQuery { Since = since }));
```

- [ ] **Step 4: Test + build** → PASS, `0 Error(s)`.

- [ ] **Step 5: Commit**

```bash
git add -A .
git commit -m "Senkron için değişiklik ucu: since sonrası eklenen/güncellenen/silinen kişiler"
```

---

### Task 10: Hata bildirimi entity'leri ve migration v8

**Files:**
- Create: `Core/StaffSync.Domain/Entities/CorrectionStatus.cs`, `ContactCorrection.cs`, `ContactCorrectionItem.cs`
- Create: `Infrastructure/StaffSync.Persistence/Configurations/ContactCorrectionConfiguration.cs`
- Modify: `Infrastructure/StaffSync.Persistence/Context/StaffSyncContext.cs`

- [ ] **Step 1: Entity'ler**

`CorrectionStatus.cs`:
```csharp
namespace StaffSync.Domain.Entities
{
    public enum CorrectionStatus { New = 0, Applied = 1, Rejected = 2 }
}
```

`ContactCorrection.cs`:
```csharp
using StaffSync.Domain.Common;

namespace StaffSync.Domain.Entities
{
    public class ContactCorrection : EntityBase, IEntityBase
    {
        public int ContactId { get; set; }
        public Contact? Contact { get; set; }
        public Guid ReportedByUserId { get; set; }
        public string? Note { get; set; }
        public CorrectionStatus Status { get; set; } = CorrectionStatus.New;
        public List<ContactCorrectionItem> Items { get; set; } = new();
    }
}
```

`ContactCorrectionItem.cs`:
```csharp
using StaffSync.Domain.Common;

namespace StaffSync.Domain.Entities
{
    public class ContactCorrectionItem : EntityBase, IEntityBase
    {
        public int ContactCorrectionId { get; set; }
        public string FieldName { get; set; } = "";
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
    }
}
```

`ContactCorrectionConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StaffSync.Domain.Entities;

namespace StaffSync.Persistence.Configurations
{
    public class ContactCorrectionConfiguration : IEntityTypeConfiguration<ContactCorrection>
    {
        public void Configure(EntityTypeBuilder<ContactCorrection> builder)
        {
            builder.Property(x => x.Note).HasMaxLength(500);
            builder.HasOne(x => x.Contact).WithMany().HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.ContactCorrectionId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => new { x.ReportedByUserId, x.CreatedDate });
            builder.HasIndex(x => x.Status);
        }
    }

    public class ContactCorrectionItemConfiguration : IEntityTypeConfiguration<ContactCorrectionItem>
    {
        public void Configure(EntityTypeBuilder<ContactCorrectionItem> builder)
        {
            builder.Property(x => x.FieldName).HasMaxLength(50);
            builder.Property(x => x.OldValue).HasMaxLength(200);
            builder.Property(x => x.NewValue).HasMaxLength(200);
        }
    }
}
```

`StaffSyncContext.cs` — DbSet'leri ekle:
```csharp
        public DbSet<ContactCorrection> ContactCorrections { get; set; }
        public DbSet<ContactCorrectionItem> ContactCorrectionItems { get; set; }
```

- [ ] **Step 2: Migration**

```bash
dotnet ef migrations add v8_ContactCorrections --project Infrastructure/StaffSync.Persistence --startup-project Presentation/StaffSync.WebApi
```

Kontrol: `Up` içinde sadece `ContactCorrections` ve `ContactCorrectionItems` tabloları, FK'ları ve index'leri olmalı. Başka bir şey varsa dur, kullanıcıya göster.

- [ ] **Step 3: Build** → `0 Error(s)`.

- [ ] **Step 4: Commit**

```bash
git add -A Core/StaffSync.Domain Infrastructure/StaffSync.Persistence
git commit -m "Hatalı bilgi bildirimi tabloları (migration v8)"
```

---

### Task 11: Hata bildirimi gönderme — alanlar, kurallar, e-posta

**Files:**
- Create: `Core/StaffSync.Application/Features/Corrections/ContactFields.cs`
- Create: `Core/StaffSync.Application/Features/Corrections/CorrectionEmailBuilder.cs`
- Create: `Core/StaffSync.Application/Features/Corrections/CorrectionSettings.cs`
- Create: `Core/StaffSync.Application/Features/Commands/CorrectionCommands/CreateContactCorrectionCommand.cs`
- Create: `Core/StaffSync.Application/Features/Validators/CorrectionValidators/CreateContactCorrectionCommandValidator.cs`
- Create: `Core/StaffSync.Application/Features/Rules/CorrectionRules/CorrectionRules.cs`
- Create: `Core/StaffSync.Application/Features/Exceptions/CorrectionExceptions/CorrectionValueUnchangedException.cs`, `TooManyCorrectionsException.cs`
- Create: `Core/StaffSync.Application/Features/Handlers/CorrectionHandlers/CreateContactCorrectionCommandHandler.cs`
- Create: `Core/StaffSync.Application/Interfaces/Email/IEmailSender.cs`
- Create: `Infrastructure/StaffSync.Infrastructure/Email/SmtpSettings.cs`, `SmtpEmailSender.cs`
- Modify: `Infrastructure/StaffSync.Infrastructure/Registration.cs`
- Modify: `Presentation/StaffSync.WebApi/appsettings.json`
- Modify: `Presentation/StaffSync.WebApi/Controllers/ContactsController.cs`
- Test: `Tests/StaffSync.Application.Tests/Corrections/ContactFieldsTests.cs`, `CorrectionRulesTests.cs`, `CorrectionEmailBuilderTests.cs`

- [ ] **Step 1: Failing testler**

`Corrections/ContactFieldsTests.cs`:
```csharp
using StaffSync.Application.Features.Corrections;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Tests.Corrections;

public class ContactFieldsTests
{
    private static readonly Contact C = new() { TelephoneNumber = "+905320000000", Home = "2145", JobTitle = null! };

    [Fact]
    public void Known_fields_are_accepted_unknown_rejected()
    {
        Assert.True(ContactFields.IsKnown("TelephoneNumber"));
        Assert.True(ContactFields.IsKnown(ContactFields.Other));
        Assert.False(ContactFields.IsKnown("PasswordHash"));
    }

    [Fact]
    public void Current_value_reads_contact_and_treats_null_as_empty()
    {
        Assert.Equal("+905320000000", ContactFields.CurrentValue(C, "TelephoneNumber"));
        Assert.Equal("", ContactFields.CurrentValue(C, "JobTitle"));
        Assert.Equal("", ContactFields.CurrentValue(C, ContactFields.Other));
    }

    [Fact]
    public void Labels_are_turkish() => Assert.Equal("Kısa kod", ContactFields.Label("Home"));
}
```

`Corrections/CorrectionRulesTests.cs`:
```csharp
using StaffSync.Application.Features.Commands.CorrectionCommands;
using StaffSync.Application.Features.Exceptions.ContactExceptions;
using StaffSync.Application.Features.Exceptions.CorrectionExceptions;
using StaffSync.Application.Features.Rules.CorrectionRules;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Tests.Corrections;

public class CorrectionRulesTests
{
    private readonly CorrectionRules rules = new();
    private static readonly Contact C = new() { Id = 1, TelephoneNumber = "111", Department = "Satış" };

    [Fact]
    public async Task Missing_contact_throws_not_found() =>
        await Assert.ThrowsAsync<ContactNotFoundException>(() => rules.ContactMustExist(null));

    [Fact]
    public async Task Same_value_is_rejected() =>
        await Assert.ThrowsAsync<CorrectionValueUnchangedException>(() => rules.ValuesMustChange(C,
            new[] { new CorrectionItemInput { FieldName = "TelephoneNumber", NewValue = " 111 " } }));

    [Fact]
    public async Task Different_value_and_other_are_accepted() =>
        await rules.ValuesMustChange(C, new[]
        {
            new CorrectionItemInput { FieldName = "TelephoneNumber", NewValue = "222" },
            new CorrectionItemInput { FieldName = "Other", NewValue = "Ofisi taşındı" }
        });

    [Theory]
    [InlineData(4, false)]
    [InlineData(5, true)]
    public async Task Hourly_limit_is_five(int countInLastHour, bool shouldThrow)
    {
        Task act() => rules.MustNotExceedHourlyLimit(countInLastHour);
        if (shouldThrow) await Assert.ThrowsAsync<TooManyCorrectionsException>(act);
        else await act();
    }
}
```

`Corrections/CorrectionEmailBuilderTests.cs`:
```csharp
using StaffSync.Application.Features.Corrections;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Tests.Corrections;

public class CorrectionEmailBuilderTests
{
    [Fact]
    public void Email_lists_old_and_new_values_and_note()
    {
        var contact = new Contact { Id = 12, DisplayName = "Ahmet Yılmaz" };
        var items = new[]
        {
            new ContactCorrectionItem { FieldName = "TelephoneNumber", OldValue = "111", NewValue = "222" },
            new ContactCorrectionItem { FieldName = "Office", OldValue = "", NewValue = "Fabrika" }
        };

        var (subject, body) = CorrectionEmailBuilder.Build(contact, "Ayşe Kaya", "ayse@x.com", items, "Ocakta taşındı");

        Assert.Equal("StaffSync düzeltme önerisi: Ahmet Yılmaz", subject);
        Assert.Contains("Ayşe Kaya (ayse@x.com)", body);
        Assert.Contains("Ahmet Yılmaz (#12)", body);
        Assert.Contains("- Cep telefonu: 111 → 222", body);
        Assert.Contains("- Ofis: (boş) → Fabrika", body);
        Assert.Contains("Not: Ocakta taşındı", body);
    }

    [Fact]
    public void Note_line_is_omitted_when_empty()
    {
        var (_, body) = CorrectionEmailBuilder.Build(new Contact { DisplayName = "X" }, "A", "a@x", Array.Empty<ContactCorrectionItem>(), null);
        Assert.DoesNotContain("Not:", body);
    }
}
```

- [ ] **Step 2: Çalıştır** → FAIL.

- [ ] **Step 3: Saf sınıflar**

`Features/Corrections/ContactFields.cs`:
```csharp
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Corrections
{
    // Bildirimde seçilebilen alanlar. Mobil uygulama FieldName olarak bu anahtarları gönderir.
    public static class ContactFields
    {
        public const string Other = "Other";

        private static readonly Dictionary<string, (string Label, Func<Contact, string?> Get)> fields = new(StringComparer.Ordinal)
        {
            ["DisplayName"] = ("Ad soyad", c => c.DisplayName),
            ["TelephoneNumber"] = ("Cep telefonu", c => c.TelephoneNumber),
            ["Home"] = ("Kısa kod", c => c.Home),
            ["Email"] = ("E-posta", c => c.Email),
            ["JobTitle"] = ("Unvan", c => c.JobTitle),
            ["Department"] = ("Departman", c => c.Department),
            ["Office"] = ("Ofis", c => c.Office),
            ["Company"] = ("Şirket", c => c.Company),
            ["ManagerName"] = ("Yönetici", c => c.ManagerName),
            [Other] = ("Diğer", _ => "")
        };

        public static bool IsKnown(string fieldName) => fields.ContainsKey(fieldName);
        public static string Label(string fieldName) => fields[fieldName].Label;
        public static string CurrentValue(Contact contact, string fieldName) => fields[fieldName].Get(contact) ?? "";
    }
}
```

`Features/Corrections/CorrectionEmailBuilder.cs`:
```csharp
using StaffSync.Domain.Entities;
using System.Text;

namespace StaffSync.Application.Features.Corrections
{
    public static class CorrectionEmailBuilder
    {
        public static (string Subject, string Body) Build(Contact contact, string reporterName, string reporterEmail,
            IEnumerable<ContactCorrectionItem> items, string? note)
        {
            var body = new StringBuilder()
                .AppendLine($"{reporterName} ({reporterEmail}) şu kişi için düzeltme önerdi: {contact.DisplayName} (#{contact.Id})")
                .AppendLine();

            foreach (var item in items)
            {
                string oldValue = string.IsNullOrEmpty(item.OldValue) ? "(boş)" : item.OldValue;
                body.AppendLine($"- {ContactFields.Label(item.FieldName)}: {oldValue} → {item.NewValue}");
            }

            if (!string.IsNullOrWhiteSpace(note))
                body.AppendLine().AppendLine($"Not: {note}");

            body.AppendLine().AppendLine("Kişiyi güncellemek için StaffSync yönetim uçlarını kullanın.");
            return ($"StaffSync düzeltme önerisi: {contact.DisplayName}", body.ToString());
        }
    }
}
```

`Features/Corrections/CorrectionSettings.cs`:
```csharp
namespace StaffSync.Application.Features.Corrections
{
    // appsettings: "Corrections": { "NotifyEmails": [ "ik@...", "bt@..." ] }
    public class CorrectionSettings
    {
        public List<string> NotifyEmails { get; set; } = new();
    }
}
```

`Features/Commands/CorrectionCommands/CreateContactCorrectionCommand.cs`:
```csharp
using MediatR;
using System.Text.Json.Serialization;

namespace StaffSync.Application.Features.Commands.CorrectionCommands
{
    public class CreateContactCorrectionCommand : IRequest<Unit>
    {
        [JsonIgnore] // Route'tan gelir, gövdeden alınmaz.
        public int ContactId { get; set; }
        public string? Note { get; set; }
        public List<CorrectionItemInput> Items { get; set; } = new();
    }

    public class CorrectionItemInput
    {
        public string FieldName { get; set; } = "";
        public string NewValue { get; set; } = "";
    }
}
```

Exception'lar — `Features/Exceptions/CorrectionExceptions/CorrectionValueUnchangedException.cs`:
```csharp
using StaffSync.Application.Bases;

namespace StaffSync.Application.Features.Exceptions.CorrectionExceptions
{
    public class CorrectionValueUnchangedException : BaseException
    {
        public CorrectionValueUnchangedException(string label) : base($"{label} için yazdığın değer mevcut değerle aynı.") { }
    }
}
```

`TooManyCorrectionsException.cs`:
```csharp
using StaffSync.Application.Bases;

namespace StaffSync.Application.Features.Exceptions.CorrectionExceptions
{
    public class TooManyCorrectionsException : BaseException
    {
        public TooManyCorrectionsException() : base("Son bir saatte çok fazla bildirim gönderdin, biraz sonra tekrar dene.") { }
    }
}
```

`Features/Rules/CorrectionRules/CorrectionRules.cs`:
```csharp
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.CorrectionCommands;
using StaffSync.Application.Features.Corrections;
using StaffSync.Application.Features.Exceptions.ContactExceptions;
using StaffSync.Application.Features.Exceptions.CorrectionExceptions;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Rules.CorrectionRules
{
    public class CorrectionRules : BaseRules
    {
        public const int HourlyLimit = 5;

        public Task ContactMustExist(Contact? contact)
        {
            if (contact is null) throw new ContactNotFoundException();
            return Task.CompletedTask;
        }

        public Task ValuesMustChange(Contact contact, IEnumerable<CorrectionItemInput> items)
        {
            foreach (var item in items.Where(i => i.FieldName != ContactFields.Other))
                if (item.NewValue.Trim() == ContactFields.CurrentValue(contact, item.FieldName).Trim())
                    throw new CorrectionValueUnchangedException(ContactFields.Label(item.FieldName));
            return Task.CompletedTask;
        }

        public Task MustNotExceedHourlyLimit(int countInLastHour)
        {
            if (countInLastHour >= HourlyLimit) throw new TooManyCorrectionsException();
            return Task.CompletedTask;
        }
    }
}
```

`Features/Validators/CorrectionValidators/CreateContactCorrectionCommandValidator.cs`:
```csharp
using FluentValidation;
using StaffSync.Application.Features.Commands.CorrectionCommands;
using StaffSync.Application.Features.Corrections;

namespace StaffSync.Application.Features.Validators.CorrectionValidators
{
    public class CreateContactCorrectionCommandValidator : AbstractValidator<CreateContactCorrectionCommand>
    {
        public CreateContactCorrectionCommandValidator()
        {
            RuleFor(x => x.Items).NotEmpty().WithMessage("En az bir alan seçmelisin.");
            RuleFor(x => x.Items).Must(items => items.Select(i => i.FieldName).Distinct().Count() == items.Count)
                .WithMessage("Aynı alan birden fazla seçilemez.");
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.FieldName).Must(ContactFields.IsKnown).WithMessage("Geçersiz alan.");
                item.RuleFor(i => i.NewValue).NotEmpty().WithMessage("Doğru değeri yazmalısın.").MaximumLength(200);
            });
            RuleFor(x => x.Note).MaximumLength(500);
        }
    }
}
```

- [ ] **Step 4: Testleri çalıştır** → PASS.

- [ ] **Step 5: E-posta arayüzü ve SMTP gönderici**

`Interfaces/Email/IEmailSender.cs`:
```csharp
namespace StaffSync.Application.Interfaces.Email
{
    public interface IEmailSender
    {
        Task SendAsync(IEnumerable<string> to, string subject, string body, CancellationToken cancellationToken = default);
    }
}
```

`Infrastructure/StaffSync.Infrastructure/Email/SmtpSettings.cs`:
```csharp
namespace StaffSync.Infrastructure.Email
{
    // appsettings "Smtp" bölümü. Host boşsa e-posta gönderilmez (bildirim yine kaydedilir).
    public class SmtpSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 25;
        public bool EnableSsl { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string From { get; set; } = "";
    }
}
```

`Infrastructure/StaffSync.Infrastructure/Email/SmtpEmailSender.cs`:
```csharp
using Microsoft.Extensions.Options;
using StaffSync.Application.Interfaces.Email;
using System.Net;
using System.Net.Mail;

namespace StaffSync.Infrastructure.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpSettings settings;

        public SmtpEmailSender(IOptions<SmtpSettings> options) => settings = options.Value;

        public async Task SendAsync(IEnumerable<string> to, string subject, string body, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(settings.Host))
                throw new InvalidOperationException("Smtp:Host ayarı boş; e-posta gönderilemez.");

            using var message = new MailMessage { From = new MailAddress(settings.From), Subject = subject, Body = body };
            foreach (string address in to) message.To.Add(address);

            using var client = new SmtpClient(settings.Host, settings.Port) { EnableSsl = settings.EnableSsl };
            if (!string.IsNullOrEmpty(settings.UserName))
                client.Credentials = new NetworkCredential(settings.UserName, settings.Password);

            await client.SendMailAsync(message, cancellationToken);
        }
    }
}
```

`Infrastructure/StaffSync.Infrastructure/Registration.cs` — `AddInfrastructure` içine (mevcut satırların altına), dosyanın başına `using StaffSync.Application.Features.Corrections;`, `using StaffSync.Application.Interfaces.Email;`, `using StaffSync.Infrastructure.Email;` ekle:
```csharp
            services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));
            services.Configure<CorrectionSettings>(configuration.GetSection("Corrections"));
            services.AddTransient<IEmailSender, SmtpEmailSender>();
```

`appsettings.json` — kök nesneye ekle (değerleri kullanıcı dolduracak; boşken e-posta atlanır):
```json
  "Smtp": { "Host": "", "Port": 25, "EnableSsl": false, "UserName": "", "Password": "", "From": "" },
  "Corrections": { "NotifyEmails": [] },
```

- [ ] **Step 6: Handler ve uç**

`Features/Handlers/CorrectionHandlers/CreateContactCorrectionCommandHandler.cs`:
```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.CorrectionCommands;
using StaffSync.Application.Features.Corrections;
using StaffSync.Application.Features.Rules.CorrectionRules;
using StaffSync.Application.Interfaces.Email;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.CorrectionHandlers
{
    public class CreateContactCorrectionCommandHandler : BaseHandler, IRequestHandler<CreateContactCorrectionCommand, Unit>
    {
        private readonly CorrectionRules rules;
        private readonly UserManager<User> userManager;
        private readonly IEmailSender emailSender;
        private readonly CorrectionSettings settings;
        private readonly ILogger<CreateContactCorrectionCommandHandler> logger;

        public CreateContactCorrectionCommandHandler(CorrectionRules rules, UserManager<User> userManager, IEmailSender emailSender,
            IOptions<CorrectionSettings> settings, ILogger<CreateContactCorrectionCommandHandler> logger,
            IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, httpContextAccessor)
        {
            this.rules = rules;
            this.userManager = userManager;
            this.emailSender = emailSender;
            this.settings = settings.Value;
            this.logger = logger;
        }

        public async Task<Unit> Handle(CreateContactCorrectionCommand request, CancellationToken cancellationToken)
        {
            Contact? contact = await unitOfWork.GetReadRepository<Contact>().GetAsync(x => x.Id == request.ContactId && !x.IsDeleted);
            await rules.ContactMustExist(contact);

            Guid reporterId = Guid.Parse(userId!); // [Authorize] uçta olduğu için dolu
            DateTime oneHourAgo = DateTime.Now.AddHours(-1); // CreatedDate yerel saatle tutuluyor (EntityBase)
            int recent = await unitOfWork.GetReadRepository<ContactCorrection>()
                .CountAsync(x => x.ReportedByUserId == reporterId && x.CreatedDate >= oneHourAgo);
            await rules.MustNotExceedHourlyLimit(recent);
            await rules.ValuesMustChange(contact!, request.Items);

            var correction = new ContactCorrection
            {
                ContactId = contact!.Id,
                ReportedByUserId = reporterId,
                Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                Items = request.Items.Select(i => new ContactCorrectionItem
                {
                    FieldName = i.FieldName,
                    OldValue = ContactFields.CurrentValue(contact, i.FieldName),
                    NewValue = i.NewValue.Trim()
                }).ToList()
            };
            await unitOfWork.GetWriteRepository<ContactCorrection>().AddAysnc(correction);
            await unitOfWork.SaveAsync();

            await NotifyAsync(contact, correction, cancellationToken);
            return Unit.Value;
        }

        // E-posta hatası bildirimi geri almaz: kayıt zaten DB'de, admin listeden görür.
        private async Task NotifyAsync(Contact contact, ContactCorrection correction, CancellationToken cancellationToken)
        {
            if (settings.NotifyEmails.Count == 0) return;
            try
            {
                User? reporter = await userManager.FindByIdAsync(correction.ReportedByUserId.ToString());
                var (subject, body) = CorrectionEmailBuilder.Build(contact, reporter?.FullName ?? "Bilinmeyen kullanıcı",
                    reporter?.Email ?? "", correction.Items, correction.Note);
                await emailSender.SendAsync(settings.NotifyEmails, subject, body, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Düzeltme bildirimi {CorrectionId} için e-posta gönderilemedi", correction.Id);
            }
        }
    }
}
```

`ContactsController.cs` — ekle (`using StaffSync.Application.Features.Commands.CorrectionCommands;`):
```csharp
        [HttpPost("{id}")]
        [Authorize]
        public async Task<IActionResult> ReportCorrection(int id, CreateContactCorrectionCommand command)
        {
            command.ContactId = id;
            await _mediator.Send(command);
            return StatusCode(StatusCodes.Status201Created);
        }
```

- [ ] **Step 7: Test + build** → PASS, `0 Error(s)`.

- [ ] **Step 8: Commit**

```bash
git add -A .
git commit -m "Hatalı bilgi bildirimi: kayıt, kurallar ve İK/BT'ye e-posta"
```

---

### Task 12: Admin için bildirim listesi ve durum güncelleme

**Files:**
- Create: `Core/StaffSync.Application/Features/Results/CorrectionResults/CorrectionDto.cs`
- Create: `Core/StaffSync.Application/Features/Mappings/CorrectionMappings.cs`
- Create: `Core/StaffSync.Application/Features/Queries/CorrectionQueries/GetCorrectionsQuery.cs`
- Create: `Core/StaffSync.Application/Features/Commands/CorrectionCommands/UpdateCorrectionStatusCommand.cs`
- Create: `Core/StaffSync.Application/Features/Exceptions/CorrectionExceptions/CorrectionNotFoundException.cs`
- Create: `Core/StaffSync.Application/Features/Handlers/CorrectionHandlers/GetCorrectionsQueryHandler.cs`, `UpdateCorrectionStatusCommandHandler.cs`
- Modify: `Core/StaffSync.Application/Features/Rules/CorrectionRules/CorrectionRules.cs`
- Create: `Presentation/StaffSync.WebApi/Controllers/CorrectionsController.cs`
- Test: `Tests/StaffSync.Application.Tests/Corrections/CorrectionMappingsTests.cs`

- [ ] **Step 1: Failing test**

```csharp
using StaffSync.Application.Features.Mappings;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Tests.Corrections;

public class CorrectionMappingsTests
{
    [Fact]
    public void ToDto_includes_contact_name_and_labelled_items()
    {
        var correction = new ContactCorrection
        {
            Id = 3, ContactId = 12, Contact = new Contact { DisplayName = "Ahmet Yılmaz" }, Note = "n", Status = CorrectionStatus.New,
            Items = { new ContactCorrectionItem { FieldName = "Home", OldValue = "2145", NewValue = "2146" } }
        };

        var dto = correction.ToDto();

        Assert.Equal("Ahmet Yılmaz", dto.ContactName);
        Assert.Equal(CorrectionStatus.New, dto.Status);
        Assert.Equal("Kısa kod", dto.Items.Single().Label);
        Assert.Equal("2146", dto.Items.Single().NewValue);
    }
}
```

- [ ] **Step 2: Çalıştır** → FAIL.

- [ ] **Step 3: Kod**

`Features/Results/CorrectionResults/CorrectionDto.cs`:
```csharp
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Results.CorrectionResults
{
    public class CorrectionDto
    {
        public int Id { get; set; }
        public int ContactId { get; set; }
        public string ContactName { get; set; } = "";
        public Guid ReportedByUserId { get; set; }
        public string? Note { get; set; }
        public CorrectionStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<CorrectionItemDto> Items { get; set; } = new();
    }

    public class CorrectionItemDto
    {
        public string FieldName { get; set; } = "";
        public string Label { get; set; } = "";
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
    }
}
```

`Features/Mappings/CorrectionMappings.cs`:
```csharp
using StaffSync.Application.Features.Corrections;
using StaffSync.Application.Features.Results.CorrectionResults;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Mappings
{
    public static class CorrectionMappings
    {
        public static CorrectionDto ToDto(this ContactCorrection c) => new()
        {
            Id = c.Id,
            ContactId = c.ContactId,
            ContactName = c.Contact?.DisplayName ?? "",
            ReportedByUserId = c.ReportedByUserId,
            Note = c.Note,
            Status = c.Status,
            CreatedDate = c.CreatedDate,
            Items = c.Items.Select(i => new CorrectionItemDto
            {
                FieldName = i.FieldName,
                Label = ContactFields.Label(i.FieldName),
                OldValue = i.OldValue,
                NewValue = i.NewValue
            }).ToList()
        };
    }
}
```

`Features/Queries/CorrectionQueries/GetCorrectionsQuery.cs`:
```csharp
using MediatR;
using StaffSync.Application.Features.Results.CorrectionResults;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Queries.CorrectionQueries
{
    public class GetCorrectionsQuery : IRequest<List<CorrectionDto>>
    {
        public CorrectionStatus? Status { get; set; }
    }
}
```

`Features/Commands/CorrectionCommands/UpdateCorrectionStatusCommand.cs`:
```csharp
using MediatR;
using StaffSync.Domain.Entities;
using System.Text.Json.Serialization;

namespace StaffSync.Application.Features.Commands.CorrectionCommands
{
    public class UpdateCorrectionStatusCommand : IRequest
    {
        [JsonIgnore]
        public int Id { get; set; }
        public CorrectionStatus Status { get; set; }
    }
}
```

`Features/Exceptions/CorrectionExceptions/CorrectionNotFoundException.cs`:
```csharp
using StaffSync.Application.Bases;

namespace StaffSync.Application.Features.Exceptions.CorrectionExceptions
{
    public class CorrectionNotFoundException : NotFoundException
    {
        public CorrectionNotFoundException() : base("Bildirim bulunamadı.") { }
    }
}
```

`CorrectionRules.cs` — sınıfa ekle:
```csharp
        public Task CorrectionMustExist(ContactCorrection? correction)
        {
            if (correction is null) throw new CorrectionNotFoundException();
            return Task.CompletedTask;
        }
```

`Features/Handlers/CorrectionHandlers/GetCorrectionsQueryHandler.cs`:
```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Mappings;
using StaffSync.Application.Features.Queries.CorrectionQueries;
using StaffSync.Application.Features.Results.CorrectionResults;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.CorrectionHandlers
{
    public class GetCorrectionsQueryHandler : BaseHandler, IRequestHandler<GetCorrectionsQuery, List<CorrectionDto>>
    {
        public GetCorrectionsQueryHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor) { }

        public async Task<List<CorrectionDto>> Handle(GetCorrectionsQuery request, CancellationToken cancellationToken)
        {
            IList<ContactCorrection> list = await unitOfWork.GetReadRepository<ContactCorrection>().GetAllAsync(
                predicate: request.Status is null ? null : x => x.Status == request.Status,
                include: q => q.Include(x => x.Items).Include(x => x.Contact!),
                orderby: q => q.OrderByDescending(x => x.CreatedDate));

            return list.Select(c => c.ToDto()).ToList();
        }
    }
}
```

`Features/Handlers/CorrectionHandlers/UpdateCorrectionStatusCommandHandler.cs`:
```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.CorrectionCommands;
using StaffSync.Application.Features.Rules.CorrectionRules;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.CorrectionHandlers
{
    public class UpdateCorrectionStatusCommandHandler : BaseHandler, IRequestHandler<UpdateCorrectionStatusCommand>
    {
        private readonly CorrectionRules rules;

        public UpdateCorrectionStatusCommandHandler(CorrectionRules rules, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {
            this.rules = rules;
        }

        public async Task Handle(UpdateCorrectionStatusCommand request, CancellationToken cancellationToken)
        {
            ContactCorrection? correction = await unitOfWork.GetReadRepository<ContactCorrection>()
                .GetAsync(x => x.Id == request.Id, enableTracking: true);
            await rules.CorrectionMustExist(correction);

            correction!.Status = request.Status;
            await unitOfWork.SaveAsync();
        }
    }
}
```

`Presentation/StaffSync.WebApi/Controllers/CorrectionsController.cs`:
```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StaffSync.Application.Features.Commands.CorrectionCommands;
using StaffSync.Application.Features.Queries.CorrectionQueries;
using StaffSync.Domain.Entities;

namespace StaffSync.WebApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class CorrectionsController : ControllerBase
    {
        private readonly IMediator mediator;

        public CorrectionsController(IMediator mediator) => this.mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] CorrectionStatus? status)
            => Ok(await mediator.Send(new GetCorrectionsQuery { Status = status }));

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateCorrectionStatusCommand command)
        {
            command.Id = id;
            await mediator.Send(command);
            return Ok();
        }
    }
}
```

- [ ] **Step 4: Test + build** → PASS, `0 Error(s)`.

- [ ] **Step 5: Commit**

```bash
git add -A .
git commit -m "Admin için hata bildirimi listesi ve durum güncelleme"
```

---

### Task 13: vCard 2.1 ve CSV üretimi (saf)

**Files:**
- Create: `Core/StaffSync.Application/Features/DirectoryExport/QuotedPrintable.cs`
- Create: `Core/StaffSync.Application/Features/DirectoryExport/VCardWriter.cs`
- Create: `Core/StaffSync.Application/Features/DirectoryExport/CsvDirectoryWriter.cs`
- Test: `Tests/StaffSync.Application.Tests/DirectoryExport/QuotedPrintableTests.cs`, `VCardWriterTests.cs`, `CsvDirectoryWriterTests.cs`

- [ ] **Step 1: Failing testler**

`DirectoryExport/QuotedPrintableTests.cs`:
```csharp
using StaffSync.Application.Features.DirectoryExport;

namespace StaffSync.Application.Tests.DirectoryExport;

public class QuotedPrintableTests
{
    [Theory]
    [InlineData("Kaya", "Kaya")]
    [InlineData("Ayşe", "Ay=C5=9Fe")]
    [InlineData("Çelik", "=C3=87elik")]
    [InlineData("a b", "a=20b")]
    [InlineData("a=b", "a=3Db")]
    public void Encodes_utf8_bytes(string input, string expected) =>
        Assert.Equal(expected, QuotedPrintable.Encode(input, 0));

    [Fact]
    public void Lines_never_exceed_76_chars_and_never_split_a_byte()
    {
        string prefix = "FN;CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE:";
        string encoded = QuotedPrintable.Encode(new string('ş', 60), prefix.Length);
        string[] lines = (prefix + encoded).Split("\r\n");

        Assert.True(lines.Length > 1);
        Assert.All(lines, l => Assert.True(l.Length <= 76, $"{l.Length}: {l}"));
        Assert.All(lines.Take(lines.Length - 1), l => Assert.EndsWith("=", l));
        // "=C" gibi yarım kalmış bir bayt kodu satır sonunda olmamalı.
        Assert.DoesNotContain(lines, l => System.Text.RegularExpressions.Regex.IsMatch(l, "=[0-9A-F]$"));
    }
}
```

`DirectoryExport/VCardWriterTests.cs`:
```csharp
using StaffSync.Application.Features.DirectoryExport;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Tests.DirectoryExport;

public class VCardWriterTests
{
    [Fact]
    public void Ascii_contact_is_written_plain()
    {
        var c = new Contact { DisplayName = "Burak Demir", TelephoneNumber = "+905320000001", Home = "2001", Email = "burak@x.com",
                              Company = "AUNDE", Department = "IT", JobTitle = "Sistem Uzmani" };

        string expected =
            "BEGIN:VCARD\r\n" +
            "VERSION:2.1\r\n" +
            "N:Demir;Burak\r\n" +
            "FN:Burak Demir\r\n" +
            "TEL;CELL:+905320000001\r\n" +
            "TEL;WORK:2001\r\n" +
            "EMAIL;INTERNET:burak@x.com\r\n" +
            "ORG:AUNDE;IT\r\n" +
            "TITLE:Sistem Uzmani\r\n" +
            "END:VCARD\r\n";

        Assert.Equal(expected, VCardWriter.Write(new[] { c }));
    }

    [Fact]
    public void Turkish_values_are_quoted_printable()
    {
        string v = VCardWriter.Write(new[] { new Contact { DisplayName = "Ayşe Kaya", TelephoneNumber = "1" } });
        Assert.Contains("N;CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE:Kaya;Ay=C5=9Fe\r\n", v);
        Assert.Contains("FN;CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE:Ay=C5=9Fe=20Kaya\r\n", v);
    }

    [Fact]
    public void Empty_fields_are_skipped_and_single_word_name_goes_to_first_name()
    {
        string v = VCardWriter.Write(new[] { new Contact { DisplayName = "Resepsiyon", TelephoneNumber = "1", Home = null!, Email = "", Company = null!, Department = null! } });
        Assert.Contains("N:;Resepsiyon\r\n", v);
        Assert.DoesNotContain("TEL;WORK", v);
        Assert.DoesNotContain("EMAIL", v);
        Assert.DoesNotContain("ORG", v);
    }

    [Fact]
    public void Semicolons_in_structured_values_are_escaped() =>
        Assert.Contains("ORG:A\\;B;C\r\n", VCardWriter.Write(new[] { new Contact { DisplayName = "X", Company = "A;B", Department = "C" } }));

    [Fact]
    public void Multiple_contacts_are_concatenated() =>
        Assert.Equal(2, VCardWriter.Write(new[] { new Contact { DisplayName = "A" }, new Contact { DisplayName = "B" } })
            .Split("BEGIN:VCARD").Length - 1);
}
```

`DirectoryExport/CsvDirectoryWriterTests.cs`:
```csharp
using StaffSync.Application.Features.DirectoryExport;
using StaffSync.Domain.Entities;
using System.Text;

namespace StaffSync.Application.Tests.DirectoryExport;

public class CsvDirectoryWriterTests
{
    [Fact]
    public void Starts_with_utf8_bom_and_turkish_header()
    {
        byte[] bytes = CsvDirectoryWriter.Write(Array.Empty<Contact>());
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
        Assert.StartsWith("Ad Soyad;Cep Telefonu;Kısa Kod;E-posta;Unvan;Departman;Şirket;Ofis;Yönetici", Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3));
    }

    [Fact]
    public void Fields_with_separator_or_quote_are_quoted()
    {
        var c = new Contact { DisplayName = "Ayşe \"AK\" Kaya", TelephoneNumber = "1", Home = "", Email = "", JobTitle = "A;B",
                              Department = "", Company = "", Office = "", ManagerName = null! };
        string text = Encoding.UTF8.GetString(CsvDirectoryWriter.Write(new[] { c })[3..]);
        Assert.Contains("\r\n\"Ayşe \"\"AK\"\" Kaya\";1;;;\"A;B\";;;;", text);
    }
}
```

- [ ] **Step 2: Çalıştır** → FAIL.

- [ ] **Step 3: Kod**

`Features/DirectoryExport/QuotedPrintable.cs`:
```csharp
using System.Text;

namespace StaffSync.Application.Features.DirectoryExport
{
    // vCard 2.1'in istediği quoted-printable kodlaması (UTF-8 baytları üzerinden).
    public static class QuotedPrintable
    {
        private const int MaxLine = 76;

        // firstLineOffset: değerin önündeki "AD;PARAMETRELER:" kısmının uzunluğu (ilk satır sınırına dahil).
        public static string Encode(string value, int firstLineOffset)
        {
            var sb = new StringBuilder();
            int lineLength = firstLineOffset;

            foreach (byte b in Encoding.UTF8.GetBytes(value))
            {
                string token = b is >= 33 and <= 126 && b != (byte)'=' ? ((char)b).ToString() : $"={b:X2}";

                // Yumuşak satır sonu "=" de satıra sayılır; "=XX" asla bölünmez.
                if (lineLength + token.Length > MaxLine - 1)
                {
                    sb.Append("=\r\n");
                    lineLength = 0;
                }

                sb.Append(token);
                lineLength += token.Length;
            }

            return sb.ToString();
        }
    }
}
```

`Features/DirectoryExport/VCardWriter.cs`:
```csharp
using StaffSync.Domain.Entities;
using System.Text;

namespace StaffSync.Application.Features.DirectoryExport
{
    // Tuşlu telefonların okuduğu vCard 2.1. ASCII değerler düz, diğerleri UTF-8 + quoted-printable yazılır.
    public static class VCardWriter
    {
        public static string Write(IEnumerable<Contact> contacts)
        {
            var sb = new StringBuilder();
            foreach (Contact c in contacts) WriteOne(sb, c);
            return sb.ToString();
        }

        private static void WriteOne(StringBuilder sb, Contact c)
        {
            var (first, last) = SplitName(c.DisplayName ?? "");

            sb.Append("BEGIN:VCARD\r\n").Append("VERSION:2.1\r\n");
            Prop(sb, "N", $"{Escape(last)};{Escape(first)}", alwaysWrite: true);
            Prop(sb, "FN", c.DisplayName);
            Prop(sb, "TEL;CELL", c.TelephoneNumber);
            Prop(sb, "TEL;WORK", c.Home);
            Prop(sb, "EMAIL;INTERNET", c.Email);
            if (!string.IsNullOrWhiteSpace(c.Company) || !string.IsNullOrWhiteSpace(c.Department))
                Prop(sb, "ORG", $"{Escape(c.Company ?? "")};{Escape(c.Department ?? "")}");
            Prop(sb, "TITLE", c.JobTitle);
            sb.Append("END:VCARD\r\n");
        }

        private static void Prop(StringBuilder sb, string name, string? value, bool alwaysWrite = false)
        {
            if (string.IsNullOrWhiteSpace(value) && !alwaysWrite) return;
            value ??= "";

            if (value.All(ch => ch < 128))
            {
                sb.Append(name).Append(':').Append(value).Append("\r\n");
                return;
            }

            string prefix = name + ";CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE:";
            sb.Append(prefix).Append(QuotedPrintable.Encode(value, prefix.Length)).Append("\r\n");
        }

        // "Ali Rıza Kaya" → ("Ali Rıza", "Kaya"); tek kelime → (kelime, "").
        private static (string First, string Last) SplitName(string displayName)
        {
            string[] parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length switch
            {
                0 => ("", ""),
                1 => (parts[0], ""),
                _ => (string.Join(' ', parts[..^1]), parts[^1])
            };
        }

        private static string Escape(string s) => s.Replace("\\", "\\\\").Replace(";", "\\;");
    }
}
```

`Features/DirectoryExport/CsvDirectoryWriter.cs`:
```csharp
using StaffSync.Domain.Entities;
using System.Text;

namespace StaffSync.Application.Features.DirectoryExport
{
    // Türkçe Excel'in doğrudan açtığı biçim: UTF-8 BOM + ";" ayırıcı.
    public static class CsvDirectoryWriter
    {
        private static readonly string[] Header =
            { "Ad Soyad", "Cep Telefonu", "Kısa Kod", "E-posta", "Unvan", "Departman", "Şirket", "Ofis", "Yönetici" };

        public static byte[] Write(IEnumerable<Contact> contacts)
        {
            var sb = new StringBuilder().Append(string.Join(';', Header));
            foreach (Contact c in contacts)
            {
                sb.Append("\r\n").Append(string.Join(';', new[]
                {
                    c.DisplayName, c.TelephoneNumber, c.Home, c.Email, c.JobTitle, c.Department, c.Company, c.Office, c.ManagerName
                }.Select(Field)));
            }

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        private static string Field(string? value)
        {
            value ??= "";
            return value.IndexOfAny(new[] { ';', '"', '\r', '\n' }) >= 0
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }
    }
}
```

- [ ] **Step 4: Test** → PASS.

- [ ] **Step 5: Commit**

```bash
git add -A .
git commit -m "Tuşlu telefonlar için vCard 2.1 ve CSV üretimi"
```

---

### Task 14: Rehber dosyalarını otomatik yaz + indirme ucu

**Files:**
- Create: `Core/StaffSync.Application/Features/DirectoryExport/ExportSchedule.cs`
- Create: `Core/StaffSync.Application/Features/DirectoryExport/DirectoryExportFiles.cs`
- Create: `Core/StaffSync.Application/Interfaces/Directory/IDirectoryChangeNotifier.cs`
- Create: `Core/StaffSync.Application/Features/Queries/DirectoryQueries/GetDirectoryExportQuery.cs`
- Create: `Core/StaffSync.Application/Features/Handlers/DirectoryHandlers/GetDirectoryExportQueryHandler.cs`
- Create: `Infrastructure/StaffSync.Infrastructure/DirectoryExport/DirectoryChangeNotifier.cs`, `DirectoryExportSettings.cs`, `DirectoryExportBackgroundService.cs`
- Modify: `Infrastructure/StaffSync.Infrastructure/StaffSync.Infrastructure.csproj`, `Registration.cs`
- Modify: `Create/Update/RemoveContactCommandHandler.cs` (sinyal)
- Create: `Presentation/StaffSync.WebApi/Controllers/DirectoryController.cs`
- Modify: `Presentation/StaffSync.WebApi/appsettings.json`
- Test: `Tests/StaffSync.Application.Tests/DirectoryExport/ExportScheduleTests.cs`

- [ ] **Step 1: Failing test**

```csharp
using StaffSync.Application.Features.DirectoryExport;

namespace StaffSync.Application.Tests.DirectoryExport;

public class ExportScheduleTests
{
    private static readonly TimeOnly SixAm = new(6, 0);
    private static readonly TimeSpan Debounce = TimeSpan.FromMinutes(5);
    private static DateTime At(int day, int hour, int minute = 0) => new(2026, 11, day, hour, minute, 0);

    [Fact]
    public void First_run_always_exports() =>
        Assert.True(ExportSchedule.ShouldExport(At(24, 10), lastExport: null, pendingSince: null, SixAm, Debounce));

    [Fact]
    public void Daily_time_passed_since_last_export() =>
        Assert.True(ExportSchedule.ShouldExport(At(24, 6, 1), lastExport: At(23, 6, 0), pendingSince: null, SixAm, Debounce));

    [Fact]
    public void Before_daily_time_nothing_happens() =>
        Assert.False(ExportSchedule.ShouldExport(At(24, 5, 59), lastExport: At(23, 6, 0), pendingSince: null, SixAm, Debounce));

    [Fact]
    public void Already_exported_today_after_daily_time() =>
        Assert.False(ExportSchedule.ShouldExport(At(24, 12), lastExport: At(24, 6, 0), pendingSince: null, SixAm, Debounce));

    [Fact]
    public void Change_waits_for_debounce() =>
        Assert.False(ExportSchedule.ShouldExport(At(24, 12, 4), lastExport: At(24, 6), pendingSince: At(24, 12, 0), SixAm, Debounce));

    [Fact]
    public void Change_exports_after_debounce() =>
        Assert.True(ExportSchedule.ShouldExport(At(24, 12, 5), lastExport: At(24, 6), pendingSince: At(24, 12, 0), SixAm, Debounce));
}
```

- [ ] **Step 2: Çalıştır** → FAIL.

- [ ] **Step 3: Saf zamanlama ve dosya üretimi**

`Features/DirectoryExport/ExportSchedule.cs`:
```csharp
namespace StaffSync.Application.Features.DirectoryExport
{
    public static class ExportSchedule
    {
        // now, lastExport, pendingSince: sunucunun yerel saati.
        public static bool ShouldExport(DateTime now, DateTime? lastExport, DateTime? pendingSince, TimeOnly dailyAt, TimeSpan debounce)
        {
            if (lastExport is null) return true;

            DateTime todaysRun = now.Date + dailyAt.ToTimeSpan();
            if (now >= todaysRun && lastExport < todaysRun) return true;

            return pendingSince is not null && now - pendingSince >= debounce;
        }
    }
}
```

`Features/DirectoryExport/DirectoryExportFiles.cs`:
```csharp
using StaffSync.Domain.Entities;
using System.Text;

namespace StaffSync.Application.Features.DirectoryExport
{
    public record DirectoryExportFile(string FileName, string ContentType, byte[] Content);

    public static class DirectoryExportFiles
    {
        public const string VcfName = "rehber.vcf";
        public const string CsvName = "rehber.csv";

        public static DirectoryExportFile Vcf(IEnumerable<Contact> contacts) =>
            new(VcfName, "text/x-vcard", Encoding.UTF8.GetBytes(VCardWriter.Write(contacts)));

        public static DirectoryExportFile Csv(IEnumerable<Contact> contacts) =>
            new(CsvName, "text/csv", CsvDirectoryWriter.Write(contacts));
    }
}
```

`Interfaces/Directory/IDirectoryChangeNotifier.cs`:
```csharp
namespace StaffSync.Application.Interfaces.Directory
{
    // Kişi eklendi/güncellendi/silindi sinyali; arka plan servisi birkaç dakika sonra dosyaları yeniden yazar.
    public interface IDirectoryChangeNotifier
    {
        void NotifyChanged();
        DateTime? PendingSince { get; }
        void Clear(DateTime pendingSnapshot);
    }
}
```

- [ ] **Step 4: Test** → PASS.

- [ ] **Step 5: İndirme ucu**

`Features/Queries/DirectoryQueries/GetDirectoryExportQuery.cs`:
```csharp
using MediatR;
using StaffSync.Application.Features.DirectoryExport;

namespace StaffSync.Application.Features.Queries.DirectoryQueries
{
    public class GetDirectoryExportQuery : IRequest<DirectoryExportFile>
    {
        public string Format { get; set; } = "vcf"; // "vcf" | "csv"
    }
}
```

`Features/Handlers/DirectoryHandlers/GetDirectoryExportQueryHandler.cs`:
```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.DirectoryExport;
using StaffSync.Application.Features.Queries.DirectoryQueries;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Handlers.DirectoryHandlers
{
    public class GetDirectoryExportQueryHandler : BaseHandler, IRequestHandler<GetDirectoryExportQuery, DirectoryExportFile>
    {
        public GetDirectoryExportQueryHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor) { }

        public async Task<DirectoryExportFile> Handle(GetDirectoryExportQuery request, CancellationToken cancellationToken)
        {
            List<Contact> contacts = await unitOfWork.GetReadRepository<Contact>().AsQueryable()
                .Where(c => !c.IsDeleted).OrderBy(c => c.DisplayName).ToListAsync(cancellationToken);

            return request.Format.Equals("csv", StringComparison.OrdinalIgnoreCase)
                ? DirectoryExportFiles.Csv(contacts)
                : DirectoryExportFiles.Vcf(contacts);
        }
    }
}
```

`Presentation/StaffSync.WebApi/Controllers/DirectoryController.cs`:
```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StaffSync.Application.Features.Queries.DirectoryQueries;

namespace StaffSync.WebApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class DirectoryController : ControllerBase
    {
        private readonly IMediator mediator;

        public DirectoryController(IMediator mediator) => this.mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> Export([FromQuery] string format = "vcf")
        {
            var file = await mediator.Send(new GetDirectoryExportQuery { Format = format });
            return File(file.Content, file.ContentType, file.FileName);
        }
    }
}
```

- [ ] **Step 6: Arka plan servisi**

`Infrastructure/StaffSync.Infrastructure/StaffSync.Infrastructure.csproj` — `<ItemGroup>` olarak ekle:
```xml
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
```

`DirectoryExport/DirectoryExportSettings.cs`:
```csharp
namespace StaffSync.Infrastructure.DirectoryExport
{
    // appsettings "DirectoryExport". PublishFolder boşsa servis hiçbir şey yazmaz.
    public class DirectoryExportSettings
    {
        public string PublishFolder { get; set; } = "";
        public string DailyAt { get; set; } = "06:00";
        public int DebounceMinutes { get; set; } = 5;
    }
}
```

`DirectoryExport/DirectoryChangeNotifier.cs`:
```csharp
using StaffSync.Application.Interfaces.Directory;

namespace StaffSync.Infrastructure.DirectoryExport
{
    // Singleton. İlk değişikliğin zamanını tutar; dışa aktarım bitince o ana kadarki değişiklikleri temizler.
    public class DirectoryChangeNotifier : IDirectoryChangeNotifier
    {
        private long pendingTicks; // 0 = bekleyen değişiklik yok

        public void NotifyChanged() => Interlocked.CompareExchange(ref pendingTicks, DateTime.Now.Ticks, 0);

        public DateTime? PendingSince
        {
            get
            {
                long ticks = Interlocked.Read(ref pendingTicks);
                return ticks == 0 ? null : new DateTime(ticks);
            }
        }

        // Dışa aktarım sırasında yeni değişiklik geldiyse (zaman damgası değiştiyse) silinmez.
        public void Clear(DateTime pendingSnapshot) => Interlocked.CompareExchange(ref pendingTicks, 0, pendingSnapshot.Ticks);
    }
}
```

`DirectoryExport/DirectoryExportBackgroundService.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StaffSync.Application.Features.DirectoryExport;
using StaffSync.Application.Interfaces.Directory;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Infrastructure.DirectoryExport
{
    public class DirectoryExportBackgroundService : BackgroundService
    {
        private static readonly TimeSpan Tick = TimeSpan.FromSeconds(30);

        private readonly IServiceScopeFactory scopeFactory;
        private readonly IDirectoryChangeNotifier notifier;
        private readonly DirectoryExportSettings settings;
        private readonly ILogger<DirectoryExportBackgroundService> logger;
        private DateTime? lastExport;

        public DirectoryExportBackgroundService(IServiceScopeFactory scopeFactory, IDirectoryChangeNotifier notifier,
            IOptions<DirectoryExportSettings> settings, ILogger<DirectoryExportBackgroundService> logger)
        {
            this.scopeFactory = scopeFactory;
            this.notifier = notifier;
            this.settings = settings.Value;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrWhiteSpace(settings.PublishFolder))
            {
                logger.LogWarning("DirectoryExport:PublishFolder boş; rehber dosyaları yazılmayacak.");
                return;
            }

            TimeOnly dailyAt = TimeOnly.Parse(settings.DailyAt);
            TimeSpan debounce = TimeSpan.FromMinutes(settings.DebounceMinutes);

            using var timer = new PeriodicTimer(Tick);
            do
            {
                DateTime now = DateTime.Now;
                DateTime? pending = notifier.PendingSince;
                if (!ExportSchedule.ShouldExport(now, lastExport, pending, dailyAt, debounce)) continue;

                try
                {
                    await ExportAsync(stoppingToken);
                    lastExport = now;
                    if (pending is not null) notifier.Clear(pending.Value);
                    logger.LogInformation("Rehber dosyaları {Folder} klasörüne yazıldı.", settings.PublishFolder);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Klasör erişilemiyor vb. — bir sonraki turda tekrar denenir.
                    logger.LogError(ex, "Rehber dosyaları yazılamadı.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task ExportAsync(CancellationToken cancellationToken)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            List<Contact> contacts = await unitOfWork.GetReadRepository<Contact>().AsQueryable()
                .Where(c => !c.IsDeleted).OrderBy(c => c.DisplayName).ToListAsync(cancellationToken);

            System.IO.Directory.CreateDirectory(settings.PublishFolder);
            await WriteAtomicAsync(DirectoryExportFiles.Vcf(contacts), cancellationToken);
            await WriteAtomicAsync(DirectoryExportFiles.Csv(contacts), cancellationToken);
        }

        // Önce .tmp'ye yazıp sonra yer değiştirir: kopyalayan biri yarım dosya görmez.
        private async Task WriteAtomicAsync(DirectoryExportFile file, CancellationToken cancellationToken)
        {
            string target = Path.Combine(settings.PublishFolder, file.FileName);
            string temp = target + ".tmp";
            await File.WriteAllBytesAsync(temp, file.Content, cancellationToken);
            File.Move(temp, target, overwrite: true);
        }
    }
}
```

`Registration.cs` (Infrastructure) — ekle (`using StaffSync.Application.Interfaces.Directory;`, `using StaffSync.Infrastructure.DirectoryExport;`):
```csharp
            services.Configure<DirectoryExportSettings>(configuration.GetSection("DirectoryExport"));
            services.AddSingleton<IDirectoryChangeNotifier, DirectoryChangeNotifier>();
            services.AddHostedService<DirectoryExportBackgroundService>();
```

`appsettings.json` — ekle:
```json
  "DirectoryExport": { "PublishFolder": "", "DailyAt": "06:00", "DebounceMinutes": 5 },
```

- [ ] **Step 7: Kişi değişince sinyal ver**

`CreateContactCommandHandler`, `UpdateContactCommandHandler`, `RemoveContactCommandHandler` — constructor'a `IDirectoryChangeNotifier directoryChangeNotifier` parametresi ekle, alana ata, `await unitOfWork.SaveAsync();` satırından hemen sonra:
```csharp
            directoryChangeNotifier.NotifyChanged();
```
Örnek (UpdateContactCommandHandler constructor'ı):
```csharp
        private readonly ContactRules contactRules;
        private readonly IDirectoryChangeNotifier directoryChangeNotifier;

        public UpdateContactCommandHandler(ContactRules contactRules, IDirectoryChangeNotifier directoryChangeNotifier,
            IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, httpContextAccessor)
        {
            this.contactRules = contactRules;
            this.directoryChangeNotifier = directoryChangeNotifier;
        }
```
Create ve Remove handler'larına aynı iki satırı (alan + constructor parametresi/ataması) ve `NotifyChanged()` çağrısını ekle. Üç dosyaya `using StaffSync.Application.Interfaces.Directory;` ekle.

- [ ] **Step 8: Test + build** → PASS, `0 Error(s)`.

- [ ] **Step 9: Commit**

```bash
git add -A .
git commit -m "Rehber dosyalarını publish klasörüne otomatik yaz; vcf/csv indirme ucu"
```

---

### Task 15: Uçtan uca doğrulama (mevcut DB, kullanıcı onayıyla)

**Files:** yok (doğrulama)

- [ ] **Step 1: Tam build ve testler**

Run: `dotnet build StaffSync.sln -v q` → `0 Error(s)`
Run: `dotnet test Tests/StaffSync.Application.Tests` → tüm testler PASS (toplam sayıyı not et).

- [ ] **Step 2: Kullanıcıya sor, onay gelirse migration'ları mevcut DB'ye uygula**

Sor: "v7 ve v8 migration'larını appsettings'teki mevcut `StaffSyncDb`'ye uygulayayım mı? (Contacts'a UpdatedDate kolonu + iki yeni tablo)". Onay gelirse:
```bash
dotnet ef database update --project Infrastructure/StaffSync.Persistence --startup-project Presentation/StaffSync.WebApi
```
Expected: `Applying migration '..._v7_ContactUpdatedDate'`, `'..._v8_ContactCorrections'`, `Done.`

- [ ] **Step 3: API'yi çalıştır, Swagger'ı kontrol et**

```bash
cd Presentation/StaffSync.WebApi
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://localhost:5099
```
Başka terminalde:
```bash
curl -s http://localhost:5099/swagger/v1/swagger.json | python -c "import sys,json; print(sorted(json.load(sys.stdin)['paths']))"
```
Expected: listede `/api/Contacts/FilterOptions`, `/api/Contacts/Changes`, `/api/Contacts/ReportCorrection/{id}`, `/api/Corrections/List`, `/api/Corrections/UpdateStatus/{id}`, `/api/Directory/Export` var.

- [ ] **Step 4: Manuel kontrol listesi (kullanıcıyla, Swagger üzerinden, mevcut bir hesapla giriş yapıp)**
  - `POST /api/Contacts/GetContactsWithPagination` `{ "pageNumber": 1, "pageSize": 5 }` → `items`, `totalCount`, `hasMore` döner; 2. sayfada 1. sayfadaki kişiler tekrar etmez.
  - `GET /api/Contacts/FilterOptions` → departmanlar sayılarıyla.
  - `GET /api/Contacts/Changes` → tam liste + `serverTimeUtc`.
  - `GET /api/Directory/Export?format=vcf` → `rehber.vcf` iner; Türkçe isimli bir kişi `CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE` ile yazılmış.
  - Var olmayan id ile `GET /api/Contacts/GetContact/999999` → 404.
  - İndirilen `rehber.vcf` ekibin elindeki **gerçek bir tuşlu telefona** (Bluetooth/SD/USB, bugünkü yöntemle) yüklenir; Türkçe karakterli isimler doğru görünüyor mu, kaç kişinin aktarıldığı kontrol edilir. Model vCard 2.1'i okumuyorsa sonuç kullanıcıya raporlanır (format ayarı ayrı iş).

- [ ] **Step 5: API'yi durdur.** Commit yok.

---

### Task 16: README

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Desen listesini güncelle**

`README.md`'deki numaralı listeyi şununla değiştir:
```markdown
1. Onion Architecture
2. Generic Repository
3. Unit Of Work
4. CQRS
5. MediatR
6. Global Exception
7. Fluent Validation Pipeline Behaviour
8. BaseRules
9. JWT
10. Contact change tracking (mobile sync)
11. Feature-phone directory export (vCard 2.1 / CSV)
```
(AutoMapper ve henüz olmayan Redis çıkarıldı; "Madiatr" yazım hatası düzeltildi.)

- [ ] **Step 2: Commit**

```bash
git add README.md
git commit -m "README: güncel mimari ve özellikler"
```

---

## Kapsam dışı (ayrı plan)

Mobil uygulama (spec §4) ayrı bir planda yazılacak: `docs/superpowers/plans/<tarih>-mobil-uygulama.md`. Bu backend planındaki uçlara dayanır: `GetContactsWithPagination`, `FilterOptions`, `Changes`, `ReportCorrection`.
