namespace StaffSync.Models
{
    public static class ApiConfig
    {
        public const string ServerUrlPrefKey = "StaffSync_ServerUrl";

        public static string DefaultBaseUrl
        {
            get
            {
#if ANDROID
                // Android emülatöründe host makinenin localhost'u 10.0.2.2'dir
                return "http://10.0.2.2:5000/";
#else
                return "http://localhost:5000/";
#endif
            }
        }

        public static string BaseUrl
        {
            get => Preferences.Default.Get(ServerUrlPrefKey, DefaultBaseUrl);
            set => Preferences.Default.Set(ServerUrlPrefKey, value);
        }
    }
}
