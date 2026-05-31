using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ZNext.Services.Dialogs;

namespace ZNext.Services;

internal sealed class UserDialogService
{
	private readonly Func<XamlRoot?> _xamlRootProvider;

	public UserDialogService(Func<XamlRoot?> xamlRootProvider)
	{
		_xamlRootProvider = xamlRootProvider;
	}

	public async Task ShowInfoAsync(string title, string content)
	{
		XamlRoot? xamlRoot = _xamlRootProvider();
		if (xamlRoot == null)
		{
			return;
		}

		await ShowWithRetryAsync(
			() => ModernDialogFactory.Create(
				xamlRoot,
				title,
				content,
				closeButtonText: "确定",
				defaultButton: ContentDialogButton.Close));
	}

	public async Task<bool> ShowConfirmAsync(string title, string content)
	{
		XamlRoot? xamlRoot = _xamlRootProvider();
		if (xamlRoot == null)
		{
			return false;
		}

		ContentDialogResult result = await ShowWithRetryAsync(
			() => ModernDialogFactory.Create(
				xamlRoot,
				title,
				content,
				primaryButtonText: "确认",
				closeButtonText: "取消",
				defaultButton: ContentDialogButton.Close));

		return result == ContentDialogResult.Primary;
	}

	public async Task<bool> ShowPrimaryAsync(string title, string content, string primaryButtonText)
	{
		XamlRoot? xamlRoot = _xamlRootProvider();
		if (xamlRoot == null)
		{
			return false;
		}

		ContentDialogResult result = await ShowWithRetryAsync(
			() => ModernDialogFactory.Create(
				xamlRoot,
				title,
				content,
				primaryButtonText: primaryButtonText,
				closeButtonText: "取消",
				defaultButton: ContentDialogButton.Primary));

		return result == ContentDialogResult.Primary;
	}

	private static async Task<ContentDialogResult> ShowWithRetryAsync(Func<ContentDialog> dialogFactory)
	{
		try
		{
			return await DialogHost.ShowAsync(dialogFactory());
		}
		catch (COMException ex) when (IsSingleDialogOpenException(ex))
		{
			await Task.Delay(160);
			return await DialogHost.ShowAsync(dialogFactory());
		}
	}

	private static bool IsSingleDialogOpenException(COMException ex)
	{
		return ex.Message.Contains("Only a single ContentDialog can be open", StringComparison.OrdinalIgnoreCase);
	}
}
