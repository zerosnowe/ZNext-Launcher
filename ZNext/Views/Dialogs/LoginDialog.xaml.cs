using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using ZNext.Services;
using ZNext.ViewModels.Dialogs;

namespace ZNext.Views.Dialogs;

public sealed partial class LoginDialog : ContentDialog
{
	private bool _isCaptchaWebViewInitialized;
	private LoginDialogViewModel ViewModel => (LoginDialogViewModel)DataContext;

	public LoginDialog()
		: this(new LoginDialogViewModel(new AuthService()))
	{
	}

	internal LoginDialog(LoginDialogViewModel viewModel)
	{
		InitializeComponent();
		DataContext = viewModel;
		Loaded += LoginDialog_Loaded;
		Closed += LoginDialog_Closed;
		PrimaryButtonClick += LoginDialog_PrimaryButtonClick;
		IsPrimaryButtonEnabled = true;
	}

	public string? Token => ViewModel.Token;

	private async void LoginDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
	{
		ContentDialogButtonClickDeferral deferral = args.GetDeferral();
		try
		{
			ViewModel.ApplyInputSnapshot(
				UsernameTextBox.Text,
				PasswordBox.Password,
				CaptchaInputTextBox.Text);
			args.Cancel = !await ViewModel.LoginAsync();
		}
		finally
		{
			deferral.Complete();
		}
	}

	private void LoginDialog_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		IsPrimaryButtonEnabled = true;
	}

	private async void StartCaptchaButton_Click(object sender, RoutedEventArgs e)
	{
		await OpenCaptchaAsync();
	}

	private async Task OpenCaptchaAsync()
	{
		CaptchaWebViewHost.Visibility = Visibility.Visible;
		CaptchaLoadingOverlay.Visibility = Visibility.Visible;
		CaptchaLoadingRing.IsActive = true;
		StartCaptchaButton.IsEnabled = false;
		ViewModel.MarkCaptchaLoading();

		try
		{
			await EnsureCaptchaWebViewInitializedAsync();
			CaptchaWebView.Source = CaptchaVerificationBridge.CreateCaptchaUri(CaptchaVerificationBridge.LoginClient);
		}
		catch (Exception ex)
		{
			StopCaptchaLoading();
			ViewModel.MarkCaptchaError("加载内嵌验证失败: " + ex.Message);
		}
		finally
		{
			StartCaptchaButton.IsEnabled = true;
		}
	}

	private async Task EnsureCaptchaWebViewInitializedAsync()
	{
		if (_isCaptchaWebViewInitialized)
		{
			return;
		}

		CaptchaWebView.NavigationCompleted += CaptchaWebView_NavigationCompleted;
		await CaptchaWebView.EnsureCoreWebView2Async();
		_isCaptchaWebViewInitialized = true;
	}

	private void CaptchaWebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
	{
		StopCaptchaLoading();

		if (!args.IsSuccess)
		{
			ViewModel.MarkCaptchaError("验证页面加载失败，请检查网络后重试。");
			return;
		}

		ViewModel.MarkCaptchaReady();
	}

	private void StopCaptchaLoading()
	{
		CaptchaLoadingRing.IsActive = false;
		CaptchaLoadingOverlay.Visibility = Visibility.Collapsed;
	}

	private void LoginDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
	{
		StopCaptchaLoading();
		try
		{
			CaptchaWebView.CoreWebView2?.Stop();
		}
		catch
		{
		}
	}
}
