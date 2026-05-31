using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace ZNext.Services;

internal sealed class SecuritySettingsCoordinator
{
	private readonly SecurityAccessService _accessService;
	private readonly SecurityPasswordDialogService _passwordDialogService;

	public SecuritySettingsCoordinator(SecurityAccessService accessService)
	{
		_accessService = accessService;
		_passwordDialogService = new SecurityPasswordDialogService(accessService);
	}

	public SecurityAccessState GetEffectiveState()
	{
		return _accessService.GetEffectiveState();
	}

	public SecurityActionResult ApplyEnabledState(bool enabled)
	{
		return _accessService.ApplyEnabledState(enabled);
	}

	public async Task<SecurityPasswordChangeResult> ShowPasswordDialogAsync(XamlRoot? xamlRoot)
	{
		bool hadPassword = _accessService.HasPassword();
		string? password = hadPassword
			? await _passwordDialogService.ShowResetPasswordAsync(xamlRoot)
			: await _passwordDialogService.ShowSetPasswordAsync(xamlRoot);

		if (string.IsNullOrWhiteSpace(password))
		{
			return SecurityPasswordChangeResult.Cancelled();
		}

		_accessService.SavePassword(password);
		return SecurityPasswordChangeResult.Success(
			hasPassword: true,
			hadPassword ? "密码重置成功" : "密码设置成功");
	}

	public SecurityActionResult TryUnlock(string input)
	{
		return _accessService.TryUnlock(input);
	}
}

internal sealed record SecurityPasswordChangeResult(bool Changed, bool HasPassword, string? Message)
{
	public static SecurityPasswordChangeResult Cancelled()
	{
		return new SecurityPasswordChangeResult(false, false, null);
	}

	public static SecurityPasswordChangeResult Success(bool hasPassword, string message)
	{
		return new SecurityPasswordChangeResult(true, hasPassword, message);
	}
}
