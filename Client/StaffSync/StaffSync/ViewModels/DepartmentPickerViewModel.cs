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
            SelectedItems = new ObservableCollection<FilterOptionItem>();
            RemainingItems = new ObservableCollection<FilterOptionItem>();
        }

        public ObservableCollection<FilterOptionItem> SelectedItems { get; }
        public ObservableCollection<FilterOptionItem> RemainingItems { get; }

        [ObservableProperty]
        public partial string SearchText { get; set; } = "";

        [ObservableProperty]
        public partial bool IsBusy { get; set; }

        [ObservableProperty]
        public partial bool HasSelected { get; set; }

        [ObservableProperty]
        public partial bool NoMatch { get; set; }

        [ObservableProperty]
        public partial string RestTitle { get; set; } = "TÜM DEPARTMANLAR";

        [ObservableProperty]
        public partial string DoneLabel { get; set; } = "Tümünü göster";

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
            FilterLists();
        }

        public async Task LoadDepartmentsAsync(List<string> initiallySelected)
        {
            try
            {
                IsBusy = true;
                var options = await _contactsService.GetFilterOptionsAsync();
                if (options != null)
                {
                    _allDepartments = options.Departments.Select(d =>
                    {
                        var item = new FilterOptionItem
                        {
                            Name = d.Name,
                            Count = d.Count,
                            IsSelected = initiallySelected.Contains(d.Name)
                        };
                        item.OnSelectionChanged = OnItemSelectionChanged;
                        return item;
                    }).ToList();

                    FilterLists();
                }
            }
            catch (SessionExpiredException)
            {
                // Zaten login'e yönlendiriliyor
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Departmanlar yükleme hatası: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnItemSelectionChanged()
        {
            FilterLists();
        }

        private void FilterLists()
        {
            SelectedItems.Clear();
            RemainingItems.Clear();

            var search = SearchText?.Trim().ToLowerInvariant() ?? "";
            bool isSearching = !string.IsNullOrWhiteSpace(search);

            RestTitle = isSearching ? "SONUÇLAR" : "TÜM DEPARTMANLAR";

            var filtered = isSearching
                ? _allDepartments.Where(d => d.Name.ToLowerInvariant().Contains(search)).ToList()
                : _allDepartments;

            foreach (var d in filtered)
            {
                if (d.IsSelected)
                {
                    SelectedItems.Add(d);
                }
                else
                {
                    RemainingItems.Add(d);
                }
            }

            HasSelected = SelectedItems.Count > 0;
            NoMatch = isSearching && filtered.Count == 0;

            int count = _allDepartments.Count(d => d.IsSelected);
            DoneLabel = count switch
            {
                0 => "Tümünü göster",
                1 => "1 departman seçildi",
                _ => $"{count} departman seçildi"
            };
        }

        [RelayCommand]
        public void ToggleItem(FilterOptionItem? item)
        {
            if (item == null) return;
            item.IsSelected = !item.IsSelected;
        }

        [RelayCommand]
        private void ClearAll()
        {
            foreach (var d in _allDepartments)
                d.IsSelected = false;
            FilterLists();
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            if (Shell.Current.Navigation.ModalStack.Count > 0)
                await Shell.Current.Navigation.PopModalAsync();
            else
                await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        private async Task ApplyAsync()
        {
            var selectedNames = _allDepartments
                .Where(d => d.IsSelected)
                .Select(d => d.Name)
                .ToList();

            WeakReferenceMessenger.Default.Send(new DepartmentsSelectedMessage(selectedNames));
            await GoBackAsync();
        }
    }
}
