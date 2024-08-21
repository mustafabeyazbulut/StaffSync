using StaffSync.Utils;
using System.ComponentModel;

namespace StaffSync.ViewModels
{
    public class SyncSettingViewModel:INotifyPropertyChanged
    {
        public bool IsSyncEnabled
        {
            get => SyncSetting.IsSyncEnabled;
            set
            {
                if (SyncSetting.IsSyncEnabled != value)
                {
                    SyncSetting.IsSyncEnabled = value;
                    OnPropertyChanged(nameof(IsSyncEnabled));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
