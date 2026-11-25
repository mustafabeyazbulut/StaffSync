using StaffSync.Pages;

namespace StaffSync
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("contact-detail", typeof(ContactDetailPage));
            Routing.RegisterRoute("filter", typeof(FilterPage));
            Routing.RegisterRoute("department-picker", typeof(DepartmentPickerPage));
            Routing.RegisterRoute("report-correction", typeof(ReportCorrectionPage));
        }

        protected override void OnNavigated(ShellNavigatedEventArgs args)
        {
            base.OnNavigated(args);
#if ANDROID
            // Sayfa değişince durum çubuğunu yeni sayfanın zemin rengine çek
            Dispatcher.Dispatch(() =>
                (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as MainActivity)?.UpdateStatusBar());
#endif
        }
    }
}
