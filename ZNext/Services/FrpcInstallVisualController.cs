using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ZNext.Services;

internal sealed class FrpcInstallVisualController
{
	public void SetButtonsState(Button? installButton, bool isBusy)
	{
		if (installButton != null)
		{
			installButton.IsEnabled = !isBusy;
		}
	}

	public void SetBusyState(ProgressRing? busyRing, TextBlock? busyText, bool isBusy)
	{
		Visibility visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
		if (busyRing != null)
		{
			busyRing.IsActive = isBusy;
			busyRing.Visibility = visibility;
		}
		if (busyText != null)
		{
			busyText.Visibility = visibility;
		}
	}

	public void UpdateInstallButton(Button? installButton, bool isInstalled)
	{
		if (installButton == null)
		{
			return;
		}

		if (isInstalled)
		{
			installButton.Content = "删除";
			installButton.Background = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 196, 43, 28));
			installButton.Foreground = new SolidColorBrush(Colors.White);
			installButton.BorderBrush = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 173, 41, 28));
			return;
		}

		installButton.Content = "安装 frpc";
		installButton.ClearValue(Control.BackgroundProperty);
		installButton.ClearValue(Control.ForegroundProperty);
		installButton.ClearValue(Control.BorderBrushProperty);
	}

	public void UpdateStatusIcon(FontIcon? statusIcon, bool isInstalled, bool isError = false)
	{
		if (statusIcon == null)
		{
			return;
		}

		if (isError)
		{
			statusIcon.Glyph = "\uea39";
			statusIcon.Foreground = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 196, 43, 28));
			return;
		}

		if (isInstalled)
		{
			statusIcon.Glyph = "\ue73e";
			statusIcon.Foreground = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 15, 133, 72));
			return;
		}

		statusIcon.Glyph = "\ue946";
		statusIcon.Foreground = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 96, 96, 96));
	}
}
