using CommunityToolkit.Mvvm.ComponentModel;

namespace StaffSync.Models
{
    public partial class FilterOptionItem : ObservableObject
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }

        [ObservableProperty]
        public partial bool IsSelected { get; set; }
    }

    public class ContactFilterOptionsResult
    {
        public List<FilterOptionItem> Departments { get; set; } = new();
        public List<FilterOptionItem> Companies { get; set; } = new();
    }
}
