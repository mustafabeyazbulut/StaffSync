using StaffSync.ViewModels;

namespace StaffSync.Pages
{
    public partial class ReportCorrectionPage : ContentPage
    {
        public ReportCorrectionPage() : this(IPlatformApplication.Current?.Services.GetRequiredService<ReportCorrectionViewModel>()!)
        {
        }

        public ReportCorrectionPage(ReportCorrectionViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
