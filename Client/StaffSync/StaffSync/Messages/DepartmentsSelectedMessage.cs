using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StaffSync.Messages
{
    public class DepartmentsSelectedMessage : ValueChangedMessage<List<string>>
    {
        public DepartmentsSelectedMessage(List<string> value) : base(value)
        {
        }
    }
}
