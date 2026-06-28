using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext.Views.Dialogs;

public sealed partial class FrpcDownloadDialog : ContentDialog
{
	public FrpcDownloadDialog()
	{
		InitializeComponent();
	}

	public void SetStatus(string status)
	{
		StatusText.Text = status;
	}

	public void SetProgress(double value, string? text = null)
	{
		DownloadProgress.IsIndeterminate = false;
		DownloadProgress.Value = value;
		if (!string.IsNullOrEmpty(text))
		{
			ProgressText.Text = text;
			ProgressText.Visibility = Visibility.Visible;
		}
	}

	public void SetIndeterminate()
	{
		DownloadProgress.IsIndeterminate = true;
		ProgressText.Visibility = Visibility.Collapsed;
	}

	public void SetError(string message)
	{
		StatusText.Text = message;
		DownloadProgress.ShowError = true;
		DownloadProgress.IsIndeterminate = false;
		DownloadProgress.Value = 0;
		IsPrimaryButtonEnabled = false;
	}
}