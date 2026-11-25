using StaffSync.ViewModels;

namespace StaffSync.Pages
{
    public partial class DepartmentPickerPage : ContentPage
    {
        public DepartmentPickerPage() : this(IPlatformApplication.Current?.Services.GetRequiredService<DepartmentPickerViewModel>()!)
        {
        }

        public DepartmentPickerPage(DepartmentPickerViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
