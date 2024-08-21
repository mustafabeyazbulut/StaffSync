using StaffSync.ViewModels;

namespace StaffSync.Pages;

public partial class ProfilePage : ContentPage
{
	public ProfilePage()
	{
		InitializeComponent();
        BindingContext = new SyncSettingViewModel();
    }

    private void MainButton_Clicked(object sender, EventArgs e)
    {
        App.Current.MainPage = new MainPage();
    }

    private void ProfileButton_Clicked(object sender, EventArgs e)
    {
        App.Current.MainPage = new ProfilePage();

    }
}