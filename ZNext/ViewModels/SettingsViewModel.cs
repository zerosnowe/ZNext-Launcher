using ZNext.Infrastructure.Settings;
using WinRT;

namespace ZNext.ViewModels;

[GeneratedBindableCustomProperty]
internal sealed partial class SettingsViewModel : ObservableObject
{
	public const string ThemeSettingKey = "AppThemeMode";
	public const string BackdropMaterialSettingKey = "BackdropMaterial";
	public const string CustomBackgroundEnabledSettingKey = "CustomBackgroundEnabled";
	public const string CustomBackgroundPathSettingKey = "CustomBackgroundPath";
	public const string FrpcLaunchArgsKey = "FrpcLaunchArgs";

	private readonly IAppSettingsStore _settingsStore;
	private string _themeMode = "Default";
	private string _backdropMaterial = "Mica";
	private bool _isCustomBackgroundEnabled;
	private string _customBackgroundPath = string.Empty;
	private string _aboutVersionText = "Version -";
	private string _fetchUpdateButtonText = "获取更新";
	private string _frpcStatusText = "状态: 未安装";
	private string _frpcLaunchArgs = string.Empty;
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

	public string BackdropMaterial
	{
		get => _backdropMaterial;
		set
		{
			string normalized = NormalizeBackdropMaterial(value);
			if (SetProperty(ref _backdropMaterial, normalized))
			{
				_settingsStore.SetString(BackdropMaterialSettingKey, normalized);
			}
		}
	}

	public bool IsCustomBackgroundEnabled
	{
		get => _isCustomBackgroundEnabled;
		set
		{
			if (SetProperty(ref _isCustomBackgroundEnabled, value))
			{
				_settingsStore.SetBool(CustomBackgroundEnabledSettingKey, value);
				OnPropertyChanged(nameof(CustomBackgroundDisplayText));
			}
		}
	}

	public string CustomBackgroundPath
	{
		get => _customBackgroundPath;
		set
		{
			string normalized = value ?? string.Empty;
			if (SetProperty(ref _customBackgroundPath, normalized))
			{
				if (string.IsNullOrWhiteSpace(normalized))
				{
					_settingsStore.Remove(CustomBackgroundPathSettingKey);
				}
				else
				{
					_settingsStore.SetString(CustomBackgroundPathSettingKey, normalized);
				}

				OnPropertyChanged(nameof(CustomBackgroundDisplayText));
			}
		}
	}

	public string CustomBackgroundDisplayText
	{
		get
		{
			if (string.IsNullOrWhiteSpace(CustomBackgroundPath))
			{
				return "未选择背景";
			}

			string fileName = Path.GetFileName(CustomBackgroundPath);
			if (!File.Exists(CustomBackgroundPath))
			{
				return string.IsNullOrWhiteSpace(fileName) ? "背景文件不存在" : $"文件不存在：{fileName}";
			}

			return IsCustomBackgroundEnabled
				? $"已启用：{fileName}"
				: $"已选择：{fileName}";
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

	public string FrpcLaunchArgs
	{
		get => _frpcLaunchArgs;
		set
		{
			if (SetProperty(ref _frpcLaunchArgs, value ?? string.Empty))
			{
				_settingsStore.SetString(FrpcLaunchArgsKey, value ?? string.Empty);
			}
		}
	}

	public void Load()
	{
		string normalized = NormalizeThemeMode(_settingsStore.GetString(ThemeSettingKey) ?? "Default");
		SetProperty(ref _themeMode, normalized, nameof(ThemeMode));
		string backdropMaterial = NormalizeBackdropMaterial(_settingsStore.GetString(BackdropMaterialSettingKey) ?? "Mica");
		SetProperty(ref _backdropMaterial, backdropMaterial, nameof(BackdropMaterial));
		bool isCustomBackgroundEnabled = _settingsStore.GetBool(CustomBackgroundEnabledSettingKey);
		SetProperty(ref _isCustomBackgroundEnabled, isCustomBackgroundEnabled, nameof(IsCustomBackgroundEnabled));
		string customBackgroundPath = _settingsStore.GetString(CustomBackgroundPathSettingKey) ?? string.Empty;
		SetProperty(ref _customBackgroundPath, customBackgroundPath, nameof(CustomBackgroundPath));
		OnPropertyChanged(nameof(CustomBackgroundDisplayText));
		string launchArgs = _settingsStore.GetString(FrpcLaunchArgsKey) ?? string.Empty;
		SetProperty(ref _frpcLaunchArgs, launchArgs, nameof(FrpcLaunchArgs));
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

	public string LoadBackdropMaterial()
	{
		Load();
		return BackdropMaterial;
	}

	public (bool IsEnabled, string Path) LoadCustomBackground()
	{
		Load();
		return (IsCustomBackgroundEnabled, CustomBackgroundPath);
	}

	public void SetAboutVersion(string version)
	{
		AboutVersionText = "Version " + version;
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

	public static string NormalizeBackdropMaterial(string material)
	{
		return string.Equals(material, "Acrylic", StringComparison.OrdinalIgnoreCase) ? "Acrylic" : "Mica";
	}
}
