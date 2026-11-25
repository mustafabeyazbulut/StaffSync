namespace StaffSync.Application.Features.Photos
{
    /// <summary>
    /// Yüklenen dosyanın gerçekten resim olup olmadığını dosya başlığından (magic bytes) anlar.
    /// İstemcinin bildirdiği Content-Type'a güvenilmez.
    /// </summary>
    public static class ImageFormats
    {
        public const int MaxPhotoBytes = 2 * 1024 * 1024; // 2 MB

        private static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF };
        private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        /// <summary>"image/jpeg", "image/png" ya da desteklenmeyen biçimse null.</summary>
        public static string? DetectContentType(ReadOnlySpan<byte> content)
        {
            if (content.StartsWith(Jpeg)) return "image/jpeg";
            if (content.StartsWith(Png)) return "image/png";
            return null;
        }

        public static string ExtensionFor(string contentType) =>
            contentType == "image/png" ? ".png" : ".jpg";
    }
}
