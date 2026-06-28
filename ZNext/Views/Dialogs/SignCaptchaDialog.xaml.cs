using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using ZNext.Services;
using ZNext.ViewModels.Dialogs;

namespace ZNext.Views.Dialogs;

public sealed partial class SignCaptchaDialog : ContentDialog
{
	private bool _isCaptchaWebViewInitialized;
	private SignCaptchaDialogViewModel ViewModel => (SignCaptchaDialogViewModel)DataContext;

	public SignCaptchaDialog()
	{
		InitializeComponent();
		DataContext = new SignCaptchaDialogViewModel();
		Closed += SignCaptchaDialog_Closed;
		PrimaryButtonClick += SignCaptchaDialog_PrimaryButtonClick;
	}

	public string? Token => ViewModel.Token;

	private void SignCaptchaDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
	{
		args.Cancel = !ViewModel.Validate();
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
			CaptchaWebView.Source = CaptchaVerificationBridge.CreateCaptchaUri(CaptchaVerificationBridge.SignInClient);
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

	private void SignCaptchaDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
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
