using CommunityToolkit.Mvvm.ComponentModel;

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

        [ObservableProperty]
        public partial string ProposedValue { get; set; } = "";

        [ObservableProperty]
        public partial bool IsSelected { get; set; }
    }
}
