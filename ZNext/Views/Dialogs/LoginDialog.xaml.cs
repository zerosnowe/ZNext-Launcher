using Microsoft.UI.Xaml.Controls;
using ZNext.Services;
using ZNext.ViewModels.Dialogs;

namespace ZNext.Views.Dialogs;

public sealed partial class LoginDialog : ContentDialog
{
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
}
