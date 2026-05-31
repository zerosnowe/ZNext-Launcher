using Microsoft.UI.Xaml;
using ZNext.ViewModels;

namespace ZNext.Services;

internal sealed class CreateTunnelInteractionCoordinator
{
	private readonly UserSessionService _userSessionService;
	private readonly UserDialogService _userDialogService;
	private readonly Func<CreateTunnelDialogService> _dialogServiceProvider;
	private readonly PageLoadStateCoordinator _pageLoadStateCoordinator;
	private readonly Func<XamlRoot?> _xamlRootProvider;
	private readonly Action<string> _showToast;
	private readonly Func<Task> _reloadTunnelsAsync;
	private readonly Func<Task> _reloadCreateTunnelNodesAsync;

	public CreateTunnelInteractionCoordinator(
		UserSessionService userSessionService,
		UserDialogService userDialogService,
		Func<CreateTunnelDialogService> dialogServiceProvider,
		PageLoadStateCoordinator pageLoadStateCoordinator,
		Func<XamlRoot?> xamlRootProvider,
		Action<string> showToast,
		Func<Task> reloadTunnelsAsync,
		Func<Task> reloadCreateTunnelNodesAsync)
	{
		_userSessionService = userSessionService;
		_userDialogService = userDialogService;
		_dialogServiceProvider = dialogServiceProvider;
		_pageLoadStateCoordinator = pageLoadStateCoordinator;
		_xamlRootProvider = xamlRootProvider;
		_showToast = showToast;
		_reloadTunnelsAsync = reloadTunnelsAsync;
		_reloadCreateTunnelNodesAsync = reloadCreateTunnelNodesAsync;
	}

	public async Task ShowAsync(
		CreateTunnelNodeCard node,
		int? defaultLocalPort = null,
		string? defaultLocalAddress = null,
		string? defaultProxyName = null,
		XamlRoot? dialogXamlRoot = null)
	{
		if (!_userSessionService.IsSignedIn)
		{
			await _userDialogService.ShowInfoAsync("提示", "请先登录后再创建隧道");
			return;
		}

		XamlRoot? xamlRoot = _xamlRootProvider() ?? dialogXamlRoot;
		if (xamlRoot == null)
		{
			_showToast("窗口未就绪，请重试");
			return;
		}

		CreateTunnelDialogResult result = await _dialogServiceProvider().ShowAsync(
			node,
			xamlRoot,
			defaultLocalPort,
			defaultLocalAddress,
			defaultProxyName);

		if (!string.IsNullOrWhiteSpace(result.ErrorTitle) || !string.IsNullOrWhiteSpace(result.ErrorMessage))
		{
			await _userDialogService.ShowInfoAsync(
				result.ErrorTitle ?? "打开失败",
				result.ErrorMessage ?? "创建隧道窗口打开失败。");
			return;
		}

		if (!result.Created)
		{
			return;
		}

		await _userDialogService.ShowInfoAsync("创建成功", result.Message);
		_pageLoadStateCoordinator.MarkTunnelsNotLoaded();
		await _reloadTunnelsAsync();
		_pageLoadStateCoordinator.MarkCreateTunnelNodesNotLoaded();
		await _reloadCreateTunnelNodesAsync();
	}
}
