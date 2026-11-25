using StaffSync.Controls;

namespace StaffSync.Services
{
    public enum DialogKind
    {
        Info,
        Success,
        Error,
    }

    /// <summary>
    /// Uygulamanın tüm uyarıları. Sistemin sade uyarı penceresi yerine marka tasarımında kart ve
    /// bildirim gösterir; ViewModel'ler sayfaya (Application.Current.Windows[0].Page) erişmez.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>Kısa, kendiliğinden kaybolan bildirim (ör. "Rehberiniz güncel"). Dokununca da kapanır.</summary>
        Task ToastAsync(string message, DialogKind kind = DialogKind.Success);

        /// <summary>Tek düğmeli bilgi/hata kartı.</summary>
        Task AlertAsync(string title, string message, DialogKind kind = DialogKind.Info, string button = "Tamam");

        /// <summary>Onay kartı; onaylanırsa true. <paramref name="destructive"/> ise onay düğmesi kırmızıdır.</summary>
        Task<bool> ConfirmAsync(string title, string message, string accept, string cancel = "Vazgeç", bool destructive = false);
    }

    public class DialogService : IDialogService
    {
        // Aynı anda tek pencere: üst üste binmesinler
        private readonly SemaphoreSlim _gate = new(1, 1);

        public Task ToastAsync(string message, DialogKind kind = DialogKind.Success) =>
            ShowAsync(AppDialogPage.Toast(message, kind));

        public Task AlertAsync(string title, string message, DialogKind kind = DialogKind.Info, string button = "Tamam") =>
            ShowAsync(AppDialogPage.Alert(title, message, kind, button));

        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel = "Vazgeç", bool destructive = false) =>
            ShowAsync(AppDialogPage.Confirm(title, message, accept, cancel, destructive));

        private async Task<bool> ShowAsync(AppDialogPage dialog)
        {
            await _gate.WaitAsync();
            try
            {
                var navigation = Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;
                if (navigation == null)
                    return false;

                await MainThread.InvokeOnMainThreadAsync(() => navigation.PushModalAsync(dialog, animated: false));
                return await dialog.Result;
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}
