using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using ZNext.Infrastructure.Commands;
using ZNext.Services;

namespace ZNext.ViewModels.Dialogs;

internal sealed class LoginDialogViewModel : ObservableObject
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
		OpenCaptchaCommand = new RelayCommand(OpenCaptchaInBrowser);
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

	public ICommand OpenCaptchaCommand { get; }

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

	private void LoadRememberedLoginState()
	{
		RememberMe = _authService.LoadRememberLoginPreference();
		string? rememberedUsername = _authService.LoadRememberedUsername();
		if (!string.IsNullOrWhiteSpace(rememberedUsername))
		{
			Username = rememberedUsername;
		}
	}

	private void OpenCaptchaInBrowser()
	{
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = "https://www.mefrp.com/3rdparty/captcha?client=ZNextWinUI3App",
				UseShellExecute = true
			});
			CaptchaStatusText = "已在浏览器打开验证页，请完成验证并复制返回的 Base64 验证码。";
			CaptchaStatusBrush = SecondaryBrush;
		}
		catch (Exception ex)
		{
			CaptchaStatusText = "打开验证页失败: " + ex.Message;
			CaptchaStatusBrush = ErrorBrush;
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
		if (string.IsNullOrWhiteSpace(tokenText))
		{
			return null;
		}

		string token = tokenText.Trim();
		try
		{
			byte[] bytes = Convert.FromBase64String(token);
			string decoded = Encoding.UTF8.GetString(bytes).Trim();
			if (!string.IsNullOrWhiteSpace(decoded))
			{
				token = decoded;
			}
		}
		catch
		{
			// Raw token input is accepted.
		}

		string[] parts = token.Split(new[] { "||" }, StringSplitOptions.None);
		string normalized = parts.Length > 0 ? parts[0].Trim() : token;
		return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
	}
}
