using ZNext.Infrastructure.Settings;

namespace ZNext.ViewModels;

internal sealed class SettingsViewModel : ObservableObject
{
	public const string ThemeSettingKey = "AppThemeMode";

	private readonly IAppSettingsStore _settingsStore;
	private string _themeMode = "Default";
	private string _aboutVersionText = "版本: -";
	private string _fetchUpdateButtonText = "获取更新";
	private string _frpcStatusText = "状态: 未安装";
	private bool _isFetchUpdateEnabled = true;
	private bool _isAutoStartEnabled;
	private bool _isAutoStartTunnelsEnabled;
	private bool _isSecurityPasswordEnabled;
	private string _securityPasswordButtonText = "设置密码";

	public SettingsViewModel(IAppSettingsStore settingsStore)
	{
		_settingsStore = settingsStore;
	}

	public string ThemeMode
	{
		get => _themeMode;
		set
		{
			string normalized = NormalizeThemeMode(value);
			if (SetProperty(ref _themeMode, normalized))
			{
				_settingsStore.SetString(ThemeSettingKey, normalized);
			}
		}
	}

	public string AboutVersionText
	{
		get => _aboutVersionText;
		private set => SetProperty(ref _aboutVersionText, value);
	}

	public string FetchUpdateButtonText
	{
		get => _fetchUpdateButtonText;
		private set => SetProperty(ref _fetchUpdateButtonText, value);
	}

	public string FrpcStatusText
	{
		get => _frpcStatusText;
		private set => SetProperty(ref _frpcStatusText, value);
	}

	public bool IsFetchUpdateEnabled
	{
		get => _isFetchUpdateEnabled;
		private set => SetProperty(ref _isFetchUpdateEnabled, value);
	}

	public bool IsAutoStartEnabled
	{
		get => _isAutoStartEnabled;
		set => SetProperty(ref _isAutoStartEnabled, value);
	}

	public bool IsAutoStartTunnelsEnabled
	{
		get => _isAutoStartTunnelsEnabled;
		set => SetProperty(ref _isAutoStartTunnelsEnabled, value);
	}

	public bool IsSecurityPasswordEnabled
	{
		get => _isSecurityPasswordEnabled;
		set => SetProperty(ref _isSecurityPasswordEnabled, value);
	}

	public string SecurityPasswordButtonText
	{
		get => _securityPasswordButtonText;
		private set => SetProperty(ref _securityPasswordButtonText, value);
	}

	public void Load()
	{
		string normalized = NormalizeThemeMode(_settingsStore.GetString(ThemeSettingKey) ?? "Default");
		SetProperty(ref _themeMode, normalized, nameof(ThemeMode));
	}

	public string LoadThemeMode()
	{
		Load();
		return ThemeMode;
	}

	public string SetThemeMode(string mode)
	{
		ThemeMode = mode;
		return ThemeMode;
	}

	public void SetAboutVersion(string version)
	{
		AboutVersionText = "版本: " + version;
	}

	public void SetFetchUpdateBusy(bool isBusy)
	{
		IsFetchUpdateEnabled = !isBusy;
		FetchUpdateButtonText = isBusy ? "获取中..." : "获取更新";
	}

	public void SetFrpcStatusText(string text)
	{
		FrpcStatusText = string.IsNullOrWhiteSpace(text) ? "状态: 未知" : text;
	}

	public void SetSecurityPasswordButtonState(bool hasPassword)
	{
		SecurityPasswordButtonText = hasPassword ? "重置密码" : "设置密码";
	}

	public static string NormalizeThemeMode(string mode)
	{
		return mode == "Light" || mode == "Dark" ? mode : "Default";
	}
}
