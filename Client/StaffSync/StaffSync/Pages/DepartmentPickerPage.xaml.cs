using StaffSync.ViewModels;

namespace StaffSync.Pages
{
    public partial class DepartmentPickerPage : ContentPage
    {
        public DepartmentPickerPage(DepartmentPickerViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
