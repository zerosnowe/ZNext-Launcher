using System.Diagnostics;
using ZNext.ViewModels;

namespace ZNext.Services;

internal sealed class SettingsToggleCoordinator
{
	private readonly SettingsViewModel _settingsViewModel;
	private readonly AutoStartSettingsCoordinator _autoStartSettingsCoordinator;
	private readonly SecuritySettingsCoordinator _securitySettingsCoordinator;
	private bool _isInitializingSecurityToggle;
	private bool _isInitializingAutoStartToggle;
	private bool _isInitializingAutoStartTunnelsToggle;

	public SettingsToggleCoordinator(
		SettingsViewModel settingsViewModel,
		AutoStartSettingsCoordinator autoStartSettingsCoordinator,
		SecuritySettingsCoordinator securitySettingsCoordinator)
	{
		_settingsViewModel = settingsViewModel;
		_autoStartSettingsCoordinator = autoStartSettingsCoordinator;
		_securitySettingsCoordinator = securitySettingsCoordinator;
	}

	public SettingsSecurityInitializationResult InitializeSecurityAccess(bool updateOverlay)
	{
		try
		{
			SecurityAccessState state = _securitySettingsCoordinator.GetEffectiveState();
			SetSecurityEnabledSilently(state.IsEnabled);
			_settingsViewModel.SetSecurityPasswordButtonState(state.HasPassword);
			return new SettingsSecurityInitializationResult(
				ShouldShowLockOverlay: updateOverlay && state.IsEnabled,
				ShouldHideLockOverlay: updateOverlay && !state.IsEnabled);
		}
		catch (Exception ex)
		{
			_isInitializingSecurityToggle = false;
			Debug.WriteLine("InitializeSecurityAccessSetting failed: " + ex.Message);
			return SettingsSecurityInitializationResult.None;
		}
	}

	public void RefreshSecurityAccess()
	{
		try
		{
			SecurityAccessState state = _securitySettingsCoordinator.GetEffectiveState();
			SetSecurityEnabledSilently(state.IsEnabled);
			_settingsViewModel.SetSecurityPasswordButtonState(state.HasPassword);
		}
		catch (Exception ex)
		{
			_isInitializingSecurityToggle = false;
			Debug.WriteLine("RefreshSecurityAccessSettingsUi failed: " + ex.Message);
		}
	}

	public void InitializeApplicationAutoStart()
	{
		try
		{
			SetApplicationAutoStartEnabledSilently(_autoStartSettingsCoordinator.IsApplicationAutoStartEnabled());
		}
		catch (Exception ex)
		{
			_isInitializingAutoStartToggle = false;
			Debug.WriteLine("InitializeAutoStartSetting failed: " + ex.Message);
		}
	}

	public SettingsAutoStartTunnelsInitializationResult InitializeTunnelAutoStart()
	{
		try
		{
			AutoStartTunnelSettingsSnapshot settings = _autoStartSettingsCoordinator.LoadTunnelSettings();
			SetTunnelAutoStartEnabledSilently(settings.IsEnabled);
			return new SettingsAutoStartTunnelsInitializationResult(settings.SelectedTunnelIds);
		}
		catch (Exception ex)
		{
			_isInitializingAutoStartTunnelsToggle = false;
			Debug.WriteLine("InitializeAutoStartTunnelsSetting failed: " + ex.Message);
			return SettingsAutoStartTunnelsInitializationResult.None;
		}
	}

	public SettingsToggleActionResult ToggleApplicationAutoStart(bool enabled)
	{
		if (_isInitializingAutoStartToggle)
		{
			return SettingsToggleActionResult.Ignored;
		}

		AutoStartChangeResult result = _autoStartSettingsCoordinator.SetApplicationAutoStartEnabled(enabled);
		if (result.Succeeded)
		{
			return SettingsToggleActionResult.Toast(result.Message);
		}

		SetApplicationAutoStartEnabledSilently(!enabled);
		return SettingsToggleActionResult.Dialog(enabled ? "启用失败" : "关闭失败", result.Message);
	}

	public SettingsToggleActionResult ToggleTunnelAutoStart(bool enabled)
	{
		if (_isInitializingAutoStartTunnelsToggle)
		{
			return SettingsToggleActionResult.Ignored;
		}

		AutoStartTunnelToggleResult result = _autoStartSettingsCoordinator.SetTunnelAutoStartEnabled(enabled);
		return SettingsToggleActionResult.Toast(result.Message, shouldRefreshAutoStartChecklist: enabled);
	}

	public SettingsToggleActionResult ToggleSecurityPassword(bool enabled)
	{
		if (_isInitializingSecurityToggle)
		{
			return SettingsToggleActionResult.Ignored;
		}

		SecurityActionResult result = _securitySettingsCoordinator.ApplyEnabledState(enabled);
		if (result.Succeeded)
		{
			return SettingsToggleActionResult.Toast(result.Message, shouldHideSecurityLockOverlay: !enabled);
		}

		SetSecurityEnabledSilently(false);
		return SettingsToggleActionResult.Dialog("提示", result.Message);
	}

	private void SetApplicationAutoStartEnabledSilently(bool enabled)
	{
		_isInitializingAutoStartToggle = true;
		_settingsViewModel.IsAutoStartEnabled = enabled;
		_isInitializingAutoStartToggle = false;
	}

	private void SetTunnelAutoStartEnabledSilently(bool enabled)
	{
		_isInitializingAutoStartTunnelsToggle = true;
		_settingsViewModel.IsAutoStartTunnelsEnabled = enabled;
		_isInitializingAutoStartTunnelsToggle = false;
	}

	private void SetSecurityEnabledSilently(bool enabled)
	{
		_isInitializingSecurityToggle = true;
		_settingsViewModel.IsSecurityPasswordEnabled = enabled;
		_isInitializingSecurityToggle = false;
	}
}

internal sealed record SettingsSecurityInitializationResult(
	bool ShouldShowLockOverlay,
	bool ShouldHideLockOverlay)
{
	public static readonly SettingsSecurityInitializationResult None = new(false, false);
}

internal sealed record SettingsAutoStartTunnelsInitializationResult(HashSet<int>? SelectedTunnelIds)
{
	public bool HasSelectedTunnelIds => SelectedTunnelIds != null;

	public static readonly SettingsAutoStartTunnelsInitializationResult None = new((HashSet<int>?)null);
}

internal sealed record SettingsToggleActionResult(
	bool IsIgnored,
	bool ShouldShowToast,
	string ToastMessage,
	bool ShouldShowDialog,
	string DialogTitle,
	string DialogMessage,
	bool ShouldRefreshAutoStartChecklist,
	bool ShouldHideSecurityLockOverlay)
{
	public static readonly SettingsToggleActionResult Ignored = new(
		IsIgnored: true,
		ShouldShowToast: false,
		ToastMessage: string.Empty,
		ShouldShowDialog: false,
		DialogTitle: string.Empty,
		DialogMessage: string.Empty,
		ShouldRefreshAutoStartChecklist: false,
		ShouldHideSecurityLockOverlay: false);

	public static SettingsToggleActionResult Toast(
		string message,
		bool shouldRefreshAutoStartChecklist = false,
		bool shouldHideSecurityLockOverlay = false)
	{
		return new SettingsToggleActionResult(
			IsIgnored: false,
			ShouldShowToast: true,
			ToastMessage: message,
			ShouldShowDialog: false,
			DialogTitle: string.Empty,
			DialogMessage: string.Empty,
			ShouldRefreshAutoStartChecklist: shouldRefreshAutoStartChecklist,
			ShouldHideSecurityLockOverlay: shouldHideSecurityLockOverlay);
	}

	public static SettingsToggleActionResult Dialog(string title, string message)
	{
		return new SettingsToggleActionResult(
			IsIgnored: false,
			ShouldShowToast: false,
			ToastMessage: string.Empty,
			ShouldShowDialog: true,
			DialogTitle: title,
			DialogMessage: message,
			ShouldRefreshAutoStartChecklist: false,
			ShouldHideSecurityLockOverlay: false);
	}
}
