using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext.Views.Dialogs;

public sealed partial class LaunchArgsDialog : ContentDialog
{
	public LaunchArgsDialog()
	{
		InitializeComponent();
		Loaded += LaunchArgsDialog_Loaded;
		PrimaryButtonClick += LaunchArgsDialog_PrimaryButtonClick;
	}

	public string? LaunchArgs { get; private set; }

	public void SetLaunchArgs(string args)
	{
		LaunchArgsTextBox.Text = args ?? string.Empty;
	}

	private void LaunchArgsDialog_Loaded(object sender, RoutedEventArgs e)
	{
		LaunchArgsTextBox.Focus(FocusState.Programmatic);
	}

	private void LaunchArgsDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
	{
		LaunchArgs = LaunchArgsTextBox.Text?.Trim() ?? string.Empty;
	}
}