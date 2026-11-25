namespace StaffSync.Application.Interfaces.Photos
{
    /// <summary>Kişi fotoğraflarının saklandığı yer (Infrastructure: sunucu diski).</summary>
    public interface IContactPhotoStore
    {
        Task SaveAsync(int contactId, byte[] content, string contentType, CancellationToken cancellationToken = default);
        Task<ContactPhoto?> GetAsync(int contactId, CancellationToken cancellationToken = default);
        Task DeleteAsync(int contactId, CancellationToken cancellationToken = default);
    }

    public sealed record ContactPhoto(byte[] Content, string ContentType);
}
