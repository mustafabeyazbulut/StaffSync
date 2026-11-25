using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StaffSync.Application.Features.Photos;
using StaffSync.Application.Interfaces.Photos;

namespace StaffSync.Infrastructure.Photos
{
    /// <summary>
    /// Kişi fotoğraflarını sunucu diskinde tutar: {Folder}/{kişiId}.jpg|.png.
    /// Folder boşsa uygulama klasöründe App_Data/contact-photos kullanılır (yayında korunması gerekir).
    /// </summary>
    public class FileContactPhotoStore : IContactPhotoStore
    {
        private static readonly string[] Extensions = { ".jpg", ".png" };
        private readonly string folder;

        public FileContactPhotoStore(IOptions<ContactPhotoSettings> settings, IHostEnvironment environment)
        {
            folder = string.IsNullOrWhiteSpace(settings.Value.Folder)
                ? Path.Combine(environment.ContentRootPath, "App_Data", "contact-photos")
                : settings.Value.Folder;
        }

        public async Task SaveAsync(int contactId, byte[] content, string contentType, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(folder);
            DeleteExisting(contactId); // önceki fotoğraf başka uzantıda olabilir

            string target = PathFor(contactId, ImageFormats.ExtensionFor(contentType));
            string temp = target + ".tmp";
            await File.WriteAllBytesAsync(temp, content, cancellationToken);
            File.Move(temp, target, overwrite: true);
        }

        public async Task<ContactPhoto?> GetAsync(int contactId, CancellationToken cancellationToken = default)
        {
            foreach (var ext in Extensions)
            {
                string path = PathFor(contactId, ext);
                if (File.Exists(path))
                {
                    byte[] content = await File.ReadAllBytesAsync(path, cancellationToken);
                    return new ContactPhoto(content, ext == ".png" ? "image/png" : "image/jpeg");
                }
            }
            return null;
        }

        public Task DeleteAsync(int contactId, CancellationToken cancellationToken = default)
        {
            DeleteExisting(contactId);
            return Task.CompletedTask;
        }

        private void DeleteExisting(int contactId)
        {
            foreach (var ext in Extensions)
            {
                string path = PathFor(contactId, ext);
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private string PathFor(int contactId, string extension) => Path.Combine(folder, $"{contactId}{extension}");
    }
}
