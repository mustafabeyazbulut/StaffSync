using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StaffSync.Models
{
    public partial class FilterOptionItem : ObservableObject
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }

        public string CountFormatted => $"{Count} kişi";

        [ObservableProperty]
        public partial bool IsSelected { get; set; }

        public Action? OnSelectionChanged { get; set; }

        partial void OnIsSelectedChanged(bool value)
        {
            OnSelectionChanged?.Invoke();
        }

        [RelayCommand]
        private void Toggle()
        {
            IsSelected = !IsSelected;
        }
    }

    public class ContactFilterOptionsResult
    {
        public List<FilterOptionItem> Departments { get; set; } = new();
        public List<FilterOptionItem> Companies { get; set; } = new();
    }
}
