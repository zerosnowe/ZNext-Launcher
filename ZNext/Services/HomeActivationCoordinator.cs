namespace ZNext.Services;

internal sealed class HomeActivationCoordinator
{
	private readonly bool _isAutoStartLaunch;
	private readonly UserSessionService _userSessionService;
	private bool _hasAppliedAutoStartMinimize;
	private bool _isHomeLoaded;

	public HomeActivationCoordinator(
		bool isAutoStartLaunch,
		UserSessionService userSessionService)
	{
		_isAutoStartLaunch = isAutoStartLaunch;
		_userSessionService = userSessionService;
	}

	public async Task<HomeActivationResult> ActivateAsync(HomeActivationContext context)
	{
		if (_isAutoStartLaunch && !_hasAppliedAutoStartMinimize)
		{
			if (_userSessionService.IsSignedIn)
			{
				await context.TryAutoStartConfiguredTunnelsAsync();
			}

			_hasAppliedAutoStartMinimize = true;
			return HomeActivationResult.ApplyAutoStartMinimize;
		}

		if (_isHomeLoaded)
		{
			return HomeActivationResult.None;
		}

		_isHomeLoaded = true;
		await context.RefreshAnnouncementIfCreatedAsync();
		if (_userSessionService.IsSignedIn)
		{
			_userSessionService.SynchronizeTokens();
			context.ApplySignedInHomeState();
			await context.UpdateUserInfoAndUiAsync();
			await context.LoadSystemStatusAsync();
			await context.LoadImportantAnnouncementAsync();
			await context.TryAutoStartConfiguredTunnelsAsync();
			return HomeActivationResult.None;
		}

		context.ApplySignedOutHomeState();
		await context.HideStartupLoadingOverlayAsync();
		return HomeActivationResult.None;
	}

	public void MarkHomeLoaded()
	{
		_isHomeLoaded = true;
	}
}

internal sealed record HomeActivationContext(
	Func<Task> RefreshAnnouncementIfCreatedAsync,
	Func<Task> UpdateUserInfoAndUiAsync,
	Func<Task> LoadSystemStatusAsync,
	Func<Task> LoadImportantAnnouncementAsync,
	Func<Task> TryAutoStartConfiguredTunnelsAsync,
	Action ApplySignedInHomeState,
	Action ApplySignedOutHomeState,
	Func<Task> HideStartupLoadingOverlayAsync);

internal sealed record HomeActivationResult(bool ShouldApplyAutoStartMinimize)
{
	public static readonly HomeActivationResult None = new(false);
	public static readonly HomeActivationResult ApplyAutoStartMinimize = new(true);
}
