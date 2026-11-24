using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StaffSync.Messages
{
    public class FilterCriteria
    {
        public List<string> SelectedDepartments { get; set; } = new();
        public string? SelectedCompany { get; set; }
        public string? SortBy { get; set; }
    }

    public class FilterChangedMessage : ValueChangedMessage<FilterCriteria>
    {
        public FilterChangedMessage(FilterCriteria value) : base(value)
        {
        }
    }
}
