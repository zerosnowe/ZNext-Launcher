using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using ZNext.ViewModels;
using ZNext.Views;

namespace ZNext.Services;

internal sealed class SectionInteractionCoordinator
{
	private readonly NodeSectionViewModel _nodesViewModel;
	private readonly TunnelsSectionViewModel _tunnelsViewModel;
	private readonly CreateTunnelSectionViewModel _createTunnelViewModel;
	private readonly Func<NodeSectionView?> _nodeViewAccessor;
	private readonly Func<TunnelsSectionView?> _tunnelsViewAccessor;
	private readonly Func<CreateTunnelSectionView?> _createTunnelViewAccessor;
	private readonly Func<ScrollViewer?> _mainScrollViewerAccessor;
	private readonly UniformGridLayout _tunnelGridLayout = new UniformGridLayout
	{
		MinItemWidth = 300.0,
		MinItemHeight = 224.0,
		MinColumnSpacing = 16.0,
		MinRowSpacing = 16.0
	};
	private readonly StackLayout _tunnelListLayout = new StackLayout
	{
		Spacing = 12.0
	};

	public SectionInteractionCoordinator(
		NodeSectionViewModel nodesViewModel,
		TunnelsSectionViewModel tunnelsViewModel,
		CreateTunnelSectionViewModel createTunnelViewModel,
		Func<NodeSectionView?> nodeViewAccessor,
		Func<TunnelsSectionView?> tunnelsViewAccessor,
		Func<CreateTunnelSectionView?> createTunnelViewAccessor,
		Func<ScrollViewer?> mainScrollViewerAccessor)
	{
		_nodesViewModel = nodesViewModel;
		_tunnelsViewModel = tunnelsViewModel;
		_createTunnelViewModel = createTunnelViewModel;
		_nodeViewAccessor = nodeViewAccessor;
		_tunnelsViewAccessor = tunnelsViewAccessor;
		_createTunnelViewAccessor = createTunnelViewAccessor;
		_mainScrollViewerAccessor = mainScrollViewerAccessor;
	}

	public bool HasVisibleTunnelData => _tunnelsViewModel.HasVisibleData;

	public void RefreshTunnelsSectionUi()
	{
		if (TunnelsView == null)
		{
			return;
		}

		if (TunnelGridModeButton != null)
		{
			TunnelGridModeButton.IsChecked = !_tunnelsViewModel.IsListView;
		}

		if (TunnelListModeButton != null)
		{
			TunnelListModeButton.IsChecked = _tunnelsViewModel.IsListView;
		}

		if (_tunnelsViewModel.HasCachedTunnels)
		{
			ApplyTunnelFiltersAndLayout();
		}
	}

	public void HandleTunnelSearchChanged()
	{
		try
		{
			_tunnelsViewModel.SearchKeyword = TunnelSearchBox?.Text ?? string.Empty;
			ApplyTunnelFiltersAndLayout();
		}
		catch (Exception ex)
		{
			Debug.WriteLine("TunnelSearchBox_TextChanged failed: " + ex.Message);
		}
	}

	public void UseTunnelGridMode()
	{
		try
		{
			_tunnelsViewModel.IsListView = false;
			if (TunnelGridModeButton != null)
			{
				TunnelGridModeButton.IsChecked = true;
			}

			if (TunnelListModeButton != null)
			{
				TunnelListModeButton.IsChecked = false;
			}

			ApplyTunnelFiltersAndLayout();
			ResetTunnelListViewport();
		}
		catch (Exception ex)
		{
			Debug.WriteLine("TunnelGridModeButton_Click failed: " + ex.Message);
		}
	}

	public void UseTunnelListMode()
	{
		try
		{
			_tunnelsViewModel.IsListView = true;
			if (TunnelGridModeButton != null)
			{
				TunnelGridModeButton.IsChecked = false;
			}

			if (TunnelListModeButton != null)
			{
				TunnelListModeButton.IsChecked = true;
			}

			ApplyTunnelFiltersAndLayout();
			ResetTunnelListViewport();
		}
		catch (Exception ex)
		{
			Debug.WriteLine("TunnelListModeButton_Click failed: " + ex.Message);
		}
	}

	public void ApplyTunnelFiltersAndLayout()
	{
		if (TunnelsView == null)
		{
			return;
		}

		ApplyTunnelRepeaterLayout();
		IReadOnlyList<TunnelInfo> filtered = _tunnelsViewModel.ApplyFilters();
		if (filtered.Count > 0)
		{
			UpdateTunnelsRepeaterWidth();
		}
	}

	public void UpdateTunnelsRepeaterWidth()
	{
		if (TunnelsItemsRepeater == null || TunnelsView == null)
		{
			return;
		}

		if (_tunnelsViewModel.IsListView)
		{
			TunnelsItemsRepeater.Width = double.NaN;
			TunnelsItemsRepeater.HorizontalAlignment = HorizontalAlignment.Stretch;
			return;
		}

		double available = Math.Max(0.0, TunnelsView.ActualWidth - 8.0);
		double targetWidth = CalculateRepeaterRowWidth(available, 300.0, 16.0);
		if (!double.IsNaN(targetWidth))
		{
			TunnelsItemsRepeater.Width = targetWidth;
			TunnelsItemsRepeater.HorizontalAlignment = HorizontalAlignment.Center;
		}
	}

	public void ApplyNodeFilters()
	{
		if (NodeView == null)
		{
			return;
		}

		_nodesViewModel.SearchKeyword = NodeSearchTextBox?.Text ?? string.Empty;
		_nodesViewModel.HideOffline = HideOfflineNodesCheckBox?.IsChecked == true;
		_nodesViewModel.HideOverloaded = HideOverloadedNodesCheckBox?.IsChecked == true;
		_nodesViewModel.ApplyFilters();
	}

	public void RefreshCreateTunnelSectionUi()
	{
		if (CreateTunnelView != null && _createTunnelViewModel.HasNodes)
		{
			ApplyCreateTunnelNodeFilters();
		}
	}

	public void HandleCreateTunnelSearchChanged()
	{
		_createTunnelViewModel.SearchKeyword = CreateTunnelSearchBox?.Text ?? string.Empty;
		UpdateCreateTunnelRepeaterWidth();
	}

	public void HandleCreateTunnelCountryChanged()
	{
		_createTunnelViewModel.Country = CreateTunnelCountryComboBox?.SelectedItem?.ToString() ?? string.Empty;
		UpdateCreateTunnelRepeaterWidth();
	}

	public void HandleCreateTunnelFilterToggled(object sender)
	{
		if (sender is not ToggleMenuFlyoutItem { IsChecked: var isChecked, Tag: var tag })
		{
			return;
		}

		switch (tag?.ToString())
		{
			case "CanWeb":
				_createTunnelViewModel.CanWeb = isChecked;
				break;
			case "HighBandwidth":
				_createTunnelViewModel.HighBandwidth = isChecked;
				break;
			case "NotOverloaded":
				_createTunnelViewModel.NotOverloaded = isChecked;
				break;
			case "IncludeNoPermission":
				_createTunnelViewModel.IncludeNoPermission = isChecked;
				break;
		}

		UpdateCreateTunnelRepeaterWidth();
	}

	public void ApplyCreateTunnelNodeFilters()
	{
		if (CreateTunnelView == null)
		{
			return;
		}

		IReadOnlyList<CreateTunnelNodeCard> list = _createTunnelViewModel.ApplyFilters();
		if (list.Count > 0)
		{
			UpdateCreateTunnelRepeaterWidth();
		}
	}

	public void UpdateCreateTunnelRepeaterWidth()
	{
		if (CreateTunnelNodesGridView == null || CreateTunnelView == null)
		{
			return;
		}

		double available = Math.Max(0.0, CreateTunnelView.ActualWidth - 40.0);
		double targetWidth = CalculateRepeaterRowWidth(available, 260.0, 12.0);
		if (!double.IsNaN(targetWidth))
		{
			CreateTunnelNodesGridView.MaxWidth = Math.Max(272.0, targetWidth + 40.0);
			CreateTunnelNodesGridView.HorizontalAlignment = HorizontalAlignment.Center;
		}
	}

	private NodeSectionView? NodeView => _nodeViewAccessor();

	private TunnelsSectionView? TunnelsView => _tunnelsViewAccessor();

	private CreateTunnelSectionView? CreateTunnelView => _createTunnelViewAccessor();

	private CheckBox? HideOfflineNodesCheckBox => NodeView?.HideOfflineNodesCheckBox;

	private CheckBox? HideOverloadedNodesCheckBox => NodeView?.HideOverloadedNodesCheckBox;

	private TextBox? NodeSearchTextBox => NodeView?.NodeSearchTextBox;

	private TextBox? TunnelSearchBox => TunnelsView?.TunnelSearchBox;

	private ToggleButton? TunnelGridModeButton => TunnelsView?.TunnelGridModeButton;

	private ToggleButton? TunnelListModeButton => TunnelsView?.TunnelListModeButton;

	private ItemsRepeater? TunnelsItemsRepeater => TunnelsView?.TunnelsItemsRepeater;

	private ComboBox? CreateTunnelCountryComboBox => CreateTunnelView?.CreateTunnelCountryComboBox;

	private TextBox? CreateTunnelSearchBox => CreateTunnelView?.CreateTunnelSearchBox;

	private GridView? CreateTunnelNodesGridView => CreateTunnelView?.CreateTunnelNodesGridView;

	private void ApplyTunnelRepeaterLayout()
	{
		if (TunnelsItemsRepeater == null)
		{
			return;
		}

		Layout targetLayout = _tunnelsViewModel.IsListView
			? _tunnelListLayout
			: _tunnelGridLayout;
		if (!ReferenceEquals(TunnelsItemsRepeater.Layout, targetLayout))
		{
			TunnelsItemsRepeater.Layout = targetLayout;
		}
	}

	private void ResetTunnelListViewport()
	{
		try
		{
			_mainScrollViewerAccessor()?.ChangeView(null, 0.0, null, disableAnimation: true);
		}
		catch (Exception ex)
		{
			Debug.WriteLine("ResetTunnelListViewport failed: " + ex.Message);
		}
	}

	private static double CalculateRepeaterRowWidth(double availableWidth, double itemWidth, double spacing)
	{
		if (availableWidth <= 0.0 || itemWidth <= 0.0)
		{
			return double.NaN;
		}

		int columns = Math.Max(1, (int)Math.Floor((availableWidth + spacing) / (itemWidth + spacing)));
		return columns * itemWidth + Math.Max(0, columns - 1) * spacing;
	}
}
