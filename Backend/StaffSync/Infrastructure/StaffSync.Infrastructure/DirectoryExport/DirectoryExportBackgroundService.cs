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
