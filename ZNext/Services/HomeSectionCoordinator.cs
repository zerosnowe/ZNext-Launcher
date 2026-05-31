using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ZNext.ViewModels;

namespace ZNext.Services;

internal sealed class HomeSectionCoordinator
{
	private readonly HomeSectionViewModel _viewModel;
	private readonly HomeDataCoordinator _dataCoordinator;
	private readonly UserDialogService _userDialogService;
	private readonly UserProfileSettingsService _userProfileSettingsService;
	private readonly AvatarService _avatarService;
	private readonly AvatarVisualController _avatarVisualController;
	private readonly AnnouncementContentRenderer _announcementContentRenderer;
	private readonly Func<Task> _loadAnnouncementAsync;
	private readonly Action _resetCachedSections;
	private readonly Func<PersonPicture?> _homeAvatarPictureProvider;
	private readonly Func<Border?> _homeAvatarFallbackProvider;
	private readonly Func<RichTextBlock?> _importantAnnouncementBodyProvider;
	private readonly Func<Button?> _loginButtonProvider;
	private readonly Func<Button?> _logoutButtonProvider;
	private readonly Func<TextBlock?> _titleBarUsernameProvider;
	private readonly Func<TextBlock?> _titleBarEmailProvider;
	private readonly Func<FrameworkElement?> _startupOverlayProvider;
	private readonly Func<ProgressRing?> _startupRingProvider;
	private readonly Func<Image?> _startupIconProvider;
	private bool _startupLoadingOverlayDismissed;
	private bool _startupLoadingOverlayDismissing;

	public HomeSectionCoordinator(
		HomeSectionViewModel viewModel,
		HomeDataCoordinator dataCoordinator,
		UserDialogService userDialogService,
		UserProfileSettingsService userProfileSettingsService,
		AvatarService avatarService,
		AvatarVisualController avatarVisualController,
		AnnouncementContentRenderer announcementContentRenderer,
		Func<Task> loadAnnouncementAsync,
		Action resetCachedSections,
		Func<PersonPicture?> homeAvatarPictureProvider,
		Func<Border?> homeAvatarFallbackProvider,
		Func<RichTextBlock?> importantAnnouncementBodyProvider,
		Func<Button?> loginButtonProvider,
		Func<Button?> logoutButtonProvider,
		Func<TextBlock?> titleBarUsernameProvider,
		Func<TextBlock?> titleBarEmailProvider,
		Func<FrameworkElement?> startupOverlayProvider,
		Func<ProgressRing?> startupRingProvider,
		Func<Image?> startupIconProvider)
	{
		_viewModel = viewModel;
		_dataCoordinator = dataCoordinator;
		_userDialogService = userDialogService;
		_userProfileSettingsService = userProfileSettingsService;
		_avatarService = avatarService;
		_avatarVisualController = avatarVisualController;
		_announcementContentRenderer = announcementContentRenderer;
		_loadAnnouncementAsync = loadAnnouncementAsync;
		_resetCachedSections = resetCachedSections;
		_homeAvatarPictureProvider = homeAvatarPictureProvider;
		_homeAvatarFallbackProvider = homeAvatarFallbackProvider;
		_importantAnnouncementBodyProvider = importantAnnouncementBodyProvider;
		_loginButtonProvider = loginButtonProvider;
		_logoutButtonProvider = logoutButtonProvider;
		_titleBarUsernameProvider = titleBarUsernameProvider;
		_titleBarEmailProvider = titleBarEmailProvider;
		_startupOverlayProvider = startupOverlayProvider;
		_startupRingProvider = startupRingProvider;
		_startupIconProvider = startupIconProvider;
	}

	public async Task ApplyUserSessionOperationResultAsync(UserSessionOperationResult result)
	{
		if (result.ShouldResetCachedSections)
		{
			_resetCachedSections();
		}

		try
		{
			if (result.ShouldShowDialog)
			{
				await _userDialogService.ShowInfoAsync(result.DialogTitle, result.DialogMessage);
			}

			if (result.ShouldLoadAnnouncement)
			{
				await _loadAnnouncementAsync();
			}

			if (result.ShouldRefreshUserInfo)
			{
				await UpdateUserInfoAndUIAsync();
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("User session operation failed: " + ex.Message);
		}
		finally
		{
			if (result.ShouldUpdateLogoutUi)
			{
				ApplyLogoutState();
			}
		}
	}

	public async Task UpdateUserInfoAndUIAsync()
	{
		_viewModel.IsTunnelCountLoading = true;
		try
		{
			ApplyHomeUserInfoLoadResult(await _dataCoordinator.LoadUserInfoAsync());
		}
		finally
		{
			_viewModel.IsTunnelCountLoading = false;
			await HideStartupLoadingOverlayAsync();
		}
	}

	public void ApplySignedInTokenState()
	{
		_viewModel.ApplySignedInTokenState();
		TextBlock? usernameText = _titleBarUsernameProvider();
		if (usernameText != null)
		{
			usernameText.Text = "已登录";
		}

		TextBlock? emailText = _titleBarEmailProvider();
		if (emailText != null)
		{
			emailText.Text = "正在获取账户信息";
		}

		UpdateButtonVisibility(isLoggedIn: true);
	}

	public void ApplySignedOutState()
	{
		ApplySystemStatusLoadResult(HomeSystemStatusLoadResult.SignedOut);
		_viewModel.ImportantAnnouncementVisibility = Visibility.Collapsed;
	}

	public void EnsureStartupLoadingIconVisible()
	{
		Image? icon = _startupIconProvider();
		if (icon != null)
		{
			icon.Opacity = 1.0;
		}
	}

	public async Task HideStartupLoadingOverlayAsync()
	{
		FrameworkElement? overlay = _startupOverlayProvider();
		if (_startupLoadingOverlayDismissed || _startupLoadingOverlayDismissing || overlay == null)
		{
			return;
		}

		_startupLoadingOverlayDismissing = true;
		await Task.Delay(2000);
		overlay = _startupOverlayProvider();
		if (_startupLoadingOverlayDismissed || overlay == null)
		{
			_startupLoadingOverlayDismissing = false;
			return;
		}

		overlay.Visibility = Visibility.Collapsed;
		overlay.Opacity = 0.0;
		ProgressRing? loadingRing = _startupRingProvider();
		if (loadingRing != null)
		{
			loadingRing.IsActive = false;
		}

		_startupLoadingOverlayDismissed = true;
		_startupLoadingOverlayDismissing = false;
	}

	public void RefreshHomeBannerAvatar()
	{
		_avatarVisualController.RefreshHomeAvatar(
			_homeAvatarPictureProvider(),
			_homeAvatarFallbackProvider(),
			_avatarService.LoadAvatarPath());
	}

	public async Task LoadImportantAnnouncementAsync()
	{
		HomeImportantAnnouncementLoadResult result = await _dataCoordinator.LoadImportantAnnouncementAsync();
		if (!result.ShouldShow)
		{
			_viewModel.ImportantAnnouncementVisibility = Visibility.Collapsed;
			return;
		}

		_viewModel.ImportantAnnouncementTitle = RenderImportantAnnouncementMarkdown(result.Markdown);
		_viewModel.ImportantAnnouncementVisibility = Visibility.Visible;
	}

	public void DismissImportantAnnouncement()
	{
		_dataCoordinator.DismissImportantAnnouncement();
		_viewModel.ImportantAnnouncementVisibility = Visibility.Collapsed;
	}

	public async Task LoadSystemStatusAsync()
	{
		ApplySystemStatusLoadResult(await _dataCoordinator.LoadSystemStatusAsync());
	}

	public void UpdateButtonVisibility(bool isLoggedIn)
	{
		Button? loginButton = _loginButtonProvider();
		Button? logoutButton = _logoutButtonProvider();
		if (loginButton == null || logoutButton == null)
		{
			return;
		}

		loginButton.Visibility = isLoggedIn ? Visibility.Collapsed : Visibility.Visible;
		logoutButton.Visibility = Visibility.Collapsed;
	}

	private void ApplyHomeUserInfoLoadResult(HomeUserInfoLoadResult result)
	{
		if (result.CachedUser != null)
		{
			UpdateUserInfoDisplay(result.CachedUser);
		}

		if (result.RemoteUser != null)
		{
			UpdateUserInfoDisplay(result.RemoteUser);
		}

		if (result.ShouldMarkLoggedIn)
		{
			UpdateButtonVisibility(isLoggedIn: true);
		}
	}

	private void UpdateUserInfoDisplay(UserInfoData user)
	{
		string username = string.IsNullOrWhiteSpace(user.Username) ? "未设置" : user.Username;
		_viewModel.ApplyUser(user);

		TextBlock? usernameText = _titleBarUsernameProvider();
		if (usernameText != null)
		{
			usernameText.Text = username;
		}

		TextBlock? emailText = _titleBarEmailProvider();
		if (emailText != null)
		{
			emailText.Text = string.IsNullOrWhiteSpace(user.Email) ? "未设置" : user.Email;
		}

		_userProfileSettingsService.SaveUserGroup(user.Group);
		RefreshHomeBannerAvatar();
	}

	private void ApplyLogoutState()
	{
		_viewModel.ResetForLogout();

		TextBlock? usernameText = _titleBarUsernameProvider();
		if (usernameText != null)
		{
			usernameText.Text = "未登录";
		}

		TextBlock? emailText = _titleBarEmailProvider();
		if (emailText != null)
		{
			emailText.Text = "-";
		}

		RefreshHomeBannerAvatar();
		UpdateButtonVisibility(isLoggedIn: false);
	}

	private string RenderImportantAnnouncementMarkdown(string markdown)
	{
		string title = "重要公告";
		RichTextBlock? body = _importantAnnouncementBodyProvider();
		if (body != null)
		{
			_announcementContentRenderer.Render(body, markdown, extractFirstHeadingAsTitle: true, out string detectedTitle);
			if (!string.IsNullOrWhiteSpace(detectedTitle))
			{
				title = detectedTitle;
			}
		}

		return title;
	}

	private void ApplySystemStatusLoadResult(HomeSystemStatusLoadResult result)
	{
		_viewModel.SetSystemStatus(result.IsHealthy, result.Title, result.Remark);
	}
}
