using System;
using System.Threading.Tasks;

namespace ZNext.Services;

internal sealed class LauncherUpdateCoordinatorService
{
	private readonly LauncherUpdateService _launcherUpdateService;
	private readonly ExternalLauncherService _externalLauncherService;
	private readonly AppVersionService _appVersionService;

	public LauncherUpdateCoordinatorService(
		LauncherUpdateService launcherUpdateService,
		ExternalLauncherService externalLauncherService,
		AppVersionService appVersionService)
	{
		_launcherUpdateService = launcherUpdateService;
		_externalLauncherService = externalLauncherService;
		_appVersionService = appVersionService;
	}

	public async Task<LauncherUpdateActionResult> CheckAndOpenLatestAsync()
	{
		try
		{
			LauncherUpdateResult update = await _launcherUpdateService.GetLatestLauncherZipUrlAsync();
			if (!update.Success || string.IsNullOrWhiteSpace(update.Url))
			{
				return LauncherUpdateActionResult.Dialog("获取更新失败", update.Message);
			}

			if (update.LatestVersion != null
				&& _appVersionService.TryGetCurrentVersion(out Version? currentVersion)
				&& currentVersion != null
				&& currentVersion >= update.LatestVersion)
			{
				return LauncherUpdateActionResult.Dialog("更新提示", $"已是最新版本（当前 {currentVersion}，远端 {update.LatestVersion}）。");
			}

			_externalLauncherService.OpenUrl(update.Url);
			return LauncherUpdateActionResult.Toast("已打开浏览器下载最新版本");
		}
		catch (Exception ex)
		{
			return LauncherUpdateActionResult.Dialog("获取更新失败", ex.Message);
		}
	}
}

internal sealed record LauncherUpdateActionResult(bool ShowDialog, string Title, string Message, string? ToastMessage)
{
	public static LauncherUpdateActionResult Dialog(string title, string message)
	{
		return new LauncherUpdateActionResult(true, title, message, null);
	}

	public static LauncherUpdateActionResult Toast(string message)
	{
		return new LauncherUpdateActionResult(false, string.Empty, string.Empty, message);
	}
}
