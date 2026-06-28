using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext.Views.Settings;

public sealed partial class StartupPage : Page, ISettingsBreadcrumbAware, ISettingsPageHostAware
{
	private SettingsSectionView? _host;

	public string Route => SettingsRoutes.Startup;

	public StartupPage()
	{
		InitializeComponent();
	}

	public void Attach(SettingsSectionView host)
	{
		_host = host;
		_host.RegisterStartupControls(AutoStartTunnelListStatusText, AutoStartTunnelsExpander);
	}

	private void AutoStartToggle_Toggled(object sender, RoutedEventArgs e)
	{
		_host?.RaiseAutoStartToggled(sender, e);
	}

	private void AutoStartTunnelsToggle_Toggled(object sender, RoutedEventArgs e)
	{
		_host?.RaiseAutoStartTunnelsToggled(sender, e);
	}
}
