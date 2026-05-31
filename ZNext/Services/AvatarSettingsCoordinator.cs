using System;
using System.Threading.Tasks;

namespace ZNext.Services;

internal sealed class AvatarSettingsCoordinator
{
	private readonly AvatarPickerService _avatarPickerService;
	public AvatarSettingsCoordinator(AvatarService avatarService)
	{
		_avatarPickerService = new AvatarPickerService(avatarService);
	}

	public async Task<AvatarSettingsActionResult> PickAndSaveAsync(nint ownerHwnd)
	{
		try
		{
			bool saved = await _avatarPickerService.PickAndSaveAvatarAsync(ownerHwnd);
			return saved
				? AvatarSettingsActionResult.Success("头像已更新")
				: AvatarSettingsActionResult.Cancelled();
		}
		catch (Exception ex)
		{
			return AvatarSettingsActionResult.Failure("上传头像失败", ex.Message);
		}
	}

}

internal sealed record AvatarSettingsActionResult(
	bool Succeeded,
	bool WasCancelled,
	string? SuccessMessage,
	string? FailureTitle,
	string? FailureMessage)
{
	public static AvatarSettingsActionResult Success(string message)
	{
		return new AvatarSettingsActionResult(true, false, message, null, null);
	}

	public static AvatarSettingsActionResult Cancelled()
	{
		return new AvatarSettingsActionResult(false, true, null, null, null);
	}

	public static AvatarSettingsActionResult Failure(string title, string message)
	{
		return new AvatarSettingsActionResult(false, false, null, title, message);
	}
}
