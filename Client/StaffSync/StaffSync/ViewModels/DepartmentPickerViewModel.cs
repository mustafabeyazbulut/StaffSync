using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StaffSync.Messages;
using StaffSync.Models;
using StaffSync.Services;
using System.Collections.ObjectModel;

namespace StaffSync.ViewModels
{
    public partial class DepartmentPickerViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IContactsService _contactsService;
        private List<FilterOptionItem> _allDepartments = new();

        public DepartmentPickerViewModel(IContactsService contactsService)
        {
            _contactsService = contactsService;
            Departments = new ObservableCollection<FilterOptionItem>();
        }

        public ObservableCollection<FilterOptionItem> Departments { get; }

        [ObservableProperty]
        public partial string SearchText { get; set; } = "";

        [ObservableProperty]
        public partial bool IsBusy { get; set; }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            List<string> selected = new();
            if (query.TryGetValue("SelectedDepartments", out var deptsObj) && deptsObj is List<string> depts)
            {
                selected = depts;
            }

            _ = LoadDepartmentsAsync(selected);
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterVisibleDepartments();
        }

        public async Task LoadDepartmentsAsync(List<string> initiallySelected)
        {
            try
            {
                IsBusy = true;
                var options = await _contactsService.GetFilterOptionsAsync();
                if (options != null)
                {
                    _allDepartments = options.Departments.Select(d => new FilterOptionItem
                    {
                        Name = d.Name,
                        Count = d.Count,
                        IsSelected = initiallySelected.Contains(d.Name)
                    }).OrderByDescending(d => d.IsSelected).ThenBy(d => d.Name).ToList();

                    FilterVisibleDepartments();
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void FilterVisibleDepartments()
        {
            Departments.Clear();
            var query = _allDepartments.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var s = SearchText.Trim().ToLowerInvariant();
                query = query.Where(d => d.Name.ToLowerInvariant().Contains(s));
            }

            foreach (var item in query)
            {
                Departments.Add(item);
            }
        }

        [RelayCommand]
        private void SelectAll()
        {
            foreach (var d in _allDepartments)
                d.IsSelected = true;
        }

        [RelayCommand]
        private void ClearAll()
        {
            foreach (var d in _allDepartments)
                d.IsSelected = false;
        }

        [RelayCommand]
        private async Task ApplyAsync()
        {
            var selectedNames = _allDepartments
                .Where(d => d.IsSelected)
                .Select(d => d.Name)
                .ToList();

            WeakReferenceMessenger.Default.Send(new DepartmentsSelectedMessage(selectedNames));
            await Shell.Current.GoToAsync("..");
        }
    }
}
