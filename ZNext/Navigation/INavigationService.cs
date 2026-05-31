using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext.Navigation;

internal interface INavigationService
{
	string? CurrentKey { get; }

	IEnumerable<FrameworkElement> Sections { get; }

	bool NavigateTo(string key, bool synchronizeSelection = true);

	void ShowStandalone(FrameworkElement panel, string backButtonKey);

	FrameworkElement? GetSection(string key);

	NavigationViewItem? FindNavigationItem(string key);
}
