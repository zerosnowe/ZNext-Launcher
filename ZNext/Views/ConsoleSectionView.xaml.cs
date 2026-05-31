using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace ZNext.Views;

public sealed partial class ConsoleSectionView : UserControl
{
	public ConsoleSectionView()
	{
		InitializeComponent();
	}

	public event TypedEventHandler<NavigationView, NavigationViewSelectionChangedEventArgs>? SessionSelectionChanged;

	public event KeyEventHandler? InputKeyDown;

	public event RoutedEventHandler? RunRequested;

	public event RoutedEventHandler? InterruptRequested;

	private void ConsoleSessionsNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
	{
		SessionSelectionChanged?.Invoke(sender, args);
	}

	private void ConsoleInputTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
	{
		InputKeyDown?.Invoke(sender, e);
	}

	private void ConsoleRunButton_Click(object sender, RoutedEventArgs e)
	{
		RunRequested?.Invoke(sender, e);
	}

	private void ConsoleInterruptButton_Click(object sender, RoutedEventArgs e)
	{
		InterruptRequested?.Invoke(sender, e);
	}
}
