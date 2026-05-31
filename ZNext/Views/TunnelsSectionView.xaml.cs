using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace ZNext.Views;

public sealed partial class TunnelsSectionView : UserControl
{
	public TunnelsSectionView()
	{
		InitializeComponent();
	}

	public event SizeChangedEventHandler? SectionSizeChanged;
	public event TextChangedEventHandler? SearchTextChanged;
	public event RoutedEventHandler? GridModeRequested;
	public event RoutedEventHandler? ListModeRequested;
	public event RoutedEventHandler? RefreshRequested;
	public event RoutedEventHandler? RetryRequested;
	public event RoutedEventHandler? ViewDetailsRequested;
	public event RoutedEventHandler? EditRequested;
	public event RoutedEventHandler? EnableRequested;
	public event RoutedEventHandler? ForceOfflineRequested;
	public event RoutedEventHandler? DeleteRequested;
	public event RoutedEventHandler? RunToggleLoaded;
	public event RoutedEventHandler? RunToggleToggled;
	public event RoutedEventHandler? CopyLinkRequested;

	private void TunnelsSectionView_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		SectionSizeChanged?.Invoke(sender, e);
	}

	private void TunnelSearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		SearchTextChanged?.Invoke(sender, e);
	}

	private void TunnelGridModeButton_Click(object sender, RoutedEventArgs e)
	{
		GridModeRequested?.Invoke(sender, e);
	}

	private void TunnelListModeButton_Click(object sender, RoutedEventArgs e)
	{
		ListModeRequested?.Invoke(sender, e);
	}

	private void RefreshTunnelsButton_Click(object sender, RoutedEventArgs e)
	{
		RefreshRequested?.Invoke(sender, e);
	}

	private void RetryTunnelsButton_Click(object sender, RoutedEventArgs e)
	{
		RetryRequested?.Invoke(sender, e);
	}

	private void TunnelViewDetailsMenuItem_Click(object sender, RoutedEventArgs e)
	{
		ViewDetailsRequested?.Invoke(sender, e);
	}

	private void TunnelEditMenuItem_Click(object sender, RoutedEventArgs e)
	{
		EditRequested?.Invoke(sender, e);
	}

	private void TunnelEnableMenuItem_Click(object sender, RoutedEventArgs e)
	{
		EnableRequested?.Invoke(sender, e);
	}

	private void TunnelForceOfflineMenuItem_Click(object sender, RoutedEventArgs e)
	{
		ForceOfflineRequested?.Invoke(sender, e);
	}

	private void TunnelDeleteMenuItem_Click(object sender, RoutedEventArgs e)
	{
		DeleteRequested?.Invoke(sender, e);
	}

	private void TunnelRunToggleSwitch_Loaded(object sender, RoutedEventArgs e)
	{
		RunToggleLoaded?.Invoke(sender, e);
	}

	private void TunnelRunToggleSwitch_Toggled(object sender, RoutedEventArgs e)
	{
		RunToggleToggled?.Invoke(sender, e);
	}

	private void TunnelCopyLinkButton_Click(object sender, RoutedEventArgs e)
	{
		CopyLinkRequested?.Invoke(sender, e);
	}
}
