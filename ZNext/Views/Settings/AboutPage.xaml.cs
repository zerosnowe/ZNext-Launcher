using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace ZNext.Views.Settings;

public sealed partial class AboutPage : Page, ISettingsBreadcrumbAware, ISettingsPageHostAware
{
	private SettingsSectionView? _host;

	public string Route => SettingsRoutes.About;

	public AboutPage()
	{
		InitializeComponent();
	}

	public void Attach(SettingsSectionView host)
	{
		_host = host;
	}

	private void FetchUpdateButton_Click(object sender, RoutedEventArgs e)
	{
		_host?.RaiseFetchUpdateRequested(sender, e);
	}

	private async void ExternalLinkSettingsCard_Tapped(object sender, TappedRoutedEventArgs e)
	{
		if (sender is FrameworkElement { Tag: string url } && Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
		{
			await Launcher.LaunchUriAsync(uri);
		}
	}
}
