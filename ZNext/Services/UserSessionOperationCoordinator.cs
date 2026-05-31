using Microsoft.UI.Xaml;

namespace ZNext.Services;

internal sealed class UserSessionOperationCoordinator
{
	private readonly UserSessionService _userSessionService;
	private readonly UserInfoService _userInfoService;
	private readonly UserProfileSettingsService _userProfileSettingsService;
	private readonly SignCaptchaDialogService _signCaptchaDialogService;

	public UserSessionOperationCoordinator(
		UserSessionService userSessionService,
		UserInfoService userInfoService,
		UserProfileSettingsService userProfileSettingsService,
		SignCaptchaDialogService signCaptchaDialogService)
	{
		_userSessionService = userSessionService;
		_userInfoService = userInfoService;
		_userProfileSettingsService = userProfileSettingsService;
		_signCaptchaDialogService = signCaptchaDialogService;
	}

	public async Task<UserSessionOperationResult> SignInAsync(
		XamlRoot? xamlRoot,
		Action<bool> signInEnabledChanged)
	{
		if (!_userSessionService.IsSignedIn)
		{
			return UserSessionOperationResult.Dialog("提示", "请先登录后再签到。");
		}

		string? captchaToken = await _signCaptchaDialogService.ShowAsync(xamlRoot);
		if (string.IsNullOrWhiteSpace(captchaToken))
		{
			return UserSessionOperationResult.None;
		}

		signInEnabledChanged(false);
		try
		{
			SignResult signResult = await _userInfoService.SignWithCaptchaAsync(captchaToken);
			if (signResult.Success)
			{
				return UserSessionOperationResult.Dialog(
					"签到成功",
					signResult.Message,
					shouldRefreshUserInfo: true);
			}

			signInEnabledChanged(true);
			return UserSessionOperationResult.Dialog("签到失败", signResult.Message);
		}
		catch (Exception ex)
		{
			signInEnabledChanged(true);
			return UserSessionOperationResult.Dialog("签到失败", ex.Message);
		}
	}

	public async Task<UserSessionOperationResult> LogoutAsync()
	{
		_userSessionService.Clear();
		_userProfileSettingsService.ClearUserGroup();
		await _userInfoService.ClearCachedUserInfoAsync();
		return UserSessionOperationResult.LogoutCompleted();
	}
}

internal sealed record UserSessionOperationResult(
	bool ShouldShowDialog,
	string DialogTitle,
	string DialogMessage,
	bool ShouldRefreshUserInfo,
	bool ShouldResetCachedSections,
	bool ShouldLoadAnnouncement,
	bool ShouldUpdateLogoutUi)
{
	public static readonly UserSessionOperationResult None = new(
		ShouldShowDialog: false,
		DialogTitle: string.Empty,
		DialogMessage: string.Empty,
		ShouldRefreshUserInfo: false,
		ShouldResetCachedSections: false,
		ShouldLoadAnnouncement: false,
		ShouldUpdateLogoutUi: false);

	public static UserSessionOperationResult Dialog(
		string title,
		string message,
		bool shouldRefreshUserInfo = false)
	{
		return new UserSessionOperationResult(
			ShouldShowDialog: true,
			DialogTitle: title,
			DialogMessage: message,
			ShouldRefreshUserInfo: shouldRefreshUserInfo,
			ShouldResetCachedSections: false,
			ShouldLoadAnnouncement: false,
			ShouldUpdateLogoutUi: false);
	}

	public static UserSessionOperationResult LogoutCompleted()
	{
		return new UserSessionOperationResult(
			ShouldShowDialog: true,
			DialogTitle: "已退出登录",
			DialogMessage: "您已成功退出",
			ShouldRefreshUserInfo: false,
			ShouldResetCachedSections: true,
			ShouldLoadAnnouncement: true,
			ShouldUpdateLogoutUi: true);
	}
}
