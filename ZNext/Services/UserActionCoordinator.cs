using System;
using IOPath = System.IO.Path;

namespace ZNext.Services;

internal sealed class UserActionCoordinator
{
	private readonly ExternalLauncherService _externalLauncherService;
	private readonly ClipboardService _clipboardService;
	private readonly FrpcManagerService _frpcManagerService;

	public UserActionCoordinator(
		ExternalLauncherService externalLauncherService,
		ClipboardService clipboardService,
		FrpcManagerService frpcManagerService)
	{
		_externalLauncherService = externalLauncherService;
		_clipboardService = clipboardService;
		_frpcManagerService = frpcManagerService;
	}

	public UserActionResult OpenHelpLink(string? link)
	{
		if (string.IsNullOrWhiteSpace(link))
		{
			return UserActionResult.Skipped();
		}

		try
		{
			_externalLauncherService.OpenUrl(link);
			return UserActionResult.Skipped();
		}
		catch (Exception ex)
		{
			return UserActionResult.Dialog("打开失败", "无法打开链接：" + ex.Message);
		}
	}

	public UserActionResult CopyHelpText(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return UserActionResult.Skipped();
		}

		try
		{
			_clipboardService.SetText(text);
			return UserActionResult.Dialog("已复制", "群号 " + text + " 已复制到剪贴板。");
		}
		catch (Exception ex)
		{
			return UserActionResult.Dialog("复制失败", ex.Message);
		}
	}

	public UserActionResult OpenFrpcDirectory()
	{
		try
		{
			string directory = IOPath.GetDirectoryName(_frpcManagerService.GetExecutablePath())
				?? _frpcManagerService.GetApplicationRootDirectory();
			_externalLauncherService.OpenDirectory(directory);
			return UserActionResult.Skipped();
		}
		catch (Exception ex)
		{
			return UserActionResult.Dialog("打开失败", "无法打开 frpc 目录：" + ex.Message);
		}
	}
}

internal sealed record UserActionResult(bool ShouldShowDialog, string Title, string Message)
{
	public static UserActionResult Skipped()
	{
		return new UserActionResult(false, string.Empty, string.Empty);
	}

	public static UserActionResult Dialog(string title, string message)
	{
		return new UserActionResult(true, title, message);
	}
}
