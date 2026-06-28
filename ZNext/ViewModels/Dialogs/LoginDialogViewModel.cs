using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WinRT;
using ZNext.Services;

namespace ZNext.ViewModels.Dialogs;

[GeneratedBindableCustomProperty]
internal sealed partial class LoginDialogViewModel : ObservableObject
{
	private static readonly SolidColorBrush SecondaryBrush = new SolidColorBrush(Color.FromArgb(255, 107, 114, 128));
	private static readonly SolidColorBrush ErrorBrush = new SolidColorBrush(Color.FromArgb(255, 196, 43, 28));
	private static readonly SolidColorBrush SuccessBrush = new SolidColorBrush(Color.FromArgb(255, 16, 124, 16));
	private readonly AuthService _authService;
	private string _username = string.Empty;
	private string _password = string.Empty;
	private string _captchaInput = string.Empty;
	private string _captchaStatusText = string.Empty;
	private Brush _captchaStatusBrush = SecondaryBrush;
	private string _errorMessage = string.Empty;
	private bool _rememberMe = true;
	private bool _isBusy;

	public LoginDialogViewModel(AuthService authService)
	{
		_authService = authService;
		LoadRememberedLoginState();
	}

	public string Username
	{
		get => _username;
		set
		{
			if (SetProperty(ref _username, value ?? string.Empty))
			{
				HideError();
				OnPropertyChanged(nameof(CanLogin));
			}
		}
	}

	public string Password
	{
		get => _password;
		set
		{
			if (SetProperty(ref _password, value ?? string.Empty))
			{
				HideError();
				OnPropertyChanged(nameof(CanLogin));
			}
		}
	}

	public string CaptchaInput
	{
		get => _captchaInput;
		set
		{
			if (SetProperty(ref _captchaInput, value ?? string.Empty))
			{
				HideError();
				UpdateCaptchaStatus();
				OnPropertyChanged(nameof(CanLogin));
			}
		}
	}

	public string CaptchaStatusText
	{
		get => _captchaStatusText;
		private set => SetProperty(ref _captchaStatusText, value);
	}

	public Brush CaptchaStatusBrush
	{
		get => _captchaStatusBrush;
		private set => SetProperty(ref _captchaStatusBrush, value);
	}

	public string ErrorMessage
	{
		get => _errorMessage;
		private set
		{
			if (SetProperty(ref _errorMessage, value))
			{
				OnPropertyChanged(nameof(ErrorVisibility));
			}
		}
	}

	public Visibility ErrorVisibility => string.IsNullOrWhiteSpace(ErrorMessage) ? Visibility.Collapsed : Visibility.Visible;

	public bool RememberMe
	{
		get => _rememberMe;
		set => SetProperty(ref _rememberMe, value);
	}

	public bool IsBusy
	{
		get => _isBusy;
		private set
		{
			if (SetProperty(ref _isBusy, value))
			{
				OnPropertyChanged(nameof(CanLogin));
				OnPropertyChanged(nameof(ProgressVisibility));
			}
		}
	}

	public Visibility ProgressVisibility => IsBusy ? Visibility.Visible : Visibility.Collapsed;

	public bool CanLogin => !IsBusy
		&& !string.IsNullOrWhiteSpace(Username)
		&& !string.IsNullOrWhiteSpace(Password)
		&& !string.IsNullOrWhiteSpace(CaptchaToken);

	public string? Token { get; private set; }

	public string? CaptchaToken => DecodeCaptchaToken(CaptchaInput);

	public async Task<bool> LoginAsync()
	{
		if (IsBusy)
		{
			return false;
		}

		try
		{
			HideError();
			if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
			{
				ShowError("请输入用户名和密码。");
				return false;
			}

			IsBusy = true;
			string? captchaToken = CaptchaToken;
			if (string.IsNullOrWhiteSpace(captchaToken))
			{
				ShowError("请先完成验证码并输入验证码。");
				return false;
			}

			LoginResult result = await _authService.LoginAsync(Username.Trim(), Password, captchaToken, rememberLogin: true);
			if (!result.Success)
			{
				ShowError(string.IsNullOrWhiteSpace(result.Error)
					? result.Message
					: $"{result.Message}\n详情: {result.Error}");
				return false;
			}

			Token = result.Token;
			if (!string.IsNullOrWhiteSpace(Token))
			{
				_authService.SetToken(Token, persist: true);
				_authService.SaveLoginPreferences(Username.Trim(), RememberMe);
			}

			return !string.IsNullOrWhiteSpace(Token);
		}
		catch (Exception ex)
		{
			ShowError($"登录异常: {ex.Message}");
			return false;
		}
		finally
		{
			IsBusy = false;
		}
	}

	public void ApplyInputSnapshot(string? username, string? password, string? captchaInput)
	{
		Username = username ?? string.Empty;
		Password = password ?? string.Empty;
		CaptchaInput = captchaInput ?? string.Empty;
	}

	public void MarkCaptchaLoading()
	{
		CaptchaStatusText = "正在加载内嵌验证页面...";
		CaptchaStatusBrush = SecondaryBrush;
	}

	public void MarkCaptchaReady()
	{
		if (string.IsNullOrWhiteSpace(CaptchaInput))
		{
			CaptchaStatusText = "请在内嵌页面完成验证，然后手动粘贴返回的 token。";
			CaptchaStatusBrush = SecondaryBrush;
		}
	}

	public void MarkCaptchaError(string message)
	{
		CaptchaStatusText = message;
		CaptchaStatusBrush = ErrorBrush;
	}

	private void LoadRememberedLoginState()
	{
		RememberMe = _authService.LoadRememberLoginPreference();
		string? rememberedUsername = _authService.LoadRememberedUsername();
		if (!string.IsNullOrWhiteSpace(rememberedUsername))
		{
			Username = rememberedUsername;
		}
	}

	private void UpdateCaptchaStatus()
	{
		if (string.IsNullOrWhiteSpace(CaptchaInput))
		{
			CaptchaStatusText = string.Empty;
			return;
		}

		if (string.IsNullOrWhiteSpace(CaptchaToken))
		{
			CaptchaStatusText = "验证码格式无效。";
			CaptchaStatusBrush = ErrorBrush;
			return;
		}

		CaptchaStatusText = CaptchaInput.Trim() == CaptchaToken ? "验证码已识别。" : "已自动完成 Base64 解码。";
		CaptchaStatusBrush = SuccessBrush;
	}

	private void ShowError(string message)
	{
		ErrorMessage = message;
	}

	private void HideError()
	{
		ErrorMessage = string.Empty;
	}

	internal static string? DecodeCaptchaToken(string? tokenText)
	{
		return CaptchaVerificationBridge.DecodeToken(tokenText);
	}
}
