using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext.Views;

public sealed partial class HomeSectionView : UserControl
{
	public HomeSectionView()
	{
		InitializeComponent();
	}

	public event RoutedEventHandler? AnnouncementRequested;
	public event RoutedEventHandler? SignInRequested;
	public event RoutedEventHandler? CloseImportantAnnouncementRequested;
	public event RoutedEventHandler? LoginRequested;
	public event RoutedEventHandler? LogoutRequested;

	private void HomeBannerAnnouncementButton_Click(object sender, RoutedEventArgs e)
	{
		AnnouncementRequested?.Invoke(sender, e);
	}

	private void HomeBannerSignInButton_Click(object sender, RoutedEventArgs e)
	{
		SignInRequested?.Invoke(sender, e);
	}

	private void CloseImportantAnnouncementButton_Click(object sender, RoutedEventArgs e)
	{
		CloseImportantAnnouncementRequested?.Invoke(sender, e);
	}

	private void SignInButton_Click(object sender, RoutedEventArgs e)
	{
		SignInRequested?.Invoke(sender, e);
	}

	private void LoginButton_Click(object sender, RoutedEventArgs e)
	{
		LoginRequested?.Invoke(sender, e);
	}

	private void LogoutButton_Click(object sender, RoutedEventArgs e)
	{
		LogoutRequested?.Invoke(sender, e);
	}
}
