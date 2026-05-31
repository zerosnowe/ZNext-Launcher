using Microsoft.UI.Xaml;
using ZNext.ViewModels;

namespace ZNext.Services;

internal sealed class ThemeApplicationCoordinator(
	Func<FrameworkElement?> contentProvider,
	AnnouncementSectionController announcementSectionController,
	Action updateTitleBarVisuals,
	Func<bool> hasConsoleSection,
	Action refreshConsoleSection)
{
	public void Apply(string mode)
	{
		FrameworkElement? content = contentProvider();
		if (content == null)
		{
			return;
		}

		content.RequestedTheme = ToElementTheme(SettingsViewModel.NormalizeThemeMode(mode));
		announcementSectionController.RenderCachedMarkdown();
		updateTitleBarVisuals();
		if (hasConsoleSection())
		{
			refreshConsoleSection();
		}
	}

	private static ElementTheme ToElementTheme(string mode)
	{
		return mode switch
		{
			"Light" => ElementTheme.Light,
			"Dark" => ElementTheme.Dark,
			_ => ElementTheme.Default
		};
	}
}
