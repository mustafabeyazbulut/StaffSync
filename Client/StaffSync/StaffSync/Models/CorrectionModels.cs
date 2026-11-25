using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StaffSync.Models
{
    public class CreateContactCorrectionRequest
    {
        public string? Note { get; set; }
        public List<CorrectionItemInput> Items { get; set; } = new();
    }

    public class CorrectionItemInput
    {
        public string FieldName { get; set; } = "";
        public string NewValue { get; set; } = "";
    }

    public partial class CorrectionFieldOption : ObservableObject
    {
        public string FieldName { get; set; } = "";
        public string Label { get; set; } = "";
        public string CurrentValue { get; set; } = "";
        public string Placeholder { get; set; } = "";
        public Keyboard Keyboard { get; set; } = Keyboard.Default;

        public string CurrentValueDisplay => string.IsNullOrWhiteSpace(CurrentValue) ? "(Boş)" : CurrentValue;

        [ObservableProperty]
        public partial string ProposedValue { get; set; } = "";

        [ObservableProperty]
        public partial bool IsSelected { get; set; }

        public Action? OnChanged { get; set; }

        partial void OnIsSelectedChanged(bool value)
        {
            OnChanged?.Invoke();
        }

        partial void OnProposedValueChanged(string value)
        {
            OnChanged?.Invoke();
        }

        [RelayCommand]
        private void Toggle()
        {
            IsSelected = !IsSelected;
        }
    }
}
