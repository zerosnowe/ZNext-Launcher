using Microsoft.UI.Xaml;
using ZNext.Views;

namespace ZNext.Services;

internal sealed class AnnouncementInteractionCoordinator(
	UserSessionService userSessionService,
	AnnouncementSectionController announcementSectionController,
	Func<AnnouncementSectionView> announcementPanelProvider,
	Action<FrameworkElement, string> showStandalonePanel)
{
	public async Task OpenAndLoadAsync()
	{
		AnnouncementSectionView panel = announcementPanelProvider();
		showStandalonePanel(panel, "Announcement");
		panel.AnnouncementRichTextBlock?.Focus(FocusState.Programmatic);
		await LoadAsync();
	}

	public async Task LoadAsync()
	{
		userSessionService.SynchronizeTokens();
		await announcementSectionController.LoadAsync();
	}

	public async Task RefreshIfViewCreatedAsync()
	{
		userSessionService.SynchronizeTokens();
		await announcementSectionController.RefreshIfViewCreatedAsync();
	}

	public void RefreshView()
	{
		announcementSectionController.RefreshView();
	}
}
