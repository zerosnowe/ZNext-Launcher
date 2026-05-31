namespace ZNext.Services;

internal sealed class HelpSectionCoordinator(
	UserActionCoordinator userActionCoordinator,
	UserDialogService userDialogService)
{

	public async Task OpenLinkAsync(string? link)
	{
		await ShowResultAsync(userActionCoordinator.OpenHelpLink(link));
	}

	public async Task CopyTextAsync(string? text)
	{
		await ShowResultAsync(userActionCoordinator.CopyHelpText(text));
	}

	private async Task ShowResultAsync(UserActionResult result)
	{
		if (result.ShouldShowDialog)
		{
			await userDialogService.ShowInfoAsync(result.Title, result.Message);
		}
	}
}
