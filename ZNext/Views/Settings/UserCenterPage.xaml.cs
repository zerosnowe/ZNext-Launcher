using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ZNext.ViewModels;
using ZNext.Views;

namespace ZNext.Views.Settings;

public sealed partial class UserCenterPage : Page, ISettingsBreadcrumbAware, ISettingsPageHostAware
{
	private HomeSectionViewModel? _homeViewModel;
	private SettingsSectionView? _host;

	public UserCenterPage()
	{
		InitializeComponent();
	}

	public string Route => SettingsRoutes.UserCenter;

	public HomeSectionViewModel? HomeViewModel
	{
		get => _homeViewModel;
		set
		{
			_homeViewModel = value;
			Bindings.Update();
			UpdateProfileText();
		}
	}

	public ProfileControl GetProfileControl()
	{
		return UserCenterProfileControl;
	}

	public void Attach(SettingsSectionView host)
	{
		_host = host;
	}

	private void UpdateProfileText()
	{
		if (_homeViewModel == null)
		{
			return;
		}

		UserCenterProfileControl.SetProfileText(_homeViewModel.UsernameText, _homeViewModel.EmailText);
	}

	private void UserCenterActionButton_Click(object sender, RoutedEventArgs e)
	{
		if ((sender as FrameworkElement)?.Tag is string action)
		{
			_host?.RaiseUserCenterActionRequested(action);
		}
	}

	private void CopyAccessTokenButton_Click(object sender, RoutedEventArgs e)
	{
		_host?.RaiseCopyAccessTokenRequested(sender, e);
	}
}
