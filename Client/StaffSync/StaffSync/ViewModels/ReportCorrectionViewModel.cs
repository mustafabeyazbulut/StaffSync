using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StaffSync.Models;
using StaffSync.Services;
using System.Collections.ObjectModel;

namespace StaffSync.ViewModels
{
    public partial class ReportCorrectionViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IContactsService _contactsService;

        public ReportCorrectionViewModel(IContactsService contactsService)
        {
            _contactsService = contactsService;
            FieldOptions = new ObservableCollection<CorrectionFieldOption>();
            SubmittedItems = new ObservableCollection<CorrectionFieldOption>();
        }

        public ObservableCollection<CorrectionFieldOption> FieldOptions { get; }
        public ObservableCollection<CorrectionFieldOption> SubmittedItems { get; }

        [ObservableProperty]
        public partial ContactDto? Contact { get; set; }

        [ObservableProperty]
        public partial string? Note { get; set; }

        [ObservableProperty]
        public partial bool IsBusy { get; set; }

        [ObservableProperty]
        public partial bool IsSubmitted { get; set; }

        [ObservableProperty]
        public partial string SubmitButtonText { get; set; } = "Düzeltme seç";

        [ObservableProperty]
        public partial bool CanSubmit { get; set; } = false;

        [ObservableProperty]
        public partial string? ErrorMessage { get; set; }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        partial void OnErrorMessageChanged(string? value)
        {
            OnPropertyChanged(nameof(HasError));
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Contact", out var cObj) && cObj is ContactDto c)
            {
                Contact = c;
                BuildFieldOptions(c);
            }
        }

        private void BuildFieldOptions(ContactDto c)
        {
            FieldOptions.Clear();
            SubmittedItems.Clear();
            IsSubmitted = false;
            Note = null;
            ErrorMessage = null;

            AddField("TelephoneNumber", "Cep telefonu", c.TelephoneNumber ?? "", "Yeni numara", Keyboard.Telephone);
            AddField("Home", "Kısa kod / dahili", c.Home ?? "", "Yeni dahili", Keyboard.Telephone);
            AddField("Email", "E-posta", c.Email ?? "", "Doğru e-posta", Keyboard.Email);
            AddField("JobTitle", "Unvan", c.JobTitle ?? "", "Doğru unvan", Keyboard.Default);
            AddField("Department", "Departman", c.Department ?? "", "Doğru departman", Keyboard.Default);
            AddField("Office", "Ofis", c.Office ?? "", "Doğru ofis", Keyboard.Default);
            AddField("Company", "Şirket", c.Company ?? "", "Doğru şirket", Keyboard.Default);
            AddField("ManagerName", "Yönetici", c.ManagerName ?? "", "Doğru yönetici", Keyboard.Default);

            UpdateSubmitState();
        }

        private void AddField(string fieldName, string label, string currentValue, string placeholder, Keyboard keyboard)
        {
            var opt = new CorrectionFieldOption
            {
                FieldName = fieldName,
                Label = label,
                CurrentValue = currentValue,
                Placeholder = placeholder,
                Keyboard = keyboard,
                OnChanged = UpdateSubmitState
            };
            FieldOptions.Add(opt);
        }

        public void UpdateSubmitState()
        {
            var readyCount = FieldOptions.Count(f => f.IsSelected && !string.IsNullOrWhiteSpace(f.ProposedValue));

            if (readyCount == 0)
            {
                SubmitButtonText = "Düzeltme seç";
                CanSubmit = false;
            }
            else if (readyCount == 1)
            {
                SubmitButtonText = "1 düzeltmeyi gönder";
                CanSubmit = !IsBusy;
            }
            else
            {
                SubmitButtonText = $"{readyCount} düzeltmeyi gönder";
                CanSubmit = !IsBusy;
            }
        }

        [RelayCommand]
        private async Task CancelAsync()
        {
            if (Shell.Current.Navigation.ModalStack.Count > 0)
                await Shell.Current.Navigation.PopModalAsync();
            else
                await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        private async Task SendCorrectionAsync()
        {
            if (IsBusy || Contact == null)
                return;

            ErrorMessage = null;

            var selectedFields = FieldOptions.Where(f => f.IsSelected).ToList();
            if (selectedFields.Count == 0)
            {
                ErrorMessage = "Lütfen düzeltilmesini istediğiniz en az bir alan seçiniz.";
                return;
            }

            foreach (var f in selectedFields)
            {
                if (string.IsNullOrWhiteSpace(f.ProposedValue))
                {
                    ErrorMessage = $"'{f.Label}' alanı için lütfen doğru/yeni değeri giriniz.";
                    return;
                }

                if (string.Equals(f.ProposedValue.Trim(), f.CurrentValue.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    ErrorMessage = $"'{f.Label}' için girilen yeni değer mevcut değer ile aynı olamaz.";
                    return;
                }
            }

            try
            {
                IsBusy = true;
                UpdateSubmitState();

                var items = selectedFields.Select(f => new CorrectionItemInput
                {
                    FieldName = f.FieldName,
                    NewValue = f.ProposedValue.Trim()
                }).ToList();

                var success = await _contactsService.ReportCorrectionAsync(Contact.Id, Note?.Trim(), items);
                if (success)
                {
                    SubmittedItems.Clear();
                    foreach (var f in selectedFields)
                    {
                        SubmittedItems.Add(f);
                    }
                    IsSubmitted = true;
                }
                else
                {
                    ErrorMessage = "Bildirim gönderilemedi. Lütfen bağlantınızı kontrol edip tekrar deneyiniz.";
                }
            }
            catch (SessionExpiredException)
            {
                await Shell.Current.GoToAsync("//login");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Beklenmeyen bir hata oluştu: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                UpdateSubmitState();
            }
        }
    }
}
