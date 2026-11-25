using StaffSync.ViewModels;

namespace StaffSync.Pages
{
    public partial class FilterPage : ContentPage
    {
        private readonly FilterViewModel _viewModel;

        public FilterPage() : this(IPlatformApplication.Current?.Services.GetRequiredService<FilterViewModel>()!)
        {
        }

        public FilterPage(FilterViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        private void OnSortByNameTapped(object? sender, EventArgs e)
        {
            _viewModel.SortBy = "Name";
        }

        private void OnSortByDeptTapped(object? sender, EventArgs e)
        {
            _viewModel.SortBy = "Department";
        }

        private void OnSortByTitleTapped(object? sender, EventArgs e)
        {
            _viewModel.SortBy = "JobTitle";
        }
    }
}
