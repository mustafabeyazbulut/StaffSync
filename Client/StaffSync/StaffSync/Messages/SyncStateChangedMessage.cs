using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StaffSync.Messages
{
    /// <summary>
    /// Telefon rehberi senkronu başladığında (true) ve bittiğinde (false) gönderilir.
    /// Açılış/arka plan senkronu sürerken açık olan ekranlar durumu buradan günceller.
    /// </summary>
    public class SyncStateChangedMessage : ValueChangedMessage<bool>
    {
        public SyncStateChangedMessage(bool isRunning) : base(isRunning)
        {
        }
    }
}
