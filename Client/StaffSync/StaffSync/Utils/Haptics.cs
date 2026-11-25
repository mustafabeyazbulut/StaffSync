namespace StaffSync.Utils
{
    /// <summary>Dokunma titreşimi. Cihaz desteklemiyorsa sessizce atlanır; asıl işleme engel olmaz.</summary>
    public static class Haptics
    {
        public static void Click()
        {
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch (Exception ex) when (ex is FeatureNotSupportedException || ex is PermissionException)
            {
                System.Diagnostics.Debug.WriteLine($"Titreşim yapılamadı: {ex.Message}");
            }
        }
    }
}
