using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext.Views;

public sealed partial class HelpSectionView : UserControl
{
	public HelpSectionView()
	{
		InitializeComponent();
	}

	public event RoutedEventHandler? OpenLinkRequested;

	public event RoutedEventHandler? CopyTextRequested;

	private void OpenLinkButton_Click(object sender, RoutedEventArgs e)
	{
		OpenLinkRequested?.Invoke(sender, e);
	}

	private void CopyTextButton_Click(object sender, RoutedEventArgs e)
	{
		CopyTextRequested?.Invoke(sender, e);
	}
}
