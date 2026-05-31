using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using ZNext.ViewModels;
using ZNext.Views;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace ZNext.Services;

internal sealed class SettingsSectionCoordinator
{
	private readonly SettingsViewModel _settingsViewModel;
	private readonly SettingsToggleCoordinator _settingsToggleCoordinator;
	private readonly SettingsOperationCoordinator _settingsOperationCoordinator;
	private readonly SecuritySettingsCoordinator _securitySettingsCoordinator;
	private readonly AutoStartSettingsCoordinator _autoStartSettingsCoordinator;
	private readonly UserSessionService _userSessionService;
	private readonly TunnelsSectionViewModel _tunnelsViewModel;
	private readonly PageLoadStateCoordinator _pageLoadStateCoordinator;
	private readonly FrpcSettingsService _frpcSettingsService;
	private readonly AppVersionService _appVersionService;
	private readonly UserDialogService _userDialogService;
	private readonly UserActionCoordinator _userActionCoordinator;
	private readonly AutoStartTunnelChecklistRenderer _autoStartTunnelChecklistRenderer = new AutoStartTunnelChecklistRenderer();
	private readonly FrpcInstallVisualController _frpcInstallVisualController = new FrpcInstallVisualController();
	private readonly SecurityAccessVisualController _securityAccessVisualController = new SecurityAccessVisualController();
	private readonly Func<SettingsSectionView?> _viewAccessor;
	private readonly Func<FrameworkElement?> _securityLockOverlayAccessor;
	private readonly Func<Control?> _securityNavigationHostAccessor;
	private readonly Func<PasswordBox?> _securityUnlockPasswordBoxAccessor;
	private readonly Func<TextBlock?> _securityUnlockErrorTextAccessor;
	private readonly Func<XamlRoot?> _xamlRootProvider;
	private readonly Func<nint> _ownerHwndProvider;
	private readonly Func<bool, Task> _loadTunnelsAsync;
	private readonly Action<string> _applyThemeMode;
	private readonly Action<string> _showSuccessToast;
	private readonly Action _refreshAvatar;
	private readonly DispatcherQueue _dispatcherQueue;
	private HashSet<int> _autoStartTunnelIds = new HashSet<int>();

	public SettingsSectionCoordinator(
		SettingsViewModel settingsViewModel,
		SettingsToggleCoordinator settingsToggleCoordinator,
		SettingsOperationCoordinator settingsOperationCoordinator,
		SecuritySettingsCoordinator securitySettingsCoordinator,
		AutoStartSettingsCoordinator autoStartSettingsCoordinator,
		UserSessionService userSessionService,
		TunnelsSectionViewModel tunnelsViewModel,
		PageLoadStateCoordinator pageLoadStateCoordinator,
		FrpcSettingsService frpcSettingsService,
		AppVersionService appVersionService,
		UserDialogService userDialogService,
		UserActionCoordinator userActionCoordinator,
		Func<SettingsSectionView?> viewAccessor,
		Func<FrameworkElement?> securityLockOverlayAccessor,
		Func<Control?> securityNavigationHostAccessor,
		Func<PasswordBox?> securityUnlockPasswordBoxAccessor,
		Func<TextBlock?> securityUnlockErrorTextAccessor,
		Func<XamlRoot?> xamlRootProvider,
		Func<nint> ownerHwndProvider,
		Func<bool, Task> loadTunnelsAsync,
		Action<string> applyThemeMode,
		Action<string> showSuccessToast,
		Action refreshAvatar,
		DispatcherQueue dispatcherQueue)
	{
		_settingsViewModel = settingsViewModel;
		_settingsToggleCoordinator = settingsToggleCoordinator;
		_settingsOperationCoordinator = settingsOperationCoordinator;
		_securitySettingsCoordinator = securitySettingsCoordinator;
		_autoStartSettingsCoordinator = autoStartSettingsCoordinator;
		_userSessionService = userSessionService;
		_tunnelsViewModel = tunnelsViewModel;
		_pageLoadStateCoordinator = pageLoadStateCoordinator;
		_frpcSettingsService = frpcSettingsService;
		_appVersionService = appVersionService;
		_userDialogService = userDialogService;
		_userActionCoordinator = userActionCoordinator;
		_viewAccessor = viewAccessor;
		_securityLockOverlayAccessor = securityLockOverlayAccessor;
		_securityNavigationHostAccessor = securityNavigationHostAccessor;
		_securityUnlockPasswordBoxAccessor = securityUnlockPasswordBoxAccessor;
		_securityUnlockErrorTextAccessor = securityUnlockErrorTextAccessor;
		_xamlRootProvider = xamlRootProvider;
		_ownerHwndProvider = ownerHwndProvider;
		_loadTunnelsAsync = loadTunnelsAsync;
		_applyThemeMode = applyThemeMode;
		_showSuccessToast = showSuccessToast;
		_refreshAvatar = refreshAvatar;
		_dispatcherQueue = dispatcherQueue;
	}

	public void InitializeStartupSettings()
	{
		InitializeThemeSetting();
		InitializeAboutInfo();
		_refreshAvatar();
		_ = RefreshFrpcInstallStateAsync();
		InitializeSecurityAccessSetting();
		InitializeAutoStartSetting();
		InitializeAutoStartTunnelsSetting();
	}

	public void RefreshUi()
	{
		InitializeThemeSetting();
		InitializeAboutInfo();
		InitializeAutoStartSetting();
		InitializeAutoStartTunnelsSetting();
		RefreshSecurityAccessSettingsUi();
		_refreshAvatar();
		_ = RefreshFrpcInstallStateAsync();
		_ = RefreshAutoStartTunnelChecklistAsync(forceReload: false);
	}

	public void HandleViewModelPropertyChanged(string? propertyName)
	{
		if (string.Equals(propertyName, nameof(SettingsViewModel.ThemeMode), StringComparison.Ordinal))
		{
			_applyThemeMode(_settingsViewModel.ThemeMode);
		}
	}

	public void UpdateAutoStartTunnelSelectionSnapshot(HashSet<int> selectedTunnelIds)
	{
		_autoStartTunnelIds = selectedTunnelIds;
		RebuildAutoStartTunnelChecklistUi();
	}

	public void RebuildAutoStartTunnelChecklistUi()
	{
		if (!_userSessionService.IsSignedIn)
		{
			_autoStartTunnelChecklistRenderer.RenderMessage(
				AutoStartTunnelCheckListPanel,
				AutoStartTunnelListStatusText,
				"请先登录后查看可选隧道。");
			return;
		}

		if (!_tunnelsViewModel.HasCachedTunnels)
		{
			_autoStartTunnelChecklistRenderer.RenderMessage(
				AutoStartTunnelCheckListPanel,
				AutoStartTunnelListStatusText,
				_pageLoadStateCoordinator.IsLoadingTunnels ? "正在加载隧道列表..." : "暂无可选隧道。");
			return;
		}

		IReadOnlyList<AutoStartTunnelChecklistItem> items = _autoStartSettingsCoordinator.BuildChecklistItems(
			_tunnelsViewModel.Tunnels,
			_autoStartTunnelIds);
		_autoStartTunnelChecklistRenderer.RenderItems(
			AutoStartTunnelCheckListPanel,
			AutoStartTunnelListStatusText,
			items,
			AutoStartTunnelItemCheckBoxCheckedChanged);
	}

	public async Task RefreshAutoStartTunnelChecklistAsync(bool forceReload)
	{
		if (AutoStartTunnelCheckListPanel == null || AutoStartTunnelListStatusText == null)
		{
			return;
		}

		if (!_userSessionService.IsSignedIn)
		{
			RebuildAutoStartTunnelChecklistUi();
			return;
		}

		if (forceReload || !_tunnelsViewModel.HasCachedTunnels || _pageLoadStateCoordinator.ShouldLoadTunnels())
		{
			await _loadTunnelsAsync(forceReload);
			return;
		}

		RebuildAutoStartTunnelChecklistUi();
	}

	public async Task HandleAutoStartToggledAsync(object sender)
	{
		if (sender is ToggleSwitch toggle)
		{
			await ApplySettingsToggleActionResultAsync(_settingsToggleCoordinator.ToggleApplicationAutoStart(toggle.IsOn));
		}
	}

	public async Task HandleAutoStartTunnelsToggledAsync(object sender)
	{
		if (sender is ToggleSwitch toggle)
		{
			await ApplySettingsToggleActionResultAsync(_settingsToggleCoordinator.ToggleTunnelAutoStart(toggle.IsOn));
		}
	}

	public async Task HandleSecurityPasswordToggledAsync(object sender)
	{
		if (sender is ToggleSwitch toggle)
		{
			await ApplySettingsToggleActionResultAsync(_settingsToggleCoordinator.ToggleSecurityPassword(toggle.IsOn));
		}
	}

	public async Task HandleSetSecurityPasswordAsync()
	{
		SecurityPasswordChangeResult result = await _securitySettingsCoordinator.ShowPasswordDialogAsync(_xamlRootProvider());
		if (result.Changed)
		{
			RefreshSecurityPasswordUi(result.HasPassword);
			_showSuccessToast(result.Message ?? "密码已更新");
		}
	}

	public void HandleSecurityUnlock()
	{
		PasswordBox? passwordBox = _securityUnlockPasswordBoxAccessor();
		if (passwordBox == null)
		{
			return;
		}

		string input = passwordBox.Password ?? string.Empty;
		SecurityActionResult unlockResult = _securitySettingsCoordinator.TryUnlock(input);
		if (unlockResult.Succeeded)
		{
			HideSecurityLockOverlay();
			_securityAccessVisualController.ClearUnlockError(passwordBox, _securityUnlockErrorTextAccessor());
			return;
		}

		_securityAccessVisualController.ShowUnlockError(_securityUnlockErrorTextAccessor(), unlockResult.Message);
	}

	public void HandleSecurityUnlockKeyDown(KeyRoutedEventArgs e)
	{
		if (e.Key == VirtualKey.Enter)
		{
			e.Handled = true;
			HandleSecurityUnlock();
		}
	}

	public async Task HandleFrpcInstallAsync()
	{
		await ApplySettingsOperationResultAsync(await _settingsOperationCoordinator.InstallOrUninstallFrpcAsync(
			_settingsViewModel.SetFrpcStatusText,
			SetFrpcOperationBusyState));
	}

	public async Task HandleFetchUpdateAsync()
	{
		await ApplySettingsOperationResultAsync(await _settingsOperationCoordinator.CheckLauncherUpdateAsync(_settingsViewModel.SetFetchUpdateBusy));
	}

	public async Task HandleUploadAvatarAsync()
	{
		await ApplySettingsOperationResultAsync(await _settingsOperationCoordinator.PickAvatarAsync(_ownerHwndProvider()));
	}

	public async Task HandleOpenFrpcDirectoryAsync()
	{
		await ShowUserActionResultAsync(_userActionCoordinator.OpenFrpcDirectory());
	}

	private TextBlock? AutoStartTunnelListStatusText => _viewAccessor()?.AutoStartTunnelListStatusText;

	private StackPanel? AutoStartTunnelCheckListPanel => _viewAccessor()?.AutoStartTunnelCheckListPanel;

	private ProgressRing? FrpcInstallBusyRing => _viewAccessor()?.FrpcInstallBusyRing;

	private TextBlock? FrpcInstallBusyText => _viewAccessor()?.FrpcInstallBusyText;

	private Button? FrpcInstallButton => _viewAccessor()?.FrpcInstallButton;

	private FontIcon? FrpcStatusIcon => _viewAccessor()?.FrpcStatusIcon;

	private void InitializeThemeSetting()
	{
		_applyThemeMode(_settingsViewModel.LoadThemeMode());
	}

	private void InitializeAboutInfo()
	{
		_settingsViewModel.SetAboutVersion(_appVersionService.GetDisplayVersion());
	}

	private void InitializeSecurityAccessSetting()
	{
		SettingsSecurityInitializationResult result = _settingsToggleCoordinator.InitializeSecurityAccess(updateOverlay: true);
		if (result.ShouldShowLockOverlay)
		{
			ShowSecurityLockOverlay();
		}
		else if (result.ShouldHideLockOverlay)
		{
			HideSecurityLockOverlay();
		}
	}

	private void RefreshSecurityAccessSettingsUi()
	{
		_settingsToggleCoordinator.RefreshSecurityAccess();
	}

	private void RefreshSecurityPasswordUi(bool hasPassword)
	{
		_settingsViewModel.SetSecurityPasswordButtonState(hasPassword);
	}

	private void InitializeAutoStartSetting()
	{
		_settingsToggleCoordinator.InitializeApplicationAutoStart();
	}

	private void InitializeAutoStartTunnelsSetting()
	{
		SettingsAutoStartTunnelsInitializationResult result = _settingsToggleCoordinator.InitializeTunnelAutoStart();
		if (result.HasSelectedTunnelIds && result.SelectedTunnelIds != null)
		{
			_autoStartTunnelIds = result.SelectedTunnelIds;
		}

		RebuildAutoStartTunnelChecklistUi();
	}

	private void AutoStartTunnelItemCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
	{
		if (sender is CheckBox { Tag: int tunnelId } checkBox)
		{
			_autoStartSettingsCoordinator.UpdateTunnelSelection(_autoStartTunnelIds, tunnelId, checkBox.IsChecked == true);
		}
	}

	private async Task ApplySettingsToggleActionResultAsync(SettingsToggleActionResult result)
	{
		if (result.IsIgnored)
		{
			return;
		}

		if (result.ShouldHideSecurityLockOverlay)
		{
			HideSecurityLockOverlay();
		}

		if (result.ShouldRefreshAutoStartChecklist)
		{
			await RefreshAutoStartTunnelChecklistAsync(forceReload: false);
		}

		if (result.ShouldShowDialog)
		{
			await _userDialogService.ShowInfoAsync(result.DialogTitle, result.DialogMessage);
			return;
		}

		if (result.ShouldShowToast)
		{
			_showSuccessToast(result.ToastMessage);
		}
	}

	private void ShowSecurityLockOverlay()
	{
		_securityAccessVisualController.ShowLockOverlay(
			_securityLockOverlayAccessor(),
			_securityNavigationHostAccessor(),
			_securityUnlockPasswordBoxAccessor(),
			_securityUnlockErrorTextAccessor(),
			_dispatcherQueue);
	}

	private void HideSecurityLockOverlay()
	{
		_securityAccessVisualController.HideLockOverlay(
			_securityLockOverlayAccessor(),
			_securityNavigationHostAccessor());
	}

	private void SetFrpcOperationBusyState(bool isBusy, bool isInstallFlow)
	{
		_frpcInstallVisualController.SetButtonsState(FrpcInstallButton, isBusy);
		if (isInstallFlow)
		{
			_frpcInstallVisualController.SetBusyState(FrpcInstallBusyRing, FrpcInstallBusyText, isBusy);
		}
	}

	private async Task RefreshFrpcInstallStateAsync()
	{
		try
		{
			FrpcInstallState state = await _frpcSettingsService.GetInstallStateAsync();
			_frpcInstallVisualController.UpdateInstallButton(FrpcInstallButton, state.IsInstalled);
			_frpcInstallVisualController.UpdateStatusIcon(FrpcStatusIcon, state.IsInstalled, state.IsError);
			_settingsViewModel.SetFrpcStatusText(state.StatusText);
		}
		catch (Exception ex)
		{
			_frpcInstallVisualController.UpdateInstallButton(FrpcInstallButton, isInstalled: false);
			_frpcInstallVisualController.UpdateStatusIcon(FrpcStatusIcon, isInstalled: false, isError: true);
			_settingsViewModel.SetFrpcStatusText("状态: 检测失败 - " + ex.Message);
		}
	}

	private async Task ApplySettingsOperationResultAsync(SettingsOperationResult result)
	{
		if (result.ShouldShowDialog)
		{
			await _userDialogService.ShowInfoAsync(result.DialogTitle, result.DialogMessage);
		}

		if (result.ShouldRefreshAvatar)
		{
			_refreshAvatar();
		}

		if (result.ShouldShowToast)
		{
			_showSuccessToast(result.ToastMessage);
		}

		if (result.ShouldRefreshFrpcInstallState)
		{
			await RefreshFrpcInstallStateAsync();
		}
	}

	private async Task ShowUserActionResultAsync(UserActionResult result)
	{
		if (result.ShouldShowDialog)
		{
			await _userDialogService.ShowInfoAsync(result.Title, result.Message);
		}
	}
}
