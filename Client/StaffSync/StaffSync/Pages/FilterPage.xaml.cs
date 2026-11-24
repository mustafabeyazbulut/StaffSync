using StaffSync.ViewModels;

namespace StaffSync.Pages
{
    public partial class FilterPage : ContentPage
    {
        private readonly FilterViewModel _viewModel;

        public FilterPage(FilterViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        private void OnSortByNameTapped(object? sender, EventArgs e)
        {
            _viewModel.SortBy = "name";
        }

        private void OnSortByDeptTapped(object? sender, EventArgs e)
        {
            _viewModel.SortBy = "department";
        }

        private void OnSortByTitleTapped(object? sender, EventArgs e)
        {
            _viewModel.SortBy = "title";
        }
    }
}
