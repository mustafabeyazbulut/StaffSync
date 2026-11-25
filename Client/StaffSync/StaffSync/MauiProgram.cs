using Microsoft.Extensions.Logging;
using StaffSync.Pages;
using StaffSync.Services;
using StaffSync.ViewModels;

namespace StaffSync
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>();

            // Servis Kayıtları
            builder.Services.AddSingleton(sp => new HttpClient { Timeout = TimeSpan.FromSeconds(30) });
            builder.Services.AddSingleton<ISettingsService, SettingsService>();
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<IApiClient, ApiClient>();
            builder.Services.AddSingleton<IContactsService, ContactsService>();

#if ANDROID
            builder.Services.AddSingleton<IContactSyncService, Platforms.Android.Services.AndroidContactSyncService>();
#elif IOS
            builder.Services.AddSingleton<IContactSyncService, Platforms.iOS.Services.IosContactSyncService>();
#else
            builder.Services.AddSingleton<IContactSyncService, DefaultContactSyncService>();
#endif

            // ViewModel ve Sayfa Kayıtları
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<LoginPage>();

            builder.Services.AddTransient<PermissionViewModel>();
            builder.Services.AddTransient<PermissionPage>();

            builder.Services.AddTransient<ContactsViewModel>();
            builder.Services.AddTransient<ContactsPage>();

            builder.Services.AddTransient<FilterViewModel>();
            builder.Services.AddTransient<FilterPage>();

            builder.Services.AddTransient<DepartmentPickerViewModel>();
            builder.Services.AddTransient<DepartmentPickerPage>();

            builder.Services.AddTransient<ProfileViewModel>();
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<ContactDetailViewModel>();
            builder.Services.AddTransient<ContactDetailPage>();
            builder.Services.AddTransient<ReportCorrectionViewModel>();
            builder.Services.AddTransient<ReportCorrectionPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
