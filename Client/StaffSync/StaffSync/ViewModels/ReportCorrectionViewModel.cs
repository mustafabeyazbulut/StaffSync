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
        }

        public ObservableCollection<CorrectionFieldOption> FieldOptions { get; }

        [ObservableProperty]
        public partial ContactDto? Contact { get; set; }

        [ObservableProperty]
        public partial string? Note { get; set; }

        [ObservableProperty]
        public partial bool IsBusy { get; set; }

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
            FieldOptions.Add(new CorrectionFieldOption { FieldName = "DisplayName", Label = "Ad soyad", CurrentValue = c.DisplayName });
            FieldOptions.Add(new CorrectionFieldOption { FieldName = "TelephoneNumber", Label = "Cep telefonu", CurrentValue = c.TelephoneNumber ?? "" });
            FieldOptions.Add(new CorrectionFieldOption { FieldName = "Home", Label = "Kısa kod / dahili", CurrentValue = c.Home ?? "" });
            FieldOptions.Add(new CorrectionFieldOption { FieldName = "Email", Label = "E-posta", CurrentValue = c.Email ?? "" });
            FieldOptions.Add(new CorrectionFieldOption { FieldName = "JobTitle", Label = "Unvan", CurrentValue = c.JobTitle ?? "" });
            FieldOptions.Add(new CorrectionFieldOption { FieldName = "Department", Label = "Departman", CurrentValue = c.Department ?? "" });
            FieldOptions.Add(new CorrectionFieldOption { FieldName = "Office", Label = "Ofis", CurrentValue = c.Office ?? "" });
            FieldOptions.Add(new CorrectionFieldOption { FieldName = "Company", Label = "Şirket", CurrentValue = c.Company ?? "" });
            FieldOptions.Add(new CorrectionFieldOption { FieldName = "ManagerName", Label = "Yönetici", CurrentValue = c.ManagerName ?? "" });
            FieldOptions.Add(new CorrectionFieldOption { FieldName = "Other", Label = "Diğer", CurrentValue = "" });
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

                if (f.FieldName != "Other" && string.Equals(f.ProposedValue.Trim(), f.CurrentValue.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    ErrorMessage = $"'{f.Label}' için girilen yeni değer mevcut değer ile aynı olamaz.";
                    return;
                }
            }

            try
            {
                IsBusy = true;
                var items = selectedFields.Select(f => new CorrectionItemInput
                {
                    FieldName = f.FieldName,
                    NewValue = f.ProposedValue.Trim()
                }).ToList();

                var success = await _contactsService.ReportCorrectionAsync(Contact.Id, Note?.Trim(), items);
                if (success)
                {
                    if (Application.Current?.Windows[0].Page is Page page)
                    {
                        await page.DisplayAlertAsync(
                            "Bildirim Alındı",
                            "Düzeltme bildiriminiz başarıyla iletildi. Yetkili onayından sonra rehberde güncellenecektir.",
                            "Tamam");
                    }
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    ErrorMessage = "Bildirim gönderilemedi. Lütfen bağlantınızı kontrol edip tekrar deneyiniz.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Bir hata oluştu: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
