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
	private bool _isNavigatingBack;
	private readonly Stack<string> _backStack = new Stack<string>();

	public string? CurrentKey { get; private set; }

	public bool IsSynchronizingSelection => _isSynchronizingSelection;
	public bool CanGoBack => _backStack.Count > 0;

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

		if (string.Equals(CurrentKey, key, StringComparison.OrdinalIgnoreCase)
			&& ReferenceEquals(_sectionHost.Children.FirstOrDefault(), descriptor.Section))
		{
			return true;
		}

		if (!_isNavigatingBack && IsRegisteredKey(CurrentKey))
		{
			_backStack.Push(CurrentKey!);
		}

		ShowRegisteredSection(descriptor);
		CurrentKey = key;
		_options.UpdateBackButton?.Invoke(CurrentKey);
		return true;
	}

	public void ShowStandalone(FrameworkElement panel, string backButtonKey)
	{
		if (IsRegisteredKey(CurrentKey))
		{
			_backStack.Push(CurrentKey!);
		}

		_options.PrepareSectionHost?.Invoke(backButtonKey);
		SwitchContent(panel);
		_options.UpdateActivePageRoot?.Invoke(panel);
		CurrentKey = backButtonKey;
		_options.UpdateBackButton?.Invoke(CurrentKey);
	}

	public bool GoBack()
	{
		if (_backStack.Count == 0)
		{
			return false;
		}

		string targetKey = _backStack.Pop();
		_isNavigatingBack = true;
		try
		{
			return NavigateTo(targetKey);
		}
		finally
		{
			_isNavigatingBack = false;
			if (CurrentKey != null)
			{
				_options.UpdateBackButton?.Invoke(CurrentKey);
			}
		}
	}

	public FrameworkElement? GetSection(string key)
	{
		return _sectionProvider.GetSection(key);
	}

	public NavigationViewItem? FindNavigationItem(string key)
	{
		if (string.Equals(key, "Settings", StringComparison.OrdinalIgnoreCase)
			&& _navigationView.SettingsItem is NavigationViewItem settingsItem)
		{
			return settingsItem;
		}

		return FindNavigationItem(_navigationView.MenuItems, key)
			?? FindNavigationItem(_navigationView.FooterMenuItems, key);
	}

	private void ShowRegisteredSection(SectionDescriptor descriptor)
	{
		_options.PrepareSectionHost?.Invoke(descriptor.Key);

		FrameworkElement section = descriptor.Section;
		SwitchContent(section);
		_options.UpdateActivePageRoot?.Invoke(section);
		descriptor.OnNavigatedTo?.Invoke();
	}

	private bool IsRegisteredKey(string? key)
	{
		return !string.IsNullOrWhiteSpace(key)
			&& _sectionProvider.RegisteredSections.ContainsKey(key);
	}

	private void SwitchContent(FrameworkElement section)
	{
		if (ReferenceEquals(section.Parent, _sectionHost))
		{
			_sectionHost.Children.Remove(section);
		}
		else
		{
			DetachFromParent(section);
		}

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
