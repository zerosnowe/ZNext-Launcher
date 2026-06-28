using System;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WinRT;
using ZNext.Services;

namespace ZNext.ViewModels.Dialogs;

[GeneratedBindableCustomProperty]
internal sealed partial class SignCaptchaDialogViewModel : ObservableObject
{
	private static readonly SolidColorBrush SecondaryBrush = new SolidColorBrush(Color.FromArgb(255, 107, 114, 128));
	private static readonly SolidColorBrush ErrorBrush = new SolidColorBrush(Color.FromArgb(255, 196, 43, 28));
	private static readonly SolidColorBrush SuccessBrush = new SolidColorBrush(Color.FromArgb(255, 16, 124, 16));
	private string _captchaInput = string.Empty;
	private string _statusText = "请在内嵌页面完成验证，或手动粘贴返回的 token。";
	private Brush _statusBrush = SecondaryBrush;

	public SignCaptchaDialogViewModel()
	{
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

	public string? Token => CaptchaVerificationBridge.DecodeToken(CaptchaInput);

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

	public void MarkCaptchaLoading()
	{
		StatusText = "正在加载内嵌验证页面...";
		StatusBrush = SecondaryBrush;
	}

	public void MarkCaptchaReady()
	{
		if (string.IsNullOrWhiteSpace(CaptchaInput))
		{
			StatusText = "请在内嵌页面完成验证，然后手动粘贴返回的 token。";
			StatusBrush = SecondaryBrush;
		}
	}

	public void MarkCaptchaError(string message)
	{
		StatusText = message;
		StatusBrush = ErrorBrush;
	}

	private void UpdateStatus()
	{
		if (string.IsNullOrWhiteSpace(CaptchaInput))
		{
			StatusText = "请在内嵌页面完成验证，或手动粘贴返回的 token。";
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
