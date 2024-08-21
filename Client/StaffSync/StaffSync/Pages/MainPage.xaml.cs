namespace StaffSync.Pages;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

    private void ProfileButton_Clicked(object sender, EventArgs e)
    {
        App.Current.MainPage = new ProfilePage();

    }

    private void MainButton_Clicked(object sender, EventArgs e)
    {
        App.Current.MainPage= this;
    }
}