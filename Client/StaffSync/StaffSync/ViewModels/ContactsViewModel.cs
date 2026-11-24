using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StaffSync.Messages;
using StaffSync.Models;
using StaffSync.Services;
using System.Collections.ObjectModel;

namespace StaffSync.ViewModels
{
    public partial class ContactsViewModel : ObservableObject
    {
        private readonly IContactsService _contactsService;
        private readonly ISettingsService _settingsService;
        private CancellationTokenSource? _searchCts;

        private int _currentPage = 1;
        private const int PageSize = 25;
        private bool _hasMore = true;

        public ContactsViewModel(IContactsService contactsService, ISettingsService settingsService)
        {
            _contactsService = contactsService;
            _settingsService = settingsService;

            Contacts = new ObservableCollection<ContactDto>();
            ActiveDepartmentFilters = new ObservableCollection<string>();

            // Filtre ekranından gelen mesajları dinle
            WeakReferenceMessenger.Default.Register<FilterChangedMessage>(this, (r, m) =>
            {
                ApplyFilterCriteria(m.Value);
            });
        }

        public ObservableCollection<ContactDto> Contacts { get; }
        public ObservableCollection<string> ActiveDepartmentFilters { get; }

        [ObservableProperty]
        public partial string SearchText { get; set; } = "";

        [ObservableProperty]
        public partial bool IsBusy { get; set; }

        [ObservableProperty]
        public partial bool IsRefreshing { get; set; }

        [ObservableProperty]
        public partial bool IsLoadingMore { get; set; }

        [ObservableProperty]
        public partial bool IsOffline { get; set; }

        [ObservableProperty]
        public partial int TotalCount { get; set; }

        [ObservableProperty]
        public partial string? SelectedCompany { get; set; }

        public bool HasSelectedCompany => !string.IsNullOrEmpty(SelectedCompany);

        partial void OnSelectedCompanyChanged(string? value)
        {
            OnPropertyChanged(nameof(HasSelectedCompany));
        }

        [ObservableProperty]
        public partial string? SortBy { get; set; }

        public int ActiveFilterCount => ActiveDepartmentFilters.Count + (string.IsNullOrEmpty(SelectedCompany) ? 0 : 1);
        public bool HasActiveFilters => ActiveFilterCount > 0;

        partial void OnSearchTextChanged(string value)
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(350, token);
                    if (!token.IsCancellationRequested)
                    {
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await ReloadContactsAsync();
                        });
                    }
                }
                catch (TaskCanceledException) { }
            }, token);
        }

        public async Task InitializeAsync()
        {
            if (Contacts.Count == 0)
            {
                await ReloadContactsAsync();
            }
        }

        private void ApplyFilterCriteria(FilterCriteria criteria)
        {
            ActiveDepartmentFilters.Clear();
            foreach (var d in criteria.SelectedDepartments)
                ActiveDepartmentFilters.Add(d);

            SelectedCompany = criteria.SelectedCompany;
            SortBy = criteria.SortBy;

            OnPropertyChanged(nameof(ActiveFilterCount));
            OnPropertyChanged(nameof(HasActiveFilters));

            _ = ReloadContactsAsync();
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            IsRefreshing = true;
            await ReloadContactsAsync();
            IsRefreshing = false;
        }

        [RelayCommand]
        public async Task ReloadContactsAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                _currentPage = 1;
                _hasMore = true;

                var result = await _contactsService.GetContactsAsync(
                    pageNumber: 1,
                    pageSize: PageSize,
                    search: SearchText,
                    departments: ActiveDepartmentFilters.ToList(),
                    companies: string.IsNullOrEmpty(SelectedCompany) ? null : new List<string> { SelectedCompany },
                    sortBy: SortBy);

                if (result != null)
                {
                    IsOffline = false;
                    TotalCount = result.TotalCount;
                    _hasMore = result.HasMore;

                    Contacts.Clear();
                    foreach (var c in result.Items)
                        Contacts.Add(c);

                    // Başarılı sorguda arama/filtre boşsa ana önbelleği tazele
                    if (string.IsNullOrWhiteSpace(SearchText) && ActiveFilterCount == 0)
                    {
                        _ = _settingsService.SaveCachedContactsAsync(result.Items.ToList());
                    }
                }
                else
                {
                    // Ağ hatası -> Çevrimdışı önbellekten göster
                    await LoadFromOfflineCacheAsync();
                }
            }
            catch
            {
                await LoadFromOfflineCacheAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadFromOfflineCacheAsync()
        {
            IsOffline = true;
            var cached = await _settingsService.GetCachedContactsAsync();
            if (cached.Count > 0)
            {
                var query = cached.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var s = SearchText.Trim().ToLowerInvariant();
                    query = query.Where(c => (c.DisplayName?.ToLowerInvariant().Contains(s) ?? false) ||
                                             (c.JobTitle?.ToLowerInvariant().Contains(s) ?? false) ||
                                             (c.Department?.ToLowerInvariant().Contains(s) ?? false));
                }

                if (ActiveDepartmentFilters.Count > 0)
                {
                    query = query.Where(c => c.Department != null && ActiveDepartmentFilters.Contains(c.Department));
                }

                if (!string.IsNullOrEmpty(SelectedCompany))
                {
                    query = query.Where(c => c.Company == SelectedCompany);
                }

                var list = query.ToList();
                TotalCount = list.Count;
                Contacts.Clear();
                foreach (var c in list)
                    Contacts.Add(c);
            }
        }

        [RelayCommand]
        public async Task LoadMoreContactsAsync()
        {
            if (IsLoadingMore || IsBusy || !_hasMore || IsOffline)
                return;

            try
            {
                IsLoadingMore = true;
                var nextPage = _currentPage + 1;

                var result = await _contactsService.GetContactsAsync(
                    pageNumber: nextPage,
                    pageSize: PageSize,
                    search: SearchText,
                    departments: ActiveDepartmentFilters.ToList(),
                    companies: string.IsNullOrEmpty(SelectedCompany) ? null : new List<string> { SelectedCompany },
                    sortBy: SortBy);

                if (result != null && result.Items.Count > 0)
                {
                    _currentPage = nextPage;
                    _hasMore = result.HasMore;
                    TotalCount = result.TotalCount;

                    foreach (var c in result.Items)
                        Contacts.Add(c);
                }
                else
                {
                    _hasMore = false;
                }
            }
            catch
            {
                // Sessizce başarısız ol
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        [RelayCommand]
        public async Task OpenDetailAsync(ContactDto? contact)
        {
            if (contact == null)
                return;

            await Shell.Current.GoToAsync("contact-detail", new Dictionary<string, object>
            {
                ["Contact"] = contact
            });
        }

        [RelayCommand]
        public async Task OpenFilterAsync()
        {
            await Shell.Current.GoToAsync("filter", new Dictionary<string, object>
            {
                ["SelectedDepartments"] = ActiveDepartmentFilters.ToList(),
                ["SelectedCompany"] = SelectedCompany ?? "",
                ["SortBy"] = SortBy ?? "name"
            });
        }

        [RelayCommand]
        public void RemoveDepartmentFilter(string dept)
        {
            if (ActiveDepartmentFilters.Remove(dept))
            {
                OnPropertyChanged(nameof(ActiveFilterCount));
                OnPropertyChanged(nameof(HasActiveFilters));
                _ = ReloadContactsAsync();
            }
        }

        [RelayCommand]
        public void ClearCompanyFilter()
        {
            SelectedCompany = null;
            OnPropertyChanged(nameof(ActiveFilterCount));
            OnPropertyChanged(nameof(HasActiveFilters));
            _ = ReloadContactsAsync();
        }

        [RelayCommand]
        public void ClearAllFilters()
        {
            ActiveDepartmentFilters.Clear();
            SelectedCompany = null;
            OnPropertyChanged(nameof(ActiveFilterCount));
            OnPropertyChanged(nameof(HasActiveFilters));
            _ = ReloadContactsAsync();
        }

        [RelayCommand]
        public void CallContact(ContactDto? contact)
        {
            if (contact == null || string.IsNullOrWhiteSpace(contact.TelephoneNumber))
                return;

            try
            {
                if (PhoneDialer.Default.IsSupported)
                    PhoneDialer.Default.Open(contact.TelephoneNumber);
            }
            catch
            {
                // Arama açılamadı
            }
        }
    }
}
