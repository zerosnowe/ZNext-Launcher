namespace ZNext.Services;

internal sealed class SettingsOperationCoordinator
{
	private readonly FrpcSettingsOperationCoordinator _frpcSettingsOperationCoordinator;
	private readonly LauncherUpdateOperationCoordinator _launcherUpdateOperationCoordinator;
	private readonly AvatarSettingsCoordinator _avatarSettingsCoordinator;

	public SettingsOperationCoordinator(
		FrpcSettingsOperationCoordinator frpcSettingsOperationCoordinator,
		LauncherUpdateOperationCoordinator launcherUpdateOperationCoordinator,
		AvatarSettingsCoordinator avatarSettingsCoordinator)
	{
		_frpcSettingsOperationCoordinator = frpcSettingsOperationCoordinator;
		_launcherUpdateOperationCoordinator = launcherUpdateOperationCoordinator;
		_avatarSettingsCoordinator = avatarSettingsCoordinator;
	}

	public async Task<SettingsOperationResult> InstallOrUninstallFrpcAsync(
		Action<string> statusChanged,
		Action<bool, bool> operationStateChanged)
	{
		Progress<string> progress = new Progress<string>(statusChanged);
		FrpcSettingsOperationOutcome outcome = await _frpcSettingsOperationCoordinator.InstallOrUninstallAsync(
			progress,
			operationStateChanged);

		if (!outcome.Started)
		{
			return SettingsOperationResult.None;
		}

		if (!outcome.Succeeded)
		{
			return SettingsOperationResult.Dialog(
				outcome.FailureTitle ?? "操作失败",
				outcome.FailureMessage ?? "操作未完成，请重试。",
				shouldRefreshFrpcInstallState: true);
		}

		return SettingsOperationResult.RefreshFrpcInstallState;
	}

	public async Task<SettingsOperationResult> CheckLauncherUpdateAsync(Action<bool> busyStateChanged)
	{
		LauncherUpdateOperationOutcome outcome = await _launcherUpdateOperationCoordinator.CheckAsync(busyStateChanged);
		if (!outcome.Started || outcome.Result == null)
		{
			return SettingsOperationResult.None;
		}

		if (outcome.Result.ShowDialog)
		{
			return SettingsOperationResult.Dialog(outcome.Result.Title, outcome.Result.Message);
		}

		return string.IsNullOrWhiteSpace(outcome.Result.ToastMessage)
			? SettingsOperationResult.None
			: SettingsOperationResult.Toast(outcome.Result.ToastMessage);
	}

	public async Task<SettingsOperationResult> PickAvatarAsync(nint ownerHwnd)
	{
		return ToOperationResult(await _avatarSettingsCoordinator.PickAndSaveAsync(ownerHwnd));
	}

	private static SettingsOperationResult ToOperationResult(AvatarSettingsActionResult result)
	{
		if (result.WasCancelled)
		{
			return SettingsOperationResult.None;
		}

		if (result.Succeeded)
		{
			return SettingsOperationResult.Toast(result.SuccessMessage ?? "头像已更新", shouldRefreshAvatar: true);
		}

		return string.IsNullOrWhiteSpace(result.FailureTitle)
			? SettingsOperationResult.None
			: SettingsOperationResult.Dialog(result.FailureTitle, result.FailureMessage ?? "操作未完成，请重试。");
	}
}

internal sealed record SettingsOperationResult(
	bool ShouldShowDialog,
	string DialogTitle,
	string DialogMessage,
	bool ShouldShowToast,
	string ToastMessage,
	bool ShouldRefreshFrpcInstallState,
	bool ShouldRefreshAvatar)
{
	public static readonly SettingsOperationResult None = new(
		ShouldShowDialog: false,
		DialogTitle: string.Empty,
		DialogMessage: string.Empty,
		ShouldShowToast: false,
		ToastMessage: string.Empty,
		ShouldRefreshFrpcInstallState: false,
		ShouldRefreshAvatar: false);

	public static readonly SettingsOperationResult RefreshFrpcInstallState = new(
		ShouldShowDialog: false,
		DialogTitle: string.Empty,
		DialogMessage: string.Empty,
		ShouldShowToast: false,
		ToastMessage: string.Empty,
		ShouldRefreshFrpcInstallState: true,
		ShouldRefreshAvatar: false);

	public static SettingsOperationResult Dialog(
		string title,
		string message,
		bool shouldRefreshFrpcInstallState = false)
	{
		return new SettingsOperationResult(
			ShouldShowDialog: true,
			DialogTitle: title,
			DialogMessage: message,
			ShouldShowToast: false,
			ToastMessage: string.Empty,
			ShouldRefreshFrpcInstallState: shouldRefreshFrpcInstallState,
			ShouldRefreshAvatar: false);
	}

	public static SettingsOperationResult Toast(string message, bool shouldRefreshAvatar = false)
	{
		return new SettingsOperationResult(
			ShouldShowDialog: false,
			DialogTitle: string.Empty,
			DialogMessage: string.Empty,
			ShouldShowToast: true,
			ToastMessage: message,
			ShouldRefreshFrpcInstallState: false,
			ShouldRefreshAvatar: shouldRefreshAvatar);
	}
}
