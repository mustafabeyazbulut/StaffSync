using StaffSync.ViewModels;

namespace StaffSync.Pages
{
    public partial class FilterPage : ContentPage
    {
        public FilterPage() : this(IPlatformApplication.Current?.Services.GetRequiredService<FilterViewModel>()!)
        {
        }

        public FilterPage(FilterViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
