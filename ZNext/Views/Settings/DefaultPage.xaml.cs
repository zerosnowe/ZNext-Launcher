using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ZNext.ViewModels;

namespace ZNext.Views.Settings;

public sealed partial class DefaultPage : Page, ISettingsBreadcrumbAware, ISettingsPageHostAware
{
	private SettingsSectionView? _host;

	public string Route => SettingsRoutes.Settings;
	public string GeneralTitle => SettingsPageRegistry.GetTitle(SettingsRoutes.General);
	public string GeneralDescription => SettingsPageRegistry.GetDescription(SettingsRoutes.General);
	public string StartupTitle => SettingsPageRegistry.GetTitle(SettingsRoutes.Startup);
	public string StartupDescription => SettingsPageRegistry.GetDescription(SettingsRoutes.Startup);
	public string CoreTitle => SettingsPageRegistry.GetTitle(SettingsRoutes.Core);
	public string CoreDescription => SettingsPageRegistry.GetDescription(SettingsRoutes.Core);
	public string SecurityTitle => SettingsPageRegistry.GetTitle(SettingsRoutes.Security);
	public string SecurityDescription => SettingsPageRegistry.GetDescription(SettingsRoutes.Security);
	public string AboutTitle => SettingsPageRegistry.GetTitle(SettingsRoutes.About);
	public string AboutDescription => SettingsPageRegistry.GetDescription(SettingsRoutes.About);

	private HomeSectionViewModel? _homeViewModel;

	public HomeSectionViewModel? HomeViewModel
	{
		get => _homeViewModel;
		set
		{
			_homeViewModel = value;
			Bindings.Update();
		}
	}

	public DefaultPage()
	{
		InitializeComponent();
	}

	public void Attach(SettingsSectionView host)
	{
		_host = host;
	}

	public ProfileControl GetProfileControl()
	{
		return SettingsProfileControl;
	}

	private void SettingsCard_Tapped(object sender, TappedRoutedEventArgs e)
	{
		if (sender is FrameworkElement { Tag: string route })
		{
			_host?.NavigateTo(route);
		}
	}

	private void SettingsLogoutButton_Click(object sender, RoutedEventArgs e)
	{
		_host?.RaiseLogoutRequested(sender, e);
	}

	private void SettingsProfileControl_ProfileClicked(object sender, RoutedEventArgs e)
	{
		_host?.NavigateTo(SettingsRoutes.UserCenter);
	}
}
