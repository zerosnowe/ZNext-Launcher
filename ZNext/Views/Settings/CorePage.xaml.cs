using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext.Views.Settings;

public sealed partial class CorePage : Page, ISettingsBreadcrumbAware, ISettingsPageHostAware
{
	private SettingsSectionView? _host;

	public string Route => SettingsRoutes.Core;

	public CorePage()
	{
		InitializeComponent();
	}

	public void Attach(SettingsSectionView host)
	{
		_host = host;
		_host.RegisterCoreControls(FrpcInstallBusyRing, FrpcInstallBusyText, FrpcInstallButton, FrpcStatusIcon);
	}

	private void FrpcInstallButton_Click(object sender, RoutedEventArgs e)
	{
		_host?.RaiseFrpcInstallRequested(sender, e);
	}

	private void FrpcOpenDirectoryButton_Click(object sender, RoutedEventArgs e)
	{
		_host?.RaiseFrpcOpenDirectoryRequested(sender, e);
	}

	private void LaunchArgsButton_Click(object sender, RoutedEventArgs e)
	{
		_host?.RaiseLaunchArgsRequested(sender, e);
	}
}
