using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext.Views.Settings;

public sealed partial class SecurityPage : Page, ISettingsBreadcrumbAware, ISettingsPageHostAware
{
	private SettingsSectionView? _host;

	public string Route => SettingsRoutes.Security;

	public SecurityPage()
	{
		InitializeComponent();
	}

	public void Attach(SettingsSectionView host)
	{
		_host = host;
	}

	private void SecurityPasswordToggle_Toggled(object sender, RoutedEventArgs e)
	{
		_host?.RaiseSecurityPasswordToggled(sender, e);
	}

	private void SetSecurityPasswordButton_Click(object sender, RoutedEventArgs e)
	{
		_host?.RaiseSetSecurityPasswordRequested(sender, e);
	}
}
