using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext;

public sealed partial class ContentDialogContent : Page
{
	public ContentDialogContent()
	{
		InitializeComponent();
	}

	public ContentDialogContent(string text, bool showCloudOption = false) : this()
	{
		BodyTextBlock.Text = string.IsNullOrWhiteSpace(text) ? "-" : text;
		CloudCheckBox.Visibility = showCloudOption ? Visibility.Visible : Visibility.Collapsed;
	}
}
