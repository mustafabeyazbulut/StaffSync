using Microsoft.Extensions.Logging;
using StaffSync.Services;

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
            builder.Services.AddSingleton<HttpClient>();
            builder.Services.AddSingleton<ISettingsService, SettingsService>();
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<IApiClient, ApiClient>();
            builder.Services.AddSingleton<IContactsService, ContactsService>();
            builder.Services.AddSingleton<IContactSyncService, DefaultContactSyncService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
