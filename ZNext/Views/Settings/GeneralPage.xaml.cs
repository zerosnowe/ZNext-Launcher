using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext.Views.Settings;

public sealed partial class GeneralPage : Page, ISettingsBreadcrumbAware, ISettingsPageHostAware
{
	private SettingsSectionView? _host;

	public string Route => SettingsRoutes.General;

	public GeneralPage()
	{
		InitializeComponent();
	}

	public void Attach(SettingsSectionView host)
	{
		_host = host;
		_host.RegisterGeneralControls(AvatarStatusText);
	}

	private void UploadAvatarButton_Click(object sender, RoutedEventArgs e)
	{
		_host?.RaiseUploadAvatarRequested(sender, e);
	}

	private void ClearAvatarButton_Click(object sender, RoutedEventArgs e)
	{
		_host?.RaiseClearAvatarRequested(sender, e);
	}

	private void SelectCustomBackgroundButton_Click(object sender, RoutedEventArgs e)
	{
		_host?.RaiseSelectCustomBackgroundRequested(sender, e);
	}

	private void ClearCustomBackgroundButton_Click(object sender, RoutedEventArgs e)
	{
		_host?.RaiseClearCustomBackgroundRequested(sender, e);
	}
}
