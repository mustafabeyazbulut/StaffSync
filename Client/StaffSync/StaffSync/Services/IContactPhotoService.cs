using System.Security.Cryptography;
using System.Text;
using StaffSync.Models;

namespace StaffSync.Services
{
    /// <summary>
    /// Kişi fotoğraflarını API'den indirip cihazda saklar. Fotoğraf adresi sürüm içerdiği için
    /// (…?v=…) adres değişmedikçe tekrar indirilmez; çevrimdışıyken de son indirilen gösterilir.
    /// </summary>
    public interface IContactPhotoService
    {
        /// <summary>Cihazdaki fotoğraf dosyası; fotoğraf yoksa ya da indirilemezse null.</summary>
        Task<string?> GetLocalPathAsync(ContactDto contact, CancellationToken ct = default);

        /// <summary>Kişilerin fotoğraflarını sırayla indirip <see cref="ContactDto.PhotoPath"/>'e yazar.</summary>
        Task LoadPhotosAsync(IEnumerable<ContactDto> contacts, CancellationToken ct = default);
    }

    public class ContactPhotoService : IContactPhotoService
    {
        private readonly IApiClient _apiClient;
        private readonly string _folder = Path.Combine(FileSystem.CacheDirectory, "contact-photos");
        private readonly SemaphoreSlim _downloadLock = new(3, 3); // aynı anda en fazla 3 indirme

        public ContactPhotoService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<string?> GetLocalPathAsync(ContactDto contact, CancellationToken ct = default)
        {
            if (!contact.HasPhotoUrl)
                return null;

            var path = Path.Combine(_folder, $"{contact.Id}_{VersionKey(contact.ImageUrl!)}.img");
            if (File.Exists(path))
                return path;

            await _downloadLock.WaitAsync(ct);
            try
            {
                if (File.Exists(path))
                    return path; // beklerken başka bir istek indirmiş olabilir

                // ImageUrl "api/Contacts/Photo/5?v=…"; ApiClient "api/" önekini kendisi ekliyor
                var endpoint = contact.ImageUrl!.StartsWith("api/", StringComparison.OrdinalIgnoreCase)
                    ? contact.ImageUrl[4..]
                    : contact.ImageUrl;
                var bytes = await _apiClient.GetBytesAsync(endpoint, ct);
                if (bytes == null || bytes.Length == 0)
                    return null;

                Directory.CreateDirectory(_folder);
                DeleteOldVersions(contact.Id);
                var temp = path + ".tmp";
                await File.WriteAllBytesAsync(temp, bytes, ct);
                File.Move(temp, path, overwrite: true);
                return path;
            }
            catch (Exception ex) when (ex is ApiOfflineException || ex is ApiException || ex is IOException || ex is HttpRequestException)
            {
                // Fotoğraf gelmezse baş harfler gösterilir; liste bundan etkilenmez
                System.Diagnostics.Debug.WriteLine($"Fotoğraf indirilemedi ({contact.Id}): {ex.Message}");
                return null;
            }
            finally
            {
                _downloadLock.Release();
            }
        }

        public async Task LoadPhotosAsync(IEnumerable<ContactDto> contacts, CancellationToken ct = default)
        {
            var tasks = contacts
                .Where(c => c.HasPhotoUrl && c.PhotoPath == null)
                .Select(async c =>
                {
                    var path = await GetLocalPathAsync(c, ct);
                    if (path != null)
                        MainThread.BeginInvokeOnMainThread(() => c.PhotoPath = path);
                });
            await Task.WhenAll(tasks);
        }

        private void DeleteOldVersions(int contactId)
        {
            foreach (var old in Directory.EnumerateFiles(_folder, $"{contactId}_*.img"))
            {
                try { File.Delete(old); }
                catch (IOException ex) { System.Diagnostics.Debug.WriteLine($"Eski fotoğraf silinemedi: {ex.Message}"); }
            }
        }

        private static string VersionKey(string url) =>
            Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(url)))[..12];
    }
}
