using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext.Services;

internal sealed class SecurityAccessVisualController
{
	public void ShowLockOverlay(
		FrameworkElement? overlay,
		Control? navigationHost,
		PasswordBox? passwordBox,
		TextBlock? errorText,
		DispatcherQueue dispatcherQueue)
	{
		if (overlay != null)
		{
			overlay.Visibility = Visibility.Visible;
		}

		if (navigationHost != null)
		{
			navigationHost.IsEnabled = false;
		}

		ClearUnlockError(passwordBox, errorText);

		if (passwordBox != null)
		{
			dispatcherQueue.TryEnqueue(() => passwordBox.Focus(FocusState.Programmatic));
		}
	}

	public void HideLockOverlay(FrameworkElement? overlay, Control? navigationHost)
	{
		if (overlay != null)
		{
			overlay.Visibility = Visibility.Collapsed;
		}

		if (navigationHost != null)
		{
			navigationHost.IsEnabled = true;
		}
	}

	public void ClearUnlockError(PasswordBox? passwordBox, TextBlock? errorText)
	{
		if (passwordBox != null)
		{
			passwordBox.Password = string.Empty;
		}

		if (errorText != null)
		{
			errorText.Text = string.Empty;
			errorText.Visibility = Visibility.Collapsed;
		}
	}

	public void ShowUnlockError(TextBlock? errorText, string message)
	{
		if (errorText != null)
		{
			errorText.Text = message;
			errorText.Visibility = Visibility.Visible;
		}
	}
}
