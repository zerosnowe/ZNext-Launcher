using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;
using CommunityToolkit.WinUI.Controls;
using ZNext.Services.Dialogs;
using ZNext.ViewModels;
using ZNext.Views;
using ZNext.Views.Dialogs;
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
	private readonly AccountCenterService _accountCenterService;
	private readonly SignCaptchaDialogService _signCaptchaDialogService;
	private readonly TunnelsSectionViewModel _tunnelsViewModel;
	private readonly PageLoadStateCoordinator _pageLoadStateCoordinator;
	private readonly FrpcSettingsService _frpcSettingsService;
	private readonly FrpcManagerService _frpcManagerService;
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
	private readonly Action<string> _applyBackdropMaterial;
	private readonly Action<bool, string> _applyCustomBackground;
	private readonly Action<string> _showSuccessToast;
	private readonly Action _refreshAvatar;
	private readonly DispatcherQueue _dispatcherQueue;
	private HashSet<int> _autoStartTunnelIds = new HashSet<int>();
	private FrpcInstallState? _cachedFrpcInstallState;
	private DateTimeOffset _lastFrpcInstallRefreshUtc = DateTimeOffset.MinValue;
	private Task? _frpcInstallRefreshTask;
	private Task? _autoStartTunnelChecklistRefreshTask;
	private static readonly TimeSpan FrpcInstallRefreshInterval = TimeSpan.FromSeconds(15);
	private const string CustomBackgroundFolderName = "CustomBackground";

	public SettingsSectionCoordinator(
		SettingsViewModel settingsViewModel,
		SettingsToggleCoordinator settingsToggleCoordinator,
		SettingsOperationCoordinator settingsOperationCoordinator,
		SecuritySettingsCoordinator securitySettingsCoordinator,
		AutoStartSettingsCoordinator autoStartSettingsCoordinator,
		UserSessionService userSessionService,
		AccountCenterService accountCenterService,
		SignCaptchaDialogService signCaptchaDialogService,
		TunnelsSectionViewModel tunnelsViewModel,
		PageLoadStateCoordinator pageLoadStateCoordinator,
		FrpcSettingsService frpcSettingsService,
		FrpcManagerService frpcManagerService,
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
		Action<string> applyBackdropMaterial,
		Action<bool, string> applyCustomBackground,
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
		_accountCenterService = accountCenterService;
		_signCaptchaDialogService = signCaptchaDialogService;
		_tunnelsViewModel = tunnelsViewModel;
		_pageLoadStateCoordinator = pageLoadStateCoordinator;
		_frpcSettingsService = frpcSettingsService;
		_frpcManagerService = frpcManagerService;
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
		_applyBackdropMaterial = applyBackdropMaterial;
		_applyCustomBackground = applyCustomBackground;
		_showSuccessToast = showSuccessToast;
		_refreshAvatar = refreshAvatar;
		_dispatcherQueue = dispatcherQueue;
	}

	public void InitializeStartupSettings()
	{
		InitializeThemeSetting();
		InitializeBackdropMaterialSetting();
		InitializeCustomBackgroundSetting();
		InitializeAboutInfo();
		_refreshAvatar();
		InitializeSecurityAccessSetting();
		InitializeAutoStartSetting();
		InitializeAutoStartTunnelsSetting();
	}

	public void RefreshUi()
	{
		SettingsSectionView? view = _viewAccessor();
		if (view?.IsRootRoute == true && !view.HasRegisteredPageControls)
		{
			return;
		}

		InitializeThemeSetting();
		InitializeAboutInfo();
		InitializeAutoStartSetting();
		RefreshSecurityAccessSettingsUi();

		if (AutoStartTunnelsExpander != null || AutoStartTunnelListStatusText != null)
		{
			InitializeAutoStartTunnelsSetting();
			_ = RefreshAutoStartTunnelChecklistAsync(forceReload: false);
		}

		if (view?.AvatarStatusText != null)
		{
			_refreshAvatar();
		}

		if (FrpcInstallButton != null || FrpcStatusIcon != null || FrpcInstallBusyText != null)
		{
			_ = RefreshFrpcInstallStateAsync();
		}
	}

	public void HandleViewModelPropertyChanged(string? propertyName)
	{
		if (string.Equals(propertyName, nameof(SettingsViewModel.ThemeMode), StringComparison.Ordinal))
		{
			_applyThemeMode(_settingsViewModel.ThemeMode);
		}
		else if (string.Equals(propertyName, nameof(SettingsViewModel.BackdropMaterial), StringComparison.Ordinal))
		{
			_applyBackdropMaterial(_settingsViewModel.BackdropMaterial);
		}
		else if (string.Equals(propertyName, nameof(SettingsViewModel.IsCustomBackgroundEnabled), StringComparison.Ordinal)
			|| string.Equals(propertyName, nameof(SettingsViewModel.CustomBackgroundPath), StringComparison.Ordinal))
		{
			_applyCustomBackground(_settingsViewModel.IsCustomBackgroundEnabled, _settingsViewModel.CustomBackgroundPath);
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
				AutoStartTunnelsExpander,
				AutoStartTunnelListStatusText,
				"请先登录后查看可选隧道。");
			return;
		}

		if (!_tunnelsViewModel.HasCachedTunnels)
		{
			_autoStartTunnelChecklistRenderer.RenderMessage(
				AutoStartTunnelsExpander,
				AutoStartTunnelListStatusText,
				_pageLoadStateCoordinator.IsLoadingTunnels ? "正在加载隧道列表..." : "暂无可选隧道。");
			return;
		}

		IReadOnlyList<AutoStartTunnelChecklistItem> items = _autoStartSettingsCoordinator.BuildChecklistItems(
			_tunnelsViewModel.Tunnels,
			_autoStartTunnelIds);
		_autoStartTunnelChecklistRenderer.RenderItems(
			AutoStartTunnelsExpander,
			AutoStartTunnelListStatusText,
			items,
			AutoStartTunnelItemCheckBoxCheckedChanged);
	}

	public async Task RefreshAutoStartTunnelChecklistAsync(bool forceReload)
	{
		if (AutoStartTunnelsExpander == null || AutoStartTunnelListStatusText == null)
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
			if (!forceReload && _autoStartTunnelChecklistRefreshTask is { IsCompleted: false })
			{
				return;
			}

			_autoStartTunnelChecklistRefreshTask = _loadTunnelsAsync(forceReload);
			await _autoStartTunnelChecklistRefreshTask;
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
		XamlRoot? xamlRoot = _xamlRootProvider();
		if (xamlRoot == null)
		{
			return;
		}

		string[] installedPaths = _frpcManagerService.GetInstalledExecutablePaths();
		if (installedPaths.Length > 0)
		{
			await ApplySettingsOperationResultAsync(await _settingsOperationCoordinator.InstallOrUninstallFrpcAsync(
				_settingsViewModel.SetFrpcStatusText,
				SetFrpcOperationBusyState));
			return;
		}

		var downloadDialog = new FrpcDownloadDialog
		{
			XamlRoot = xamlRoot
		};

		Task<bool> downloadTask = DownloadFrpcWithProgressAsync(downloadDialog);
		await DialogHost.ShowAsync(downloadDialog);

		if (await downloadTask)
		{
			await RefreshFrpcInstallStateAsync(force: true);
		}
		else if (downloadTask.IsFaulted)
		{
			await _userDialogService.ShowInfoAsync("下载失败", downloadTask.Exception?.Message ?? "下载过程中发生错误。");
		}
	}

	private async Task<bool> DownloadFrpcWithProgressAsync(FrpcDownloadDialog dialog)
	{
		try
		{
			string architecture = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "amd64";
			dialog.SetStatus($"正在下载 {architecture} 核心...");
			dialog.SetIndeterminate();

			string tempDir = Path.Combine(Path.GetTempPath(), "znext-frpc-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempDir);
			string downloadPath = Path.Combine(tempDir, "mefrpc.zip");

			string downloadUrl = architecture == "arm64"
				? "https://drive.mcsl.com.cn/d/ME-Frp/Lanzou/MEFrp-Core/0.67.1_20260626_af59eefd/mefrpc_windows_arm64_0.67.1_20260626_af59eefd.zip"
				: "https://drive.mcsl.com.cn/d/ME-Frp/Lanzou/MEFrp-Core/0.67.1_20260626_af59eefd/mefrpc_windows_amd64_0.67.1_20260626_af59eefd.zip";

			bool curlSuccess = await DownloadWithCurlAsync(downloadUrl, downloadPath, dialog);
			if (!curlSuccess)
			{
				dialog.SetError("下载失败，请检查网络连接或稍后重试。");
				return false;
			}

			dialog.SetStatus("正在解压并安装...");
			dialog.SetIndeterminate();

			string targetPath = _frpcManagerService.GetExecutablePath();
			string? extractedExe = null;

			if (downloadPath.ToLowerInvariant().EndsWith(".zip"))
			{
				string extractDir = Path.Combine(tempDir, "extract");
				Directory.CreateDirectory(extractDir);
				await Task.Run(() => ZipFile.ExtractToDirectory(downloadPath, extractDir));

				string[] candidates = { "mefrpc.exe", "frpc.exe" };
				foreach (string candidate in candidates)
				{
					string? found = Directory.GetFiles(extractDir, candidate, SearchOption.AllDirectories).FirstOrDefault();
					if (!string.IsNullOrEmpty(found))
					{
						extractedExe = found;
						break;
					}
				}

				if (string.IsNullOrEmpty(extractedExe))
				{
					dialog.SetError("压缩包中未找到可执行文件。");
					return false;
				}
			}
			else
			{
				extractedExe = downloadPath;
			}

			bool copySuccess = await CopyWithElevationAsync(extractedExe, targetPath);
			if (!copySuccess)
			{
				dialog.SetError("安装失败，请确认已同意管理员权限。");
				return false;
			}

			dialog.SetStatus("安装完成");
			dialog.SetProgress(100, "完成");

			try
			{
				if (Directory.Exists(tempDir))
				{
					Directory.Delete(tempDir, recursive: true);
				}
			}
			catch { }

			dialog.Hide();
			return true;
		}
		catch (Exception ex)
		{
			dialog.SetError("下载失败: " + ex.Message);
			return false;
		}
	}

	private static async Task<bool> DownloadWithCurlAsync(string url, string outputPath, FrpcDownloadDialog dialog)
	{
		try
		{
			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = "curl",
				Arguments = $"-L -s -o \"{outputPath}\" \"{url}\"",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardError = true
			};

			using Process process = new Process { StartInfo = psi };
			process.Start();
			await Task.Run(() => process.WaitForExit());
			return process.ExitCode == 0 && File.Exists(outputPath) && new FileInfo(outputPath).Length > 0;
		}
		catch
		{
			return false;
		}
	}

	private static async Task<bool> CopyWithElevationAsync(string sourcePath, string targetPath)
	{
		string script = @"
param([string]$SourcePath,[string]$TargetPath)
$ErrorActionPreference = 'Stop'
if (Test-Path $TargetPath) { Remove-Item $TargetPath -Force }
Copy-Item -Path $SourcePath -Destination $TargetPath -Force";

		string tempScript = Path.Combine(Path.GetTempPath(), "znext-admin-copy-" + Guid.NewGuid().ToString("N") + ".ps1");
		await File.WriteAllTextAsync(tempScript, script, new UTF8Encoding(false));

		try
		{
			StringBuilder argsBuilder = new StringBuilder();
			argsBuilder.Append("-NoProfile -ExecutionPolicy Bypass -File \"").Append(tempScript).Append("\"");
			argsBuilder.Append(" -SourcePath \"").Append(sourcePath.Replace("\"", "`\"")).Append("\"");
			argsBuilder.Append(" -TargetPath \"").Append(targetPath.Replace("\"", "`\"")).Append("\"");

			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe"),
				Arguments = argsBuilder.ToString(),
				UseShellExecute = true,
				Verb = "runas",
				CreateNoWindow = false
			};

			using Process? process = Process.Start(startInfo);
			if (process == null)
			{
				return false;
			}

			await Task.Run(() => process.WaitForExit());
			return process.ExitCode == 0;
		}
		catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
		{
			return false;
		}
		finally
		{
			try { if (File.Exists(tempScript)) File.Delete(tempScript); } catch { }
		}
	}

	private static string FormatBytes(long bytes)
	{
		string[] sizes = { "B", "KB", "MB", "GB" };
		int order = 0;
		double size = bytes;
		while (size >= 1024 && order < sizes.Length - 1)
		{
			order++;
			size /= 1024;
		}
		return $"{size:0.##} {sizes[order]}";
	}

	private static async Task<string> SaveCustomBackgroundAsync(StorageFile pickedFile)
	{
		StorageFolder backgroundFolder = await ApplicationData.Current.LocalFolder
			.CreateFolderAsync(CustomBackgroundFolderName, CreationCollisionOption.OpenIfExists)
			.AsTask();

		IReadOnlyList<StorageFile> existingFiles = await backgroundFolder.GetFilesAsync().AsTask();
		foreach (StorageFile existingFile in existingFiles)
		{
			await existingFile.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask();
		}

		string extension = Path.GetExtension(pickedFile.Name);
		if (string.IsNullOrWhiteSpace(extension))
		{
			extension = ".png";
		}

		string fileName = $"custom-background-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{extension.ToLowerInvariant()}";
		StorageFile savedFile = await pickedFile
			.CopyAsync(backgroundFolder, fileName, NameCollisionOption.ReplaceExisting)
			.AsTask();

		return savedFile.Path;
	}

	public async Task HandleFetchUpdateAsync()
	{
		await ApplySettingsOperationResultAsync(await _settingsOperationCoordinator.CheckLauncherUpdateAsync(_settingsViewModel.SetFetchUpdateBusy));
	}

	public async Task HandleUploadAvatarAsync()
	{
		await ApplySettingsOperationResultAsync(await _settingsOperationCoordinator.PickAvatarAsync(_ownerHwndProvider()));
	}

	public async Task HandleClearAvatarAsync()
	{
		await ApplySettingsOperationResultAsync(await _settingsOperationCoordinator.ClearAvatarAsync());
	}

	public async Task HandleSelectCustomBackgroundAsync()
	{
		try
		{
			FileOpenPicker picker = new FileOpenPicker
			{
				ViewMode = PickerViewMode.Thumbnail,
				SuggestedStartLocation = PickerLocationId.PicturesLibrary
			};
			picker.FileTypeFilter.Add(".png");
			picker.FileTypeFilter.Add(".jpg");
			picker.FileTypeFilter.Add(".jpeg");
			picker.FileTypeFilter.Add(".bmp");
			picker.FileTypeFilter.Add(".webp");
			picker.FileTypeFilter.Add(".mp4");

			InitializeWithWindow.Initialize(picker, _ownerHwndProvider());
			StorageFile? pickedFile = await picker.PickSingleFileAsync();
			if (pickedFile == null)
			{
				return;
			}

			string savedPath = await SaveCustomBackgroundAsync(pickedFile);
			_settingsViewModel.CustomBackgroundPath = savedPath;
			_settingsViewModel.IsCustomBackgroundEnabled = true;
			_showSuccessToast("背景已应用");
		}
		catch (Exception ex)
		{
			await _userDialogService.ShowInfoAsync("自定义背景", "背景应用失败：" + ex.Message);
		}
	}

	public void HandleClearCustomBackground()
	{
		_settingsViewModel.IsCustomBackgroundEnabled = false;
		_settingsViewModel.CustomBackgroundPath = string.Empty;
		_showSuccessToast("背景图已清除");
	}

	public async Task HandleOpenFrpcDirectoryAsync()
	{
		await ShowUserActionResultAsync(_userActionCoordinator.OpenFrpcDirectory());
	}

	public async Task HandleLaunchArgsAsync()
	{
		XamlRoot? xamlRoot = _xamlRootProvider();
		if (xamlRoot == null)
		{
			return;
		}

		string currentArgs = _settingsViewModel.FrpcLaunchArgs ?? string.Empty;
		var dialog = new LaunchArgsDialog
		{
			XamlRoot = xamlRoot
		};
		dialog.SetLaunchArgs(currentArgs);

		ContentDialogResult result = await DialogHost.ShowAsync(dialog);
		if (result != ContentDialogResult.Primary)
		{
			return;
		}

		_settingsViewModel.FrpcLaunchArgs = dialog.LaunchArgs ?? string.Empty;
		await _userDialogService.ShowInfoAsync("启动参数", "启动参数已保存。");
	}

	public async Task HandleCopyAccessTokenAsync()
	{
		await ShowUserActionResultAsync(_userActionCoordinator.CopyAccessToken(_userSessionService.Token));
	}

	public async Task HandleUserCenterActionAsync(string action)
	{
		PrepareAccountCenterService();
		switch (action)
		{
			case "RedeemCode":
				await HandleRedeemCodeAsync();
				break;
			case "CdkHistory":
				await HandleCdkHistoryAsync();
				break;
			case "DomainWhitelist":
				await HandleDomainWhitelistAsync();
				break;
			case "AuditLogs":
				await HandleAuditLogsAsync();
				break;
			case "KickAllProxies":
				await HandleKickAllProxiesAsync();
				break;
			case "ChangePassword":
				await HandleChangeAccountPasswordAsync();
				break;
			case "ForgotPassword":
				await _userDialogService.ShowInfoAsync("找回密码", "找回密码属于未登录流程，当前公开面板未暴露可复用的登录态内 API。请退出登录后使用登录页的找回密码入口。");
				break;
			case "ResetAccessToken":
				await HandleResetAccessTokenAsync();
				break;
			case "RobotBindingKey":
				await HandleRobotBindingKeyAsync();
				break;
		}
	}

	private void PrepareAccountCenterService()
	{
		if (_userSessionService.TryGetToken(out string token))
		{
			_accountCenterService.SetToken(token);
			return;
		}

		_accountCenterService.ClearToken();
	}

	private async Task HandleRedeemCodeAsync()
	{
		string? code = await ShowTextInputAsync("使用兑换码", "兑换码", "请输入 32 位兑换码", 32);
		if (string.IsNullOrWhiteSpace(code))
		{
			return;
		}

		string? captchaToken = await _signCaptchaDialogService.ShowAsync(_xamlRootProvider());
		if (string.IsNullOrWhiteSpace(captchaToken))
		{
			return;
		}

		AccountApiResult<string> result = await _accountCenterService.RedeemCdkAsync(code.Trim(), captchaToken);
		await ShowApiResultAsync("使用兑换码", result, string.IsNullOrWhiteSpace(result.Data) ? "兑换成功。" : "兑换成功，" + result.Data);
	}

	private async Task HandleCdkHistoryAsync()
	{
		AccountApiResult<List<CdkUsageLog>> result = await _accountCenterService.GetCdkUsageAsync();
		if (!result.Success || result.Data == null)
		{
			await ShowApiResultAsync("兑换历史", result);
			return;
		}

		string content = result.Data.Count == 0
			? "暂无兑换记录。"
			: string.Join(Environment.NewLine + Environment.NewLine, result.Data.Select(log =>
				$"{FormatCdkType(log.Type)}  {FormatUnixTime(log.UseTime)}{Environment.NewLine}{MaskCode(log.Code)}  {FormatCdkValue(log.Type, log.Value)}"));
		await ShowScrollableTextAsync("兑换历史", content);
	}

	private async Task HandleDomainWhitelistAsync()
	{
		AccountApiResult<List<IcpDomainInfo>> result = await _accountCenterService.GetIcpDomainsAsync();
		if (!result.Success || result.Data == null)
		{
			await ShowApiResultAsync("域名白名单", result);
			return;
		}

		XamlRoot? xamlRoot = _xamlRootProvider();
		if (xamlRoot == null)
		{
			return;
		}

		var whitelistDialog = new DomainWhitelistDialog
		{
			XamlRoot = xamlRoot
		};
		whitelistDialog.SetDomains(result.Data);
		await DialogHost.ShowAsync(whitelistDialog);

		if (!whitelistDialog.ShouldOpenAddDialog)
		{
			return;
		}

		var addDialog = new AddDomainDialog
		{
			XamlRoot = xamlRoot
		};
		ContentDialogResult addResult = await DialogHost.ShowAsync(addDialog);
		if (addResult != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(addDialog.DomainText))
		{
			return;
		}

		AccountApiResult<string> apiResult = await _accountCenterService.AddIcpDomainAsync(addDialog.DomainText);
		await ShowApiResultAsync("添加域名", apiResult, "域名已添加。");
	}

	private async Task HandleAuditLogsAsync()
	{
		AccountApiResult<List<AuditLogInfo>> result = await _accountCenterService.GetAuditLogsAsync();
		if (!result.Success || result.Data == null)
		{
			await ShowApiResultAsync("审计日志", result);
			return;
		}

		string content = result.Data.Count == 0
			? "暂无审计日志。"
			: string.Join(Environment.NewLine + Environment.NewLine, result.Data.Select(log =>
				$"#{log.LogId}  {FormatAuditCategory(log.Category)}  {FormatAuditStatus(log.Status)}{Environment.NewLine}{log.CreatedAt}  {log.IpAddress}{Environment.NewLine}{log.Details}"));
		await ShowScrollableTextAsync("审计日志", content);
	}

	private async Task HandleKickAllProxiesAsync()
	{
		if (!await _userDialogService.ShowConfirmAsync("下线全部隧道", "确定要下线账户名下所有在线隧道吗？"))
		{
			return;
		}

		AccountApiResult<string> result = await _accountCenterService.KickAllProxiesAsync();
		await ShowApiResultAsync("下线全部隧道", result, "所有隧道已下线。");
	}

	private async Task HandleChangeAccountPasswordAsync()
	{
		(string OldPassword, string NewPassword)? input = await ShowPasswordChangeDialogAsync();
		if (input == null)
		{
			return;
		}

		AccountApiResult<string> result = await _accountCenterService.ChangePasswordAsync(input.Value.OldPassword, input.Value.NewPassword);
		if (result.Success)
		{
			_userSessionService.Clear();
		}

		await ShowApiResultAsync("更改密码", result, "密码已更改，请重新登录。");
	}

	private async Task HandleResetAccessTokenAsync()
	{
		if (!await _userDialogService.ShowConfirmAsync("重置访问密钥", "重置后原访问密钥将失效，并可能导致在线隧道下线。确定继续吗？"))
		{
			return;
		}

		string? captchaToken = await _signCaptchaDialogService.ShowAsync(_xamlRootProvider());
		if (string.IsNullOrWhiteSpace(captchaToken))
		{
			return;
		}

		AccountApiResult<string> result = await _accountCenterService.ResetAccessTokenAsync(captchaToken);
		if (result.Success && !string.IsNullOrWhiteSpace(result.Data))
		{
			_userSessionService.SetToken(result.Data);
			await ShowUserActionResultAsync(_userActionCoordinator.CopyAccessToken(result.Data));
			return;
		}

		await ShowApiResultAsync("重置访问密钥", result, "访问密钥已重置。");
	}

	private async Task HandleRobotBindingKeyAsync()
	{
		AccountApiResult<string> result = await _accountCenterService.GetRobotBindingKeyAsync();
		if (result.Success && !string.IsNullOrWhiteSpace(result.Data))
		{
			await ShowUserActionResultAsync(_userActionCoordinator.CopyAccessToken(result.Data));
			return;
		}

		await ShowApiResultAsync("机器人绑定密钥", result);
	}

	private async Task<string?> ShowTextInputAsync(string title, string header, string placeholder, int maxLength)
	{
		XamlRoot? xamlRoot = _xamlRootProvider();
		if (xamlRoot == null)
		{
			return null;
		}

		TextBox input = new TextBox
		{
			Header = header,
			PlaceholderText = placeholder,
			MaxLength = maxLength
		};
		ContentDialog dialog = ModernDialogFactory.Create(
			xamlRoot,
			title,
			input,
			primaryButtonText: "确认",
			closeButtonText: "取消",
			defaultButton: ContentDialogButton.Primary);
		ContentDialogResult result = await DialogHost.ShowAsync(dialog);
		return result == ContentDialogResult.Primary ? input.Text : null;
	}

	private async Task<(string OldPassword, string NewPassword)?> ShowPasswordChangeDialogAsync()
	{
		XamlRoot? xamlRoot = _xamlRootProvider();
		if (xamlRoot == null)
		{
			return null;
		}

		PasswordBox oldPasswordBox = new PasswordBox { Header = "原密码", PlaceholderText = "请输入原密码" };
		PasswordBox newPasswordBox = new PasswordBox { Header = "新密码", PlaceholderText = "请输入新密码" };
		PasswordBox confirmPasswordBox = new PasswordBox { Header = "确认密码", PlaceholderText = "请再次输入新密码" };
		TextBlock errorText = new TextBlock
		{
			Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 196, 43, 28)),
			TextWrapping = TextWrapping.Wrap,
			Visibility = Visibility.Collapsed
		};
		StackPanel panel = new StackPanel
		{
			Spacing = 12,
			Children =
			{
				oldPasswordBox,
				newPasswordBox,
				confirmPasswordBox,
				errorText
			}
		};
		ContentDialog dialog = ModernDialogFactory.Create(
			xamlRoot,
			"更改密码",
			panel,
			primaryButtonText: "确认",
			closeButtonText: "取消",
			defaultButton: ContentDialogButton.Primary);
		dialog.PrimaryButtonClick += (_, args) =>
		{
			string oldPassword = oldPasswordBox.Password ?? string.Empty;
			string newPassword = newPasswordBox.Password ?? string.Empty;
			string confirmPassword = confirmPasswordBox.Password ?? string.Empty;
			string? error = ValidatePasswordChange(oldPassword, newPassword, confirmPassword);
			if (error == null)
			{
				return;
			}

			args.Cancel = true;
			errorText.Text = error;
			errorText.Visibility = Visibility.Visible;
		};

		ContentDialogResult result = await DialogHost.ShowAsync(dialog);
		return result == ContentDialogResult.Primary
			? (oldPasswordBox.Password ?? string.Empty, newPasswordBox.Password ?? string.Empty)
			: null;
	}

	private static string? ValidatePasswordChange(string oldPassword, string newPassword, string confirmPassword)
	{
		if (string.IsNullOrWhiteSpace(oldPassword))
		{
			return "请输入原密码。";
		}

		if (string.IsNullOrWhiteSpace(newPassword))
		{
			return "请输入新密码。";
		}

		if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
		{
			return "两次输入的新密码不一致。";
		}

		return null;
	}

	private async Task ShowApiResultAsync<T>(string title, AccountApiResult<T> result, string? successMessage = null)
	{
		if (result.Success)
		{
			await _userDialogService.ShowInfoAsync(title, successMessage ?? result.Message);
			return;
		}

		await _userDialogService.ShowInfoAsync(title, string.IsNullOrWhiteSpace(result.Error) ? result.Message : result.Message + Environment.NewLine + result.Error);
	}

	private async Task ShowScrollableTextAsync(string title, string text)
	{
		XamlRoot? xamlRoot = _xamlRootProvider();
		if (xamlRoot == null)
		{
			return;
		}

		TextBlock textBlock = new TextBlock
		{
			Text = text,
			TextWrapping = TextWrapping.Wrap,
			IsTextSelectionEnabled = true
		};
		ContentDialog dialog = ModernDialogFactory.Create(
			xamlRoot,
			title,
			ModernDialogFactory.Scrollable(textBlock),
			closeButtonText: "确定",
			defaultButton: ContentDialogButton.Close);
		await DialogHost.ShowAsync(dialog);
	}

	private static string FormatUnixTime(long unixTime)
	{
		return unixTime <= 0
			? "-"
			: DateTimeOffset.FromUnixTimeSeconds(unixTime).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
	}

	private static string MaskCode(string code)
	{
		if (string.IsNullOrWhiteSpace(code) || code.Length <= 12)
		{
			return code;
		}

		return code[..8] + "..." + code[^4..];
	}

	private static string FormatCdkType(string type)
	{
		return type switch
		{
			"traffic" => "流量包",
			"proxy" => "隧道数",
			"vip" => "VIP",
			_ => string.IsNullOrWhiteSpace(type) ? "未知" : type
		};
	}

	private static string FormatCdkValue(string type, string value)
	{
		return type switch
		{
			"traffic" => $"获得流量：{value} GB",
			"proxy" => $"获得隧道数：{value}",
			"vip" => $"获得 VIP：{value} 天",
			_ => string.IsNullOrWhiteSpace(value) ? "-" : value
		};
	}

	private static string FormatAuditCategory(string category)
	{
		return category switch
		{
			"auth" => "认证",
			"proxy" => "隧道",
			"node" => "节点",
			"user" => "用户",
			"finance" => "财务",
			"domain" => "域名",
			_ => string.IsNullOrWhiteSpace(category) ? "-" : category
		};
	}

	private static string FormatAuditStatus(string status)
	{
		return string.Equals(status, "success", StringComparison.OrdinalIgnoreCase) ? "成功" : "失败";
	}

	private TextBlock? AutoStartTunnelListStatusText => _viewAccessor()?.AutoStartTunnelListStatusText;

	private SettingsExpander? AutoStartTunnelsExpander => _viewAccessor()?.AutoStartTunnelsExpander;

	private ProgressRing? FrpcInstallBusyRing => _viewAccessor()?.FrpcInstallBusyRing;

	private TextBlock? FrpcInstallBusyText => _viewAccessor()?.FrpcInstallBusyText;

	private Button? FrpcInstallButton => _viewAccessor()?.FrpcInstallButton;

	private FontIcon? FrpcStatusIcon => _viewAccessor()?.FrpcStatusIcon;

	private void InitializeThemeSetting()
	{
		_applyThemeMode(_settingsViewModel.LoadThemeMode());
	}

	private void InitializeBackdropMaterialSetting()
	{
		_applyBackdropMaterial(_settingsViewModel.LoadBackdropMaterial());
	}

	private void InitializeCustomBackgroundSetting()
	{
		(bool isEnabled, string path) = _settingsViewModel.LoadCustomBackground();
		_applyCustomBackground(isEnabled, path);
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

	private async Task RefreshFrpcInstallStateAsync(bool force = false)
	{
		if (!force
			&& _cachedFrpcInstallState != null
			&& DateTimeOffset.UtcNow - _lastFrpcInstallRefreshUtc < FrpcInstallRefreshInterval)
		{
			ApplyFrpcInstallState(_cachedFrpcInstallState);
			return;
		}

		if (!force && _frpcInstallRefreshTask is { IsCompleted: false })
		{
			return;
		}

		_frpcInstallRefreshTask = RefreshFrpcInstallStateCoreAsync();
		await _frpcInstallRefreshTask;
	}

	private async Task RefreshFrpcInstallStateCoreAsync()
	{
		try
		{
			FrpcInstallState state = await _frpcSettingsService.GetInstallStateAsync();
			_cachedFrpcInstallState = state;
			_lastFrpcInstallRefreshUtc = DateTimeOffset.UtcNow;
			ApplyFrpcInstallState(state);
		}
		catch (Exception ex)
		{
			FrpcInstallState state = FrpcInstallState.Error("状态: 检测失败 - " + ex.Message);
			_cachedFrpcInstallState = state;
			_lastFrpcInstallRefreshUtc = DateTimeOffset.UtcNow;
			ApplyFrpcInstallState(state);
		}
	}

	private void ApplyFrpcInstallState(FrpcInstallState state)
	{
		_frpcInstallVisualController.UpdateInstallButton(FrpcInstallButton, state.IsInstalled);
		_frpcInstallVisualController.UpdateStatusIcon(FrpcStatusIcon, state.IsInstalled, state.IsError);
		_settingsViewModel.SetFrpcStatusText(state.StatusText);
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
			await RefreshFrpcInstallStateAsync(force: true);
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
