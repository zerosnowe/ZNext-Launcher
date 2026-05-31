using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext.Views;

public sealed partial class NodeSectionView : UserControl
{
	public NodeSectionView()
	{
		InitializeComponent();
	}

	public event RoutedEventHandler? FilterChanged;

	public event TextChangedEventHandler? SearchTextChanged;

	public event RoutedEventHandler? RetryRequested;

	private void NodeFilterCheckBox_Changed(object sender, RoutedEventArgs e)
	{
		FilterChanged?.Invoke(sender, e);
	}

	private void NodeSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		SearchTextChanged?.Invoke(sender, e);
	}

	private void RetryNodesButton_Click(object sender, RoutedEventArgs e)
	{
		RetryRequested?.Invoke(sender, e);
	}
}
