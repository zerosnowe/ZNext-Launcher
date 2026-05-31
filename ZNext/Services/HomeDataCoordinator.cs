using System.Diagnostics;

namespace ZNext.Services;

internal sealed class HomeDataCoordinator
{
	private readonly UserInfoService _userInfoService;
	private readonly SystemStatusService _systemStatusService;
	private readonly AnnouncementService _announcementService;
	private string? _lastImportantAnnouncementContent;
	private bool _isImportantAnnouncementDismissed;

	public HomeDataCoordinator(
		UserInfoService userInfoService,
		SystemStatusService systemStatusService,
		AnnouncementService announcementService)
	{
		_userInfoService = userInfoService;
		_systemStatusService = systemStatusService;
		_announcementService = announcementService;
	}

	public async Task<HomeUserInfoLoadResult> LoadUserInfoAsync()
	{
		try
		{
			UserInfoData? cachedUserInfo = await _userInfoService.GetCachedUserInfoAsync();
			UserInfoResult userInfoResult = await _userInfoService.GetUserInfoAsync();
			return new HomeUserInfoLoadResult(
				cachedUserInfo,
				userInfoResult.Success ? userInfoResult.UserInfo : null,
				ShouldMarkLoggedIn: true);
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Update user info failed: " + ex.Message);
			return HomeUserInfoLoadResult.Empty;
		}
	}

	public async Task<HomeSystemStatusLoadResult> LoadSystemStatusAsync()
	{
		try
		{
			SystemStatusResult result = await _systemStatusService.GetSystemStatusAsync();
			if (!result.Success || result.Status == null)
			{
				return HomeSystemStatusLoadResult.Unavailable;
			}

			bool isHealthy = result.Status.Status == 0;
			string title = isHealthy ? "系统正常运行" : "系统状态异常";
			string remark = !string.IsNullOrWhiteSpace(result.Status.Remark)
				? result.Status.Remark
				: isHealthy ? "ME Frp 当前服务一切正常!" : "服务非正常状态";
			return new HomeSystemStatusLoadResult(isHealthy, title, remark);
		}
		catch
		{
			return HomeSystemStatusLoadResult.Unavailable;
		}
	}

	public async Task<HomeImportantAnnouncementLoadResult> LoadImportantAnnouncementAsync()
	{
		try
		{
			string markdownContent = await _announcementService.GetPopupAnnouncementAsync();
			if (IsMissingAnnouncement(markdownContent))
			{
				return HomeImportantAnnouncementLoadResult.Hidden;
			}

			if (!string.Equals(markdownContent, _lastImportantAnnouncementContent, StringComparison.Ordinal))
			{
				_lastImportantAnnouncementContent = markdownContent;
				_isImportantAnnouncementDismissed = false;
			}

			return _isImportantAnnouncementDismissed
				? HomeImportantAnnouncementLoadResult.Hidden
				: HomeImportantAnnouncementLoadResult.Visible(markdownContent);
		}
		catch
		{
			return HomeImportantAnnouncementLoadResult.Hidden;
		}
	}

	public void DismissImportantAnnouncement()
	{
		_isImportantAnnouncementDismissed = true;
	}

	private static bool IsMissingAnnouncement(string markdownContent)
	{
		return string.IsNullOrWhiteSpace(markdownContent)
			|| string.Equals(markdownContent, "NO_ANNOUNCEMENT", StringComparison.Ordinal)
			|| markdownContent.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase);
	}
}

internal sealed record HomeUserInfoLoadResult(
	UserInfoData? CachedUser,
	UserInfoData? RemoteUser,
	bool ShouldMarkLoggedIn)
{
	public static readonly HomeUserInfoLoadResult Empty = new(null, null, false);
}

internal sealed record HomeSystemStatusLoadResult(bool IsHealthy, string Title, string Remark)
{
	public static readonly HomeSystemStatusLoadResult SignedOut = new(false, "系统状态未知", "请先登录后获取系统状态");
	public static readonly HomeSystemStatusLoadResult Unavailable = new(false, "系统状态异常", "当前无法获取系统状态");
}

internal sealed record HomeImportantAnnouncementLoadResult(
	bool ShouldShow,
	string Markdown)
{
	public static readonly HomeImportantAnnouncementLoadResult Hidden = new(false, string.Empty);

	public static HomeImportantAnnouncementLoadResult Visible(string markdown)
	{
		return new HomeImportantAnnouncementLoadResult(true, markdown);
	}
}
