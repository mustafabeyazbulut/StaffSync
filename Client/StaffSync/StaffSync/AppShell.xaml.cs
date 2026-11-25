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
    }
}
