using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext.Views;

public sealed partial class SettingsSectionView : UserControl
{
	public SettingsSectionView()
	{
		InitializeComponent();
	}

	public event RoutedEventHandler? AutoStartToggled;
	public event RoutedEventHandler? AutoStartTunnelsToggled;
	public event RoutedEventHandler? UploadAvatarRequested;
	public event RoutedEventHandler? FrpcInstallRequested;
	public event RoutedEventHandler? FrpcOpenDirectoryRequested;
	public event RoutedEventHandler? SecurityPasswordToggled;
	public event RoutedEventHandler? SetSecurityPasswordRequested;
	public event RoutedEventHandler? FetchUpdateRequested;

	private void AutoStartToggle_Toggled(object sender, RoutedEventArgs e)
	{
		AutoStartToggled?.Invoke(sender, e);
	}

	private void AutoStartTunnelsToggle_Toggled(object sender, RoutedEventArgs e)
	{
		AutoStartTunnelsToggled?.Invoke(sender, e);
	}

	private void UploadAvatarButton_Click(object sender, RoutedEventArgs e)
	{
		UploadAvatarRequested?.Invoke(sender, e);
	}

	private void FrpcInstallButton_Click(object sender, RoutedEventArgs e)
	{
		FrpcInstallRequested?.Invoke(sender, e);
	}

	private void FrpcOpenDirectoryButton_Click(object sender, RoutedEventArgs e)
	{
		FrpcOpenDirectoryRequested?.Invoke(sender, e);
	}

	private void SecurityPasswordToggle_Toggled(object sender, RoutedEventArgs e)
	{
		SecurityPasswordToggled?.Invoke(sender, e);
	}

	private void SetSecurityPasswordButton_Click(object sender, RoutedEventArgs e)
	{
		SetSecurityPasswordRequested?.Invoke(sender, e);
	}

	private void FetchUpdateButton_Click(object sender, RoutedEventArgs e)
	{
		FetchUpdateRequested?.Invoke(sender, e);
	}
}
