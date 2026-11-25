namespace StaffSync.Services
{
    /// <summary>
    /// Telefon rehberine yazılan kişi kaydının biçimi. Biçim değişince (yeni alan eklenince ya da
    /// kullanıcı "şirket hattı" tercihini değiştirince) cihazdaki biçim eskir; bir sonraki senkron
    /// artımlı değil tam yapılır ve tüm kişiler yeni biçimle yeniden yazılır.
    /// </summary>
    public static class ContactSyncFormat
    {
        // 1: ilk sürüm (kısa kod not alanında)
        // 2: kısa kod aranabilir numara; şirket hattı tercihine göre yazılır
        // 3: kişi fotoğrafları
        private const int Version = 3;

        public static string Current(ISettingsService settings) =>
            $"{Version}:{(settings.UsesCompanyLine ? "kisakod" : "cep")}";

        public static bool IsOutdated(ISettingsService settings) =>
            settings.ContactFormat != Current(settings);

        public static void MarkWritten(ISettingsService settings) =>
            settings.ContactFormat = Current(settings);
    }
}
