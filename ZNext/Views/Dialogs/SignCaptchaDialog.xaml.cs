using Microsoft.UI.Xaml.Controls;
using ZNext.ViewModels.Dialogs;

namespace ZNext.Views.Dialogs;

public sealed partial class SignCaptchaDialog : ContentDialog
{
	private SignCaptchaDialogViewModel ViewModel => (SignCaptchaDialogViewModel)DataContext;

	public SignCaptchaDialog()
	{
		InitializeComponent();
		DataContext = new SignCaptchaDialogViewModel();
		PrimaryButtonClick += SignCaptchaDialog_PrimaryButtonClick;
	}

	public string? Token => ViewModel.Token;

	private void SignCaptchaDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
	{
		args.Cancel = !ViewModel.Validate();
	}
}
