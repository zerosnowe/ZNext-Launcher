using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ZNext.Services;

internal sealed class TitleBarVisualController
{
	public void UpdateBackButton(
		Button? backButton,
		FontIcon? backIcon,
		AppWindow? appWindow,
		FrameworkElement? root,
		string? currentTag)
	{
		if (backButton == null)
		{
			return;
		}

		bool canGoBack = !string.Equals(currentTag, "Home", StringComparison.OrdinalIgnoreCase);
		backButton.Visibility = Visibility.Visible;
		backButton.IsEnabled = canGoBack;
		backButton.Opacity = canGoBack ? 1.0 : 0.45;
		UpdateVisuals(backButton, backIcon, appWindow, root);
	}

	public void UpdateVisuals(Button? backButton, FontIcon? backIcon, AppWindow? appWindow, FrameworkElement? root)
	{
		bool isDark = root?.ActualTheme == ElementTheme.Dark;
		bool canGoBack = backButton?.IsEnabled ?? false;
		if (backIcon != null)
		{
			backIcon.Foreground = new SolidColorBrush(GetBackIconColor(isDark, canGoBack));
		}

		if (appWindow?.TitleBar == null)
		{
			return;
		}

		if (isDark)
		{
			appWindow.TitleBar.ButtonForegroundColor = Color.FromArgb(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
			appWindow.TitleBar.ButtonInactiveForegroundColor = Color.FromArgb(byte.MaxValue, 170, 170, 170);
			appWindow.TitleBar.ButtonHoverForegroundColor = Color.FromArgb(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
			appWindow.TitleBar.ButtonPressedForegroundColor = Color.FromArgb(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
			appWindow.TitleBar.ButtonBackgroundColor = Color.FromArgb(0, 0, 0, 0);
			appWindow.TitleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 0, 0, 0);
			appWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(40, byte.MaxValue, byte.MaxValue, byte.MaxValue);
			appWindow.TitleBar.ButtonPressedBackgroundColor = Color.FromArgb(70, byte.MaxValue, byte.MaxValue, byte.MaxValue);
			return;
		}

		appWindow.TitleBar.ButtonForegroundColor = Color.FromArgb(byte.MaxValue, 31, 31, 31);
		appWindow.TitleBar.ButtonInactiveForegroundColor = Color.FromArgb(byte.MaxValue, 138, 138, 138);
		appWindow.TitleBar.ButtonHoverForegroundColor = Color.FromArgb(byte.MaxValue, 31, 31, 31);
		appWindow.TitleBar.ButtonPressedForegroundColor = Color.FromArgb(byte.MaxValue, 31, 31, 31);
		appWindow.TitleBar.ButtonBackgroundColor = Color.FromArgb(0, 0, 0, 0);
		appWindow.TitleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 0, 0, 0);
		appWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(12, 0, 0, 0);
		appWindow.TitleBar.ButtonPressedBackgroundColor = Color.FromArgb(24, 0, 0, 0);
	}

	private static Color GetBackIconColor(bool isDark, bool canGoBack)
	{
		if (isDark)
		{
			return canGoBack
				? Color.FromArgb(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue)
				: Color.FromArgb(byte.MaxValue, 160, 160, 160);
		}

		return canGoBack
			? ColorFromHex("#1F1F1F")
			: ColorFromHex("#8A8A8A");
	}

	private static Color ColorFromHex(string hex)
	{
		if (string.IsNullOrWhiteSpace(hex))
		{
			return Color.FromArgb(0, 0, 0, 0);
		}

		string text = hex.TrimStart('#');
		if (text.Length == 6)
		{
			byte r = Convert.ToByte(text.Substring(0, 2), 16);
			byte g = Convert.ToByte(text.Substring(2, 2), 16);
			byte b = Convert.ToByte(text.Substring(4, 2), 16);
			return Color.FromArgb(byte.MaxValue, r, g, b);
		}
		if (text.Length == 8)
		{
			byte a = Convert.ToByte(text.Substring(0, 2), 16);
			byte r = Convert.ToByte(text.Substring(2, 2), 16);
			byte g = Convert.ToByte(text.Substring(4, 2), 16);
			byte b = Convert.ToByte(text.Substring(6, 2), 16);
			return Color.FromArgb(a, r, g, b);
		}

		return Color.FromArgb(0, 0, 0, 0);
	}
}
