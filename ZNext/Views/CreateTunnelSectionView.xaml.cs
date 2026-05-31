using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext.Views;

public sealed partial class CreateTunnelSectionView : UserControl
{
	public CreateTunnelSectionView()
	{
		InitializeComponent();
	}

	public event SizeChangedEventHandler? SectionSizeChanged;
	public event SelectionChangedEventHandler? CountrySelectionChanged;
	public event RoutedEventHandler? FilterToggled;
	public event RoutedEventHandler? RefreshRequested;
	public event TextChangedEventHandler? SearchTextChanged;
	public event RoutedEventHandler? RetryRequested;
	public event RoutedEventHandler? CardClicked;

	private void CreateTunnelSectionView_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		SectionSizeChanged?.Invoke(sender, e);
	}

	private void CreateTunnelCountryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		CountrySelectionChanged?.Invoke(sender, e);
	}

	private void CreateTunnelFilterToggleMenuItem_Click(object sender, RoutedEventArgs e)
	{
		FilterToggled?.Invoke(sender, e);
	}

	private void RefreshCreateTunnelButton_Click(object sender, RoutedEventArgs e)
	{
		RefreshRequested?.Invoke(sender, e);
	}

	private void CreateTunnelSearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		SearchTextChanged?.Invoke(sender, e);
	}

	private void RetryCreateTunnelButton_Click(object sender, RoutedEventArgs e)
	{
		RetryRequested?.Invoke(sender, e);
	}

	private void CreateTunnelCard_Click(object sender, RoutedEventArgs e)
	{
		CardClicked?.Invoke(sender, e);
	}
}
