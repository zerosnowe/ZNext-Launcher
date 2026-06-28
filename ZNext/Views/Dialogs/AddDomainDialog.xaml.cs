using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext.Views.Dialogs;

public sealed partial class AddDomainDialog : ContentDialog
{
	public AddDomainDialog()
	{
		InitializeComponent();
		Loaded += AddDomainDialog_Loaded;
		PrimaryButtonClick += AddDomainDialog_PrimaryButtonClick;
	}

	public string? DomainText { get; private set; }

	private void AddDomainDialog_Loaded(object sender, RoutedEventArgs e)
	{
		DomainTextBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
	}

	private void AddDomainDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
	{
		string text = DomainTextBox.Text?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(text))
		{
			args.Cancel = true;
			return;
		}

		DomainText = text;
	}
}
