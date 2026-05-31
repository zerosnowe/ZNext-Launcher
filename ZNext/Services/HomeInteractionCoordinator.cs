using Microsoft.UI.Xaml;
using ZNext.ViewModels;

namespace ZNext.Services;

internal sealed class HomeInteractionCoordinator
{
	private readonly Func<LoginDialogFlowService> _loginDialogFlowProvider;
	private readonly UserSessionOperationCoordinator _userSessionOperationCoordinator;
	private readonly HomeSectionCoordinator _homeSectionCoordinator;
	private readonly AnnouncementInteractionCoordinator _announcementInteractionCoordinator;
	private readonly PageLoadStateCoordinator _pageLoadStateCoordinator;
	private readonly HomeActivationCoordinator _homeActivationCoordinator;
	private readonly HomeSectionViewModel _homeViewModel;
	private readonly Func<XamlRoot?> _xamlRootProvider;
	private readonly Action _selectHome;

	public HomeInteractionCoordinator(
		Func<LoginDialogFlowService> loginDialogFlowProvider,
		UserSessionOperationCoordinator userSessionOperationCoordinator,
		HomeSectionCoordinator homeSectionCoordinator,
		AnnouncementInteractionCoordinator announcementInteractionCoordinator,
		PageLoadStateCoordinator pageLoadStateCoordinator,
		HomeActivationCoordinator homeActivationCoordinator,
		HomeSectionViewModel homeViewModel,
		Func<XamlRoot?> xamlRootProvider,
		Action selectHome)
	{
		_loginDialogFlowProvider = loginDialogFlowProvider;
		_userSessionOperationCoordinator = userSessionOperationCoordinator;
		_homeSectionCoordinator = homeSectionCoordinator;
		_announcementInteractionCoordinator = announcementInteractionCoordinator;
		_pageLoadStateCoordinator = pageLoadStateCoordinator;
		_homeActivationCoordinator = homeActivationCoordinator;
		_homeViewModel = homeViewModel;
		_xamlRootProvider = xamlRootProvider;
		_selectHome = selectHome;
	}

	public async Task LoginAsync()
	{
		await LoginAsync(exitOnCancel: false);
	}

	public async Task<bool> LoginAsync(bool exitOnCancel)
	{
		bool isLoggedIn = await _loginDialogFlowProvider().ShowAsync(exitOnCancel);
		if (isLoggedIn)
		{
			await ReloadAfterLoginAsync();
		}

		return isLoggedIn;
	}

	public async Task SignInAsync()
	{
		await _homeSectionCoordinator.ApplyUserSessionOperationResultAsync(
			await _userSessionOperationCoordinator.SignInAsync(
				_xamlRootProvider(),
				isEnabled => _homeViewModel.IsSignInEnabled = isEnabled));
	}

	public async Task LogoutAsync()
	{
		await _homeSectionCoordinator.ApplyUserSessionOperationResultAsync(await _userSessionOperationCoordinator.LogoutAsync());
	}

	public async Task ReloadAfterLoginAsync()
	{
		_pageLoadStateCoordinator.ResetAll();
		_homeActivationCoordinator.MarkHomeLoaded();
		await _homeSectionCoordinator.UpdateUserInfoAndUIAsync();
		await _homeSectionCoordinator.LoadSystemStatusAsync();
		await _homeSectionCoordinator.LoadImportantAnnouncementAsync();
		await _announcementInteractionCoordinator.RefreshIfViewCreatedAsync();
		_selectHome();
	}
}
