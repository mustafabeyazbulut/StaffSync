using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StaffSync.Messages;
using StaffSync.Services;
using System.Collections.ObjectModel;

namespace StaffSync.ViewModels
{
    public partial class CompanyFilterItem : ObservableObject
    {
        public string Name { get; set; } = "";

        [ObservableProperty]
        public partial bool IsSelected { get; set; }
    }

    public partial class FilterViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IContactsService _contactsService;

        public FilterViewModel(IContactsService contactsService)
        {
            _contactsService = contactsService;
            Companies = new ObservableCollection<CompanyFilterItem>();
            SelectedDepartments = new List<string>();

            // Departman seçim ekranından dönen mesajı dinle
            WeakReferenceMessenger.Default.Register<DepartmentsSelectedMessage>(this, (r, m) =>
            {
                SelectedDepartments = m.Value;
                UpdateDepartmentsSummary();
                UpdateApplyButtonText();
            });
        }

        public ObservableCollection<CompanyFilterItem> Companies { get; }

        [ObservableProperty]
        public partial List<string> SelectedDepartments { get; set; }

        [ObservableProperty]
        public partial string SelectedDepartmentsSummary { get; set; } = "Tümü";

        [ObservableProperty]
        public partial string DepartmentCountText { get; set; } = "Tümü";

        [ObservableProperty]
        public partial string? SelectedCompany { get; set; } = "Tümü";

        [ObservableProperty]
        public partial string SortBy { get; set; } = "Name";

        [ObservableProperty]
        public partial int SelectedSortIndex { get; set; } = 0;

        [ObservableProperty]
        public partial string ApplyButtonText { get; set; } = "Sonuçları göster";

        [ObservableProperty]
        public partial bool IsBusy { get; set; }

        partial void OnSelectedSortIndexChanged(int value)
        {
            SortBy = value switch
            {
                1 => "Department",
                2 => "JobTitle",
                _ => "Name"
            };
            UpdateApplyButtonText();
        }

        partial void OnSortByChanged(string value)
        {
            SelectedSortIndex = value?.ToLowerInvariant() switch
            {
                "department" => 1,
                "jobtitle" => 2,
                _ => 0
            };
            UpdateApplyButtonText();
        }

        partial void OnSelectedCompanyChanged(string? value)
        {
            foreach (var c in Companies)
            {
                c.IsSelected = string.Equals(c.Name, value, StringComparison.OrdinalIgnoreCase);
            }
            UpdateApplyButtonText();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("SelectedDepartments", out var deptsObj) && deptsObj is List<string> depts)
            {
                SelectedDepartments = new List<string>(depts);
                UpdateDepartmentsSummary();
            }

            if (query.TryGetValue("SelectedCompany", out var compObj) && compObj is string comp)
            {
                SelectedCompany = string.IsNullOrEmpty(comp) ? "Tümü" : comp;
            }

            if (query.TryGetValue("SortBy", out var sortObj) && sortObj is string sort)
            {
                SortBy = string.IsNullOrEmpty(sort) ? "Name" : sort;
                SelectedSortIndex = SortBy.ToLowerInvariant() switch
                {
                    "department" => 1,
                    "jobtitle" => 2,
                    _ => 0
                };
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
                    Companies.Add(new CompanyFilterItem
                    {
                        Name = "Tümü",
                        IsSelected = string.IsNullOrEmpty(SelectedCompany) || SelectedCompany == "Tümü" || SelectedCompany == "Tüm Şirketler"
                    });

                    foreach (var c in options.Companies)
                    {
                        if (!string.IsNullOrWhiteSpace(c.Name))
                        {
                            Companies.Add(new CompanyFilterItem
                            {
                                Name = c.Name,
                                IsSelected = string.Equals(SelectedCompany, c.Name, StringComparison.OrdinalIgnoreCase)
                            });
                        }
                    }

                    if (string.IsNullOrEmpty(SelectedCompany) || SelectedCompany == "Tüm Şirketler")
                    {
                        SelectedCompany = "Tümü";
                    }
                }
            }
            catch (SessionExpiredException)
            {
                // Zaten login'e yönlendiriliyor
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Filtre seçenekleri yükleme hatası: {ex}");
            }
            finally
            {
                IsBusy = false;
                UpdateApplyButtonText();
            }
        }

        private void UpdateDepartmentsSummary()
        {
            SelectedDepartmentsSummary = SelectedDepartments.Count switch
            {
                0 => "Tümü",
                1 => SelectedDepartments[0],
                _ => $"{SelectedDepartments.Count} departman seçildi"
            };

            DepartmentCountText = SelectedDepartments.Count == 0 ? "Tümü" : SelectedDepartments.Count.ToString();
        }

        private void UpdateApplyButtonText()
        {
            int filterCount = 0;
            if (SelectedDepartments.Count > 0)
                filterCount += SelectedDepartments.Count;
            if (!string.IsNullOrEmpty(SelectedCompany) && SelectedCompany != "Tümü" && SelectedCompany != "Tüm Şirketler")
                filterCount += 1;
            if (SortBy != "Name")
                filterCount += 1;

            ApplyButtonText = filterCount switch
            {
                0 => "Sonuçları göster",
                1 => "Sonuçları göster · 1 filtre",
                _ => $"Sonuçları göster · {filterCount} filtre"
            };
        }

        [RelayCommand]
        public void SelectCompany(CompanyFilterItem? item)
        {
            if (item == null) return;
            SelectedCompany = item.Name;
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
            SelectedCompany = "Tümü";
            SortBy = "Name";
            SelectedSortIndex = 0;
            UpdateApplyButtonText();
        }

        [RelayCommand]
        private async Task ApplyFiltersAsync()
        {
            var company = (SelectedCompany == "Tümü" || SelectedCompany == "Tüm Şirketler" || string.IsNullOrEmpty(SelectedCompany))
                ? null
                : SelectedCompany;

            var criteria = new FilterCriteria
            {
                SelectedDepartments = SelectedDepartments.ToList(),
                SelectedCompany = company,
                SortBy = SortBy
            };

            WeakReferenceMessenger.Default.Send(new FilterChangedMessage(criteria));
            if (Shell.Current.Navigation.ModalStack.Count > 0)
                await Shell.Current.Navigation.PopModalAsync();
            else
                await Shell.Current.GoToAsync("..");
        }
    }
}
