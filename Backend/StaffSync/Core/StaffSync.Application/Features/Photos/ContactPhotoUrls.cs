namespace StaffSync.Application.Features.Photos
{
    public static class ContactPhotoUrls
    {
        /// <summary>
        /// API köküne göre göreli adres; "v" her yüklemede değişir ki istemciler yeni fotoğrafı
        /// eski önbellekten ayırt edebilsin.
        /// </summary>
        public static string For(int contactId) =>
            $"api/Contacts/Photo/{contactId}?v={DateTime.UtcNow.Ticks}";
    }
}
