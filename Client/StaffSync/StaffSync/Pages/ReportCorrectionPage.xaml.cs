using StaffSync.ViewModels;

namespace StaffSync.Pages
{
    public partial class ReportCorrectionPage : ContentPage
    {
        public ReportCorrectionPage(ReportCorrectionViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
