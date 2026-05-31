using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using ZNext.Services.Dialogs;

namespace ZNext.Services;

internal sealed class SecurityPasswordDialogService
{
	private readonly SecurityAccessService _securityAccessService;

	public SecurityPasswordDialogService(SecurityAccessService securityAccessService)
	{
		_securityAccessService = securityAccessService;
	}

	public async Task<string?> ShowSetPasswordAsync(XamlRoot? xamlRoot)
	{
		if (xamlRoot == null)
		{
			return null;
		}

		PasswordBox passwordBox = CreatePasswordBox("请输入密码");
		PasswordBox confirmPasswordBox = CreatePasswordBox("请再次输入密码");
		TextBlock errorText = CreateErrorText();
		StackPanel panel = CreatePasswordPanel(passwordBox, confirmPasswordBox, errorText);

		ContentDialog dialog = ModernDialogFactory.Create(
			xamlRoot,
			"设置普通密码",
			panel,
			primaryButtonText: "保存",
			closeButtonText: "取消",
			defaultButton: ContentDialogButton.Primary);

		dialog.PrimaryButtonClick += (_, args) =>
		{
			SecurityActionResult validation = _securityAccessService.ValidateNewPassword(
				passwordBox.Password ?? string.Empty,
				confirmPasswordBox.Password ?? string.Empty);
			ShowValidationErrorIfNeeded(validation, errorText, args);
		};

		return await DialogHost.ShowAsync(dialog) == ContentDialogResult.Primary
			? passwordBox.Password
			: null;
	}

	public async Task<string?> ShowResetPasswordAsync(XamlRoot? xamlRoot)
	{
		if (xamlRoot == null)
		{
			return null;
		}

		PasswordBox oldPasswordBox = CreatePasswordBox("请输入原密码");
		PasswordBox newPasswordBox = CreatePasswordBox("请输入新密码");
		PasswordBox confirmPasswordBox = CreatePasswordBox("请再次输入新密码");
		TextBlock errorText = CreateErrorText();
		StackPanel panel = CreatePasswordPanel(oldPasswordBox, newPasswordBox, confirmPasswordBox, errorText);

		ContentDialog dialog = ModernDialogFactory.Create(
			xamlRoot,
			"重置普通密码",
			panel,
			primaryButtonText: "保存",
			closeButtonText: "取消",
			defaultButton: ContentDialogButton.Primary);

		dialog.PrimaryButtonClick += (_, args) =>
		{
			SecurityActionResult validation = _securityAccessService.ValidatePasswordReset(
				oldPasswordBox.Password ?? string.Empty,
				newPasswordBox.Password ?? string.Empty,
				confirmPasswordBox.Password ?? string.Empty);
			ShowValidationErrorIfNeeded(validation, errorText, args);
		};

		return await DialogHost.ShowAsync(dialog) == ContentDialogResult.Primary
			? newPasswordBox.Password
			: null;
	}

	private static PasswordBox CreatePasswordBox(string placeholder)
	{
		return new PasswordBox
		{
			PlaceholderText = placeholder,
			PasswordRevealMode = PasswordRevealMode.Peek
		};
	}

	private static TextBlock CreateErrorText()
	{
		return new TextBlock
		{
			Foreground = new SolidColorBrush(Color.FromArgb(255, 196, 43, 28)),
			Visibility = Visibility.Collapsed
		};
	}

	private static StackPanel CreatePasswordPanel(params UIElement[] children)
	{
		StackPanel panel = new StackPanel { Spacing = 8 };
		foreach (UIElement child in children)
		{
			panel.Children.Add(child);
		}

		return panel;
	}

	private static void ShowValidationErrorIfNeeded(
		SecurityActionResult validation,
		TextBlock errorText,
		ContentDialogButtonClickEventArgs args)
	{
		if (validation.Succeeded)
		{
			errorText.Text = string.Empty;
			errorText.Visibility = Visibility.Collapsed;
			return;
		}

		errorText.Text = validation.Message;
		errorText.Visibility = Visibility.Visible;
		args.Cancel = true;
	}
}
