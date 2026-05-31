using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext.Navigation;

internal sealed class SectionNavigationService : INavigationService
{
	private readonly NavigationView _navigationView;
	private readonly Panel _sectionHost;
	private readonly SectionProvider _sectionProvider;
	private readonly SectionNavigationOptions _options;
	private bool _isSynchronizingSelection;

	public string? CurrentKey { get; private set; }

	public bool IsSynchronizingSelection => _isSynchronizingSelection;

	public IEnumerable<FrameworkElement> Sections => _sectionProvider.RegisteredSections.Values
		.Where(section => section.IsCreated)
		.Select(section => section.Section);

	public SectionNavigationService(
		NavigationView navigationView,
		Panel sectionHost,
		SectionProvider sectionProvider,
		SectionNavigationOptions options)
	{
		_navigationView = navigationView;
		_sectionHost = sectionHost;
		_sectionProvider = sectionProvider;
		_options = options;
	}

	public bool NavigateTo(string key, bool synchronizeSelection = true)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return false;
		}

		if (synchronizeSelection)
		{
			NavigationViewItem? targetItem = FindNavigationItem(key);
			if (targetItem != null && !ReferenceEquals(_navigationView.SelectedItem, targetItem))
			{
				_isSynchronizingSelection = true;
				try
				{
					_navigationView.SelectedItem = targetItem;
				}
				finally
				{
					_isSynchronizingSelection = false;
				}
			}
		}

		if (!_sectionProvider.RegisteredSections.TryGetValue(key, out SectionDescriptor? descriptor))
		{
			return false;
		}

		ShowRegisteredSection(descriptor);
		CurrentKey = key;
		return true;
	}

	public void ShowStandalone(FrameworkElement panel, string backButtonKey)
	{
		_options.PrepareSectionHost?.Invoke(backButtonKey);
		SwitchContent(panel);
		_options.UpdateActivePageRoot?.Invoke(panel);
		_options.UpdateBackButton?.Invoke(backButtonKey);
		CurrentKey = backButtonKey;
	}

	public FrameworkElement? GetSection(string key)
	{
		return _sectionProvider.GetSection(key);
	}

	public NavigationViewItem? FindNavigationItem(string key)
	{
		return FindNavigationItem(_navigationView.MenuItems, key)
			?? FindNavigationItem(_navigationView.FooterMenuItems, key);
	}

	private void ShowRegisteredSection(SectionDescriptor descriptor)
	{
		_options.PrepareSectionHost?.Invoke(descriptor.Key);
		_options.UpdateBackButton?.Invoke(descriptor.Key);

		FrameworkElement section = descriptor.Section;
		SwitchContent(section);
		_options.UpdateActivePageRoot?.Invoke(section);
		descriptor.OnNavigatedTo?.Invoke();
	}

	private void SwitchContent(FrameworkElement section)
	{
		DetachFromParent(section);
		section.Visibility = Visibility.Visible;
		_sectionHost.Children.Clear();
		_sectionHost.Children.Add(section);
	}

	private static void DetachFromParent(FrameworkElement section)
	{
		if (section.Parent is Panel panel)
		{
			panel.Children.Remove(section);
			return;
		}

		if (section.Parent is ContentControl contentControl && ReferenceEquals(contentControl.Content, section))
		{
			contentControl.Content = null;
			return;
		}

		if (section.Parent is Border border && ReferenceEquals(border.Child, section))
		{
			border.Child = null;
			return;
		}

		if (section.Parent is Page page && ReferenceEquals(page.Content, section))
		{
			page.Content = null;
		}
	}

	private static NavigationViewItem? FindNavigationItem(IEnumerable<object> items, string key)
	{
		foreach (object item in items)
		{
			if (item is not NavigationViewItem navigationViewItem)
			{
				continue;
			}

			if (string.Equals(navigationViewItem.Tag?.ToString(), key, StringComparison.OrdinalIgnoreCase))
			{
				return navigationViewItem;
			}

			NavigationViewItem? childItem = FindNavigationItem(navigationViewItem.MenuItems, key);
			if (childItem != null)
			{
				return childItem;
			}
		}

		return null;
	}
}
