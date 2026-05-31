using System;
using System.Diagnostics;
using System.Windows.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using ZNext.Infrastructure.Commands;

namespace ZNext.ViewModels.Dialogs;

internal sealed class SignCaptchaDialogViewModel : ObservableObject
{
	private static readonly SolidColorBrush SecondaryBrush = new SolidColorBrush(Color.FromArgb(255, 107, 114, 128));
	private static readonly SolidColorBrush ErrorBrush = new SolidColorBrush(Color.FromArgb(255, 196, 43, 28));
	private static readonly SolidColorBrush SuccessBrush = new SolidColorBrush(Color.FromArgb(255, 16, 124, 16));
	private string _captchaInput = string.Empty;
	private string _statusText = "请在浏览器完成验证，然后把返回的 token 粘贴到输入框。";
	private Brush _statusBrush = SecondaryBrush;

	public SignCaptchaDialogViewModel()
	{
		OpenCaptchaCommand = new RelayCommand(OpenCaptchaInBrowser);
	}

	public string CaptchaInput
	{
		get => _captchaInput;
		set
		{
			if (SetProperty(ref _captchaInput, value ?? string.Empty))
			{
				UpdateStatus();
			}
		}
	}

	public string StatusText
	{
		get => _statusText;
		private set => SetProperty(ref _statusText, value);
	}

	public Brush StatusBrush
	{
		get => _statusBrush;
		private set => SetProperty(ref _statusBrush, value);
	}

	public string? Token => LoginDialogViewModel.DecodeCaptchaToken(CaptchaInput);

	public ICommand OpenCaptchaCommand { get; }

	public bool Validate()
	{
		if (!string.IsNullOrWhiteSpace(Token))
		{
			return true;
		}

		StatusText = "请先完成验证并输入 token。";
		StatusBrush = ErrorBrush;
		return false;
	}

	private void OpenCaptchaInBrowser()
	{
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = "https://www.mefrp.com/3rdparty/captcha?client=ZNextSignIn",
				UseShellExecute = true
			});
			StatusText = "已在浏览器打开验证页，请完成验证并复制返回的 Base64 验证码。";
			StatusBrush = SecondaryBrush;
		}
		catch (Exception ex)
		{
			StatusText = "打开验证页失败: " + ex.Message;
			StatusBrush = ErrorBrush;
		}
	}

	private void UpdateStatus()
	{
		if (string.IsNullOrWhiteSpace(CaptchaInput))
		{
			StatusText = "请在浏览器完成验证，然后把返回的 token 粘贴到输入框。";
			StatusBrush = SecondaryBrush;
			return;
		}

		if (string.IsNullOrWhiteSpace(Token))
		{
			StatusText = "验证码格式无效。";
			StatusBrush = ErrorBrush;
			return;
		}

		StatusText = CaptchaInput.Trim() == Token ? "验证码已识别。" : "已自动完成 Base64 解码。";
		StatusBrush = SuccessBrush;
	}
}
