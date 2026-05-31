using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext.Navigation;

internal sealed class ShellNavigationCoordinator
{
	private readonly SectionNavigationService _navigationService;

	public ShellNavigationCoordinator(
		NavigationView navigationView,
		Panel sectionHost,
		Action<string?> updateBackButton,
		Action<string>? prepareSectionHost,
		Func<FrameworkElement> homeSectionProvider,
		Func<FrameworkElement> nodesSectionProvider,
		Func<FrameworkElement> tunnelsSectionProvider,
		Func<FrameworkElement> createTunnelSectionProvider,
		Func<FrameworkElement> helpSectionProvider,
		Func<FrameworkElement> consoleSectionProvider,
		Func<FrameworkElement> settingsSectionProvider,
		Action onNodesNavigated,
		Action onTunnelsNavigated,
		Action onCreateTunnelNavigated,
		Action onConsoleNavigated,
		Action onSettingsNavigated)
	{
		SectionProvider sectionProvider = new SectionProviderBuilder()
			.WithSection("Home", homeSectionProvider)
			.WithSection("Nodes", nodesSectionProvider, onNodesNavigated)
			.WithSection("Tunnels", tunnelsSectionProvider, onTunnelsNavigated)
			.WithSection("CreateTunnel", createTunnelSectionProvider, onCreateTunnelNavigated)
			.WithSection("Help", helpSectionProvider)
			.WithSection("Console", consoleSectionProvider, onConsoleNavigated)
			.WithSection("Settings", settingsSectionProvider, onSettingsNavigated)
			.Build();

		_navigationService = new SectionNavigationService(navigationView, sectionHost, sectionProvider, new SectionNavigationOptions
		{
			UpdateBackButton = updateBackButton,
			UpdateActivePageRoot = root => ActivePageRootElement = root,
			PrepareSectionHost = prepareSectionHost
		});
	}

	public string? CurrentKey => _navigationService.CurrentKey;

	public FrameworkElement? ActivePageRootElement { get; private set; }

	public void HandleSelectionChanged(NavigationViewSelectionChangedEventArgs args)
	{
		if (_navigationService.IsSynchronizingSelection)
		{
			return;
		}

		if (args.SelectedItemContainer?.Tag is string tag && !string.IsNullOrWhiteSpace(tag))
		{
			NavigateTo(tag, fromSelectionChanged: true);
		}
	}

	public bool NavigateTo(string tag, bool fromSelectionChanged = false)
	{
		return _navigationService.NavigateTo(tag, !fromSelectionChanged);
	}

	public bool Select(string tag)
	{
		return _navigationService.NavigateTo(tag);
	}

	public void ShowStandalone(FrameworkElement panel, string backButtonKey)
	{
		_navigationService.ShowStandalone(panel, backButtonKey);
	}
}
