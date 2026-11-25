using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StaffSync.Models;
using StaffSync.Services;

namespace StaffSync.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IAuthService _authService;
        private readonly ISettingsService _settingsService;

        public LoginViewModel(IAuthService authService, ISettingsService settingsService)
        {
            _authService = authService;
            _settingsService = settingsService;
        }

        [ObservableProperty]
        public partial string Email { get; set; } = "";

        [ObservableProperty]
        public partial string Password { get; set; } = "";

        [ObservableProperty]
        public partial bool IsBusy { get; set; }

        [ObservableProperty]
        public partial string? ErrorMessage { get; set; }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        partial void OnErrorMessageChanged(string? value)
        {
            OnPropertyChanged(nameof(HasError));
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (IsBusy)
                return;

            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Lütfen e-posta adresinizi ve şifrenizi giriniz.";
                return;
            }

            try
            {
                IsBusy = true;
                var success = await _authService.LoginAsync(Email.Trim(), Password);
                if (success)
                {
                    if (_settingsService.HasCompletedPermissionIntro)
                    {
                        await Shell.Current.GoToAsync("//main/contacts");
                    }
                    else
                    {
                        await Shell.Current.GoToAsync("//permission-intro");
                    }
                }
                else
                {
                    ErrorMessage = "E-posta veya şifre hatalı. Lütfen kontrol ediniz.";
                }
            }
            catch (ApiOfflineException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (ApiException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ChangeServerUrlAsync()
        {
            if (Application.Current?.Windows[0].Page is Page page)
            {
                var newUrl = await page.DisplayPromptAsync(
                    "Sunucu Adresi",
                    "API sunucu adresini giriniz:",
                    initialValue: ApiConfig.BaseUrl,
                    placeholder: "http://10.0.2.2:5000/");

                if (!string.IsNullOrWhiteSpace(newUrl))
                {
                    ApiConfig.BaseUrl = newUrl.Trim();
                    await page.DisplayAlertAsync("Bilgi", $"Sunucu adresi güncellendi: {ApiConfig.BaseUrl}", "Tamam");
                }
            }
        }
    }
}
