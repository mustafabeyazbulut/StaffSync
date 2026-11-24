using StaffSync.Pages;

namespace StaffSync
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("login", typeof(LoginPage));
            Routing.RegisterRoute("permission-intro", typeof(PermissionPage));
            Routing.RegisterRoute("contact-detail", typeof(ContactDetailPage));
            Routing.RegisterRoute("filter", typeof(FilterPage));
            Routing.RegisterRoute("department-picker", typeof(DepartmentPickerPage));
            Routing.RegisterRoute("report-correction", typeof(ReportCorrectionPage));
        }
    }
}
