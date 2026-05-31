using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ZNext.Services.Dialogs;
using ZNext.Views.Dialogs;

namespace ZNext.Services;

internal sealed class LoginDialogFlowService
{
	private readonly Func<XamlRoot?> _xamlRootProvider;
	private readonly UserSessionService _userSessionService;

	public LoginDialogFlowService(
		Func<XamlRoot?> xamlRootProvider,
		UserSessionService userSessionService)
	{
		_xamlRootProvider = xamlRootProvider;
		_userSessionService = userSessionService;
	}

	public async Task<bool> ShowAsync(bool exitOnCancel)
	{
		XamlRoot? xamlRoot = await WaitForXamlRootAsync();

		try
		{
			if (xamlRoot == null)
			{
				await ShowFallbackDialogAsync("错误", "无法获取 XamlRoot，请重试", null);
				ExitIfRequested(exitOnCancel);
				return false;
			}

			LoginDialog loginDialog = new LoginDialog
			{
				XamlRoot = xamlRoot
			};

			await DialogHost.ShowAsync(loginDialog);
			string? token = loginDialog.Token;
			if (string.IsNullOrWhiteSpace(token))
			{
				ExitIfRequested(exitOnCancel);
				return false;
			}

			_userSessionService.SetToken(token, persist: true);
			return true;
		}
		catch (Exception ex)
		{
			Debug.WriteLine("登录错误: " + ex.Message);
			await ShowFallbackDialogAsync("错误", "登录失败: " + ex.Message, xamlRoot);
			ExitIfRequested(exitOnCancel);
			return false;
		}
	}

	private async Task<XamlRoot?> WaitForXamlRootAsync()
	{
		XamlRoot? xamlRoot = _xamlRootProvider();
		if (xamlRoot != null)
		{
			return xamlRoot;
		}

		await Task.Delay(120);
		return _xamlRootProvider();
	}

	private static async Task ShowFallbackDialogAsync(string title, string content, XamlRoot? xamlRoot)
	{
		ContentDialog dialog = ModernDialogFactory.Create(
			xamlRoot,
			title,
			content,
			closeButtonText: "确定",
			defaultButton: ContentDialogButton.Close);
		await DialogHost.ShowAsync(dialog);
	}

	private static void ExitIfRequested(bool exitOnCancel)
	{
		if (exitOnCancel)
		{
			Application.Current.Exit();
		}
	}
}
