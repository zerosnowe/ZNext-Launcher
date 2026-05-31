using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext.Views;

public sealed partial class AnnouncementSectionView : UserControl
{
	public AnnouncementSectionView()
	{
		InitializeComponent();
	}

	public event RoutedEventHandler? RefreshRequested;

	private void RefreshAnnouncementButton_Click(object sender, RoutedEventArgs e)
	{
		RefreshRequested?.Invoke(sender, e);
	}
}
