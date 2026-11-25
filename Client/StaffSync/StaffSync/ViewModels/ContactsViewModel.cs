using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StaffSync.Messages;
using StaffSync.Models;
using StaffSync.Services;
using System.Collections.ObjectModel;

namespace StaffSync.ViewModels
{
    public class ContactGroup : ObservableCollection<ContactDto>
    {
        public string Key { get; }

        public ContactGroup(string key, IEnumerable<ContactDto> contacts) : base(contacts)
        {
            Key = key;
        }
    }

    public partial class ContactsViewModel : ObservableObject
    {
        private readonly IContactsService _contactsService;
        private readonly ISettingsService _settingsService;
        private readonly IContactCacheService _contactCacheService;
        private CancellationTokenSource? _searchCts;
        private CancellationTokenSource? _reloadCts;
        private int _reloadVersion = 0;

        private int _currentPage = 1;
        private const int PageSize = 25;
        private bool _hasMore = true;

        private static readonly string[] TurkishAlphabet = new[]
        {
            "A", "B", "C", "Ç", "D", "E", "F", "G", "H", "I", "İ", "J", "K", "L", "M", "N", "O", "Ö", "P", "R", "S", "Ş", "T", "U", "Ü", "V", "Y", "Z"
        };

        public ContactsViewModel(
            IContactsService contactsService,
            ISettingsService settingsService,
            IContactCacheService contactCacheService)
        {
            _contactsService = contactsService;
            _settingsService = settingsService;
            _contactCacheService = contactCacheService;

            Contacts = new ObservableCollection<ContactDto>();
            GroupedContacts = new ObservableCollection<ContactGroup>();
            AlphabetLetters = new ObservableCollection<string>(TurkishAlphabet);
            ActiveDepartmentFilters = new ObservableCollection<string>();

            // Filtre ekranından gelen mesajları dinle
            WeakReferenceMessenger.Default.Register<FilterChangedMessage>(this, (r, m) =>
            {
                ApplyFilterCriteria(m.Value);
            });
        }

        public ObservableCollection<ContactDto> Contacts { get; }
        public ObservableCollection<ContactGroup> GroupedContacts { get; }
        public ObservableCollection<string> AlphabetLetters { get; }
        public ObservableCollection<string> ActiveDepartmentFilters { get; }

        [ObservableProperty]
        public partial bool IsGrouped { get; set; } = true;

        public bool IsNotGrouped => !IsGrouped;

        partial void OnSortByChanged(string? value)
        {
            IsGrouped = string.IsNullOrEmpty(value) || string.Equals(value, "Name", StringComparison.OrdinalIgnoreCase);
            OnPropertyChanged(nameof(IsNotGrouped));
        }

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
        public partial string? SortBy { get; set; } = "Name";

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
                catch (TaskCanceledException)
                {
                    // Yeni tuşa basıldığı için eski arama gecikmesi iptal edildi
                }
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

        public static string GetInitialLetter(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "#";

            var trimmed = name.Trim();
            var firstChar = trimmed.Substring(0, 1).ToUpper(new System.Globalization.CultureInfo("tr-TR"));
            if (char.IsLetter(firstChar[0]))
                return firstChar;

            return "#";
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            IsRefreshing = true;
            _ = _contactCacheService.RefreshCacheAsync();
            await ReloadContactsAsync();
            IsRefreshing = false;
        }

        [RelayCommand]
        public async Task ReloadContactsAsync()
        {
            _reloadCts?.Cancel();
            _reloadCts = new CancellationTokenSource();
            var cts = _reloadCts;
            var currentVersion = Interlocked.Increment(ref _reloadVersion);

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
                    company: string.IsNullOrEmpty(SelectedCompany) ? null : SelectedCompany,
                    sortBy: SortBy);

                if (cts.IsCancellationRequested || currentVersion != _reloadVersion)
                    return;

                if (result != null)
                {
                    IsOffline = false;
                    TotalCount = result.TotalCount;
                    _hasMore = result.HasMore;

                    Contacts.Clear();
                    GroupedContacts.Clear();

                    foreach (var c in result.Items)
                        Contacts.Add(c);

                    if (IsGrouped)
                    {
                        var culture = new System.Globalization.CultureInfo("tr-TR");
                        var groups = result.Items
                            .GroupBy(c => GetInitialLetter(c.DisplayName))
                            .OrderBy(g => g.Key == "#" ? "ZZZ" : g.Key, StringComparer.Create(culture, false));

                        foreach (var g in groups)
                        {
                            GroupedContacts.Add(new ContactGroup(g.Key, g));
                        }
                    }
                }
            }
            catch (ApiOfflineException)
            {
                if (currentVersion == _reloadVersion)
                {
                    await LoadFromOfflineCacheAsync();
                }
            }
            catch (ApiException ex)
            {
                if (currentVersion == _reloadVersion)
                {
                    IsOffline = false;
                    if (Application.Current?.Windows[0].Page is Page page)
                    {
                        await page.DisplayAlertAsync("Hata", ex.Message, "Tamam");
                    }
                }
            }
            catch (Exception ex)
            {
                if (currentVersion == _reloadVersion)
                {
                    IsOffline = false;
                    System.Diagnostics.Debug.WriteLine($"Rehber yükleme hatası: {ex}");
                    if (Application.Current?.Windows[0].Page is Page page)
                    {
                        await page.DisplayAlertAsync("Hata", "Beklenmeyen bir hata oluştu: " + ex.Message, "Tamam");
                    }
                }
            }
            finally
            {
                if (currentVersion == _reloadVersion)
                {
                    IsBusy = false;
                }
            }
        }

        private async Task LoadFromOfflineCacheAsync()
        {
            IsOffline = true;
            var cached = await _contactCacheService.GetCachedContactsAsync();
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
                GroupedContacts.Clear();

                foreach (var c in list)
                    Contacts.Add(c);

                if (IsGrouped)
                {
                    var culture = new System.Globalization.CultureInfo("tr-TR");
                    var groups = list
                        .GroupBy(c => GetInitialLetter(c.DisplayName))
                        .OrderBy(g => g.Key == "#" ? "ZZZ" : g.Key, StringComparer.Create(culture, false));

                    foreach (var g in groups)
                    {
                        GroupedContacts.Add(new ContactGroup(g.Key, g));
                    }
                }
            }
            else
            {
                TotalCount = 0;
                Contacts.Clear();
                GroupedContacts.Clear();
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
                    company: string.IsNullOrEmpty(SelectedCompany) ? null : SelectedCompany,
                    sortBy: SortBy);

                if (result != null && result.Items.Count > 0)
                {
                    _currentPage = nextPage;
                    _hasMore = result.HasMore;
                    TotalCount = result.TotalCount;

                    var culture = new System.Globalization.CultureInfo("tr-TR");

                    foreach (var c in result.Items)
                    {
                        Contacts.Add(c);

                        if (IsGrouped)
                        {
                            var letter = GetInitialLetter(c.DisplayName);
                            var group = GroupedContacts.FirstOrDefault(g => g.Key == letter);
                            if (group != null)
                            {
                                group.Add(c);
                            }
                            else
                            {
                                var newGroup = new ContactGroup(letter, new[] { c });
                                int insertIndex = 0;
                                while (insertIndex < GroupedContacts.Count &&
                                       string.Compare(GroupedContacts[insertIndex].Key, letter, culture, System.Globalization.CompareOptions.None) < 0)
                                {
                                    insertIndex++;
                                }
                                GroupedContacts.Insert(insertIndex, newGroup);
                            }
                        }
                    }
                }
                else
                {
                    _hasMore = false;
                }
            }
            catch (ApiOfflineException)
            {
                IsOffline = true;
            }
            catch (ApiException ex)
            {
                if (Application.Current?.Windows[0].Page is Page page)
                {
                    await page.DisplayAlertAsync("Hata", ex.Message, "Tamam");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Daha fazla yükleme hatası: {ex}");
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
                ["SortBy"] = SortBy ?? "Name"
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Arama açılamadı: {ex.Message}");
            }
        }
    }
}
