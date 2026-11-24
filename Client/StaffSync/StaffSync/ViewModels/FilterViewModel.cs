using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StaffSync.Messages;
using StaffSync.Services;
using System.Collections.ObjectModel;

namespace StaffSync.ViewModels
{
    public partial class FilterViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IContactsService _contactsService;

        public FilterViewModel(IContactsService contactsService)
        {
            _contactsService = contactsService;
            Companies = new ObservableCollection<string>();
            SelectedDepartments = new List<string>();

            // Departman seçim ekranından dönen mesajı dinle
            WeakReferenceMessenger.Default.Register<DepartmentsSelectedMessage>(this, (r, m) =>
            {
                SelectedDepartments = m.Value;
                UpdateDepartmentsSummary();
            });
        }

        public ObservableCollection<string> Companies { get; }

        [ObservableProperty]
        public partial List<string> SelectedDepartments { get; set; }

        [ObservableProperty]
        public partial string SelectedDepartmentsSummary { get; set; } = "Tümü";

        [ObservableProperty]
        public partial string? SelectedCompany { get; set; }

        [ObservableProperty]
        public partial string SortBy { get; set; } = "name";

        [ObservableProperty]
        public partial bool IsBusy { get; set; }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("SelectedDepartments", out var deptsObj) && deptsObj is List<string> depts)
            {
                SelectedDepartments = new List<string>(depts);
                UpdateDepartmentsSummary();
            }

            if (query.TryGetValue("SelectedCompany", out var compObj) && compObj is string comp)
            {
                SelectedCompany = string.IsNullOrEmpty(comp) ? "Tüm Şirketler" : comp;
            }

            if (query.TryGetValue("SortBy", out var sortObj) && sortObj is string sort)
            {
                SortBy = string.IsNullOrEmpty(sort) ? "name" : sort;
            }

            _ = LoadFilterOptionsAsync();
        }

        public async Task LoadFilterOptionsAsync()
        {
            try
            {
                IsBusy = true;
                var options = await _contactsService.GetFilterOptionsAsync();
                if (options != null)
                {
                    Companies.Clear();
                    Companies.Add("Tüm Şirketler");
                    foreach (var c in options.Companies)
                    {
                        if (!string.IsNullOrWhiteSpace(c.Name))
                            Companies.Add(c.Name);
                    }

                    if (string.IsNullOrEmpty(SelectedCompany) || !Companies.Contains(SelectedCompany))
                    {
                        SelectedCompany = "Tüm Şirketler";
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateDepartmentsSummary()
        {
            SelectedDepartmentsSummary = SelectedDepartments.Count switch
            {
                0 => "Tümü",
                1 => SelectedDepartments[0],
                _ => $"{SelectedDepartments.Count} Departman Seçildi"
            };
        }

        [RelayCommand]
        private async Task OpenDepartmentPickerAsync()
        {
            await Shell.Current.GoToAsync("department-picker", new Dictionary<string, object>
            {
                ["SelectedDepartments"] = SelectedDepartments
            });
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SelectedDepartments.Clear();
            UpdateDepartmentsSummary();
            SelectedCompany = "Tüm Şirketler";
            SortBy = "name";
        }

        [RelayCommand]
        private async Task ApplyFiltersAsync()
        {
            var company = (SelectedCompany == "Tüm Şirketler" || string.IsNullOrEmpty(SelectedCompany))
                ? null
                : SelectedCompany;

            var criteria = new FilterCriteria
            {
                SelectedDepartments = SelectedDepartments.ToList(),
                SelectedCompany = company,
                SortBy = SortBy
            };

            WeakReferenceMessenger.Default.Send(new FilterChangedMessage(criteria));
            await Shell.Current.GoToAsync("..");
        }
    }
}
