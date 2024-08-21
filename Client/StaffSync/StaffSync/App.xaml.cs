using StaffSync.Pages;

namespace StaffSync
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
           MainPage =new  LoginPage();
        }
    }
}
