using System.Diagnostics;
using Microsoft.UI.Xaml;

namespace ZNext.Services;

internal sealed class TunnelMenuActionCoordinator
{
	private readonly UserSessionService _userSessionService;
	private readonly UserDialogService _userDialogService;
	private readonly TunnelActionService _tunnelActionService;
	private readonly TunnelLinkCopyService _tunnelLinkCopyService;
	private readonly Func<TunnelDetailsDialogService> _detailsDialogServiceProvider;
	private readonly Func<EditTunnelDialogService> _editDialogServiceProvider;
	private readonly Func<XamlRoot?> _xamlRootProvider;
	private readonly Func<TunnelInfo, Task<bool>> _stopTunnelAsync;
	private readonly Action<string> _showSuccessToast;
	private readonly Action _hideSuccessToast;

	public TunnelMenuActionCoordinator(
		UserSessionService userSessionService,
		UserDialogService userDialogService,
		TunnelActionService tunnelActionService,
		TunnelLinkCopyService tunnelLinkCopyService,
		Func<TunnelDetailsDialogService> detailsDialogServiceProvider,
		Func<EditTunnelDialogService> editDialogServiceProvider,
		Func<XamlRoot?> xamlRootProvider,
		Func<TunnelInfo, Task<bool>> stopTunnelAsync,
		Action<string> showSuccessToast,
		Action hideSuccessToast)
	{
		_userSessionService = userSessionService;
		_userDialogService = userDialogService;
		_tunnelActionService = tunnelActionService;
		_tunnelLinkCopyService = tunnelLinkCopyService;
		_detailsDialogServiceProvider = detailsDialogServiceProvider;
		_editDialogServiceProvider = editDialogServiceProvider;
		_xamlRootProvider = xamlRootProvider;
		_stopTunnelAsync = stopTunnelAsync;
		_showSuccessToast = showSuccessToast;
		_hideSuccessToast = hideSuccessToast;
	}

	public async Task<TunnelMenuActionResult> CopyLinkAsync(TunnelInfo tunnel)
	{
		if (!await EnsureSignedInAsync("请先登录后再复制隧道链接。"))
		{
			return TunnelMenuActionResult.None;
		}

		try
		{
			_showSuccessToast("正在复制链接...");
			TunnelLinkCopyResult copyResult = await _tunnelLinkCopyService.CopyLinkAsync(tunnel, allowSecondRound: true);
			if (copyResult.Success)
			{
				_showSuccessToast(copyResult.Message);
			}
			else
			{
				_hideSuccessToast();
			}
		}
		catch
		{
			_hideSuccessToast();
		}

		return TunnelMenuActionResult.None;
	}

	public async Task<TunnelMenuActionResult> ShowDetailsAsync(TunnelInfo tunnel)
	{
		await _detailsDialogServiceProvider().ShowAsync(tunnel);
		return TunnelMenuActionResult.None;
	}

	public async Task<TunnelMenuActionResult> EditAsync(TunnelInfo tunnel)
	{
		if (!await EnsureSignedInAsync("请先登录后再编辑隧道。"))
		{
			return TunnelMenuActionResult.None;
		}

		XamlRoot? xamlRoot = _xamlRootProvider();
		if (xamlRoot == null)
		{
			_showSuccessToast("窗口未就绪，请重试");
			return TunnelMenuActionResult.None;
		}

		EditTunnelDialogResult result = await _editDialogServiceProvider().ShowAsync(tunnel, xamlRoot);
		if (!string.IsNullOrWhiteSpace(result.ErrorTitle) || !string.IsNullOrWhiteSpace(result.ErrorMessage))
		{
			await _userDialogService.ShowInfoAsync(result.ErrorTitle ?? "打开失败", result.ErrorMessage ?? "编辑隧道窗口打开失败。");
			return TunnelMenuActionResult.None;
		}

		if (!result.Updated)
		{
			return TunnelMenuActionResult.None;
		}

		_showSuccessToast("隧道已更新");
		if (!string.IsNullOrWhiteSpace(result.Message))
		{
			Debug.WriteLine("Tunnel updated: " + result.Message);
		}

		return TunnelMenuActionResult.ReloadCards;
	}

	public async Task<TunnelMenuActionResult> EnableAsync(TunnelInfo tunnel)
	{
		if (!await EnsureSignedInAsync("请先登录后再启用隧道。"))
		{
			return TunnelMenuActionResult.None;
		}

		ServiceActionResult enableResult = await _tunnelActionService.EnableAsync(tunnel);
		if (!enableResult.Success)
		{
			await _userDialogService.ShowInfoAsync("启用隧道失败", enableResult.Message);
			return TunnelMenuActionResult.None;
		}

		_showSuccessToast($"隧道 #{tunnel.Id} 已启用");
		return TunnelMenuActionResult.ReloadCards;
	}

	public async Task<TunnelMenuActionResult> ForceOfflineAsync(TunnelInfo tunnel)
	{
		if (!await _userDialogService.ShowConfirmAsync("强制下线", $"确认将隧道 #{tunnel.Id}（{tunnel.Name}）强制下线吗？"))
		{
			return TunnelMenuActionResult.None;
		}

		if (!await EnsureSignedInAsync("请先登录后再执行强制下线。"))
		{
			return TunnelMenuActionResult.None;
		}

		ServiceActionResult kickResult = await _tunnelActionService.ForceOfflineAsync(tunnel);
		if (!kickResult.Success)
		{
			string failureText = (kickResult.Message ?? string.Empty) + "\n" + (kickResult.Error ?? string.Empty);
			if (failureText.Contains("隧道不在线", StringComparison.OrdinalIgnoreCase))
			{
				_showSuccessToast($"隧道 #{tunnel.Id} 不在线");
				return TunnelMenuActionResult.ReloadCards;
			}

			string detail = string.IsNullOrWhiteSpace(kickResult.Error)
				? kickResult.Message ?? "强制下线失败"
				: (kickResult.Message ?? "强制下线失败") + "\n\n" + kickResult.Error;
			await _userDialogService.ShowInfoAsync("强制下线失败", detail);
			return TunnelMenuActionResult.None;
		}

		if (await _stopTunnelAsync(tunnel))
		{
			_showSuccessToast($"隧道 #{tunnel.Id} 已下线");
			return TunnelMenuActionResult.ReloadCards;
		}

		await _userDialogService.ShowInfoAsync("已强制下线", "远端隧道已下线，但本地控制台进程未完全退出，请到控制台手动 Ctrl+C。");
		return TunnelMenuActionResult.None;
	}

	public async Task<TunnelMenuActionResult> DeleteAsync(TunnelInfo tunnel)
	{
		if (!await _userDialogService.ShowConfirmAsync("删除", $"确认删除隧道 #{tunnel.Id}（{tunnel.Name}）吗？"))
		{
			return TunnelMenuActionResult.None;
		}

		if (!await EnsureSignedInAsync("请先登录后再删除隧道。"))
		{
			return TunnelMenuActionResult.None;
		}

		ServiceActionResult deleteResult = await _tunnelActionService.DeleteAsync(tunnel);
		if (!deleteResult.Success)
		{
			await _userDialogService.ShowInfoAsync("删除失败", deleteResult.Message);
			return TunnelMenuActionResult.None;
		}

		await _stopTunnelAsync(tunnel);
		_showSuccessToast($"隧道 #{tunnel.Id} 已删除");
		return TunnelMenuActionResult.LoadFresh;
	}

	private async Task<bool> EnsureSignedInAsync(string message)
	{
		if (_userSessionService.IsSignedIn)
		{
			return true;
		}

		await _userDialogService.ShowInfoAsync("提示", message);
		return false;
	}
}

internal enum TunnelMenuRefreshMode
{
	None,
	ReloadCards,
	LoadFresh
}

internal sealed record TunnelMenuActionResult(TunnelMenuRefreshMode RefreshMode)
{
	public static readonly TunnelMenuActionResult None = new(TunnelMenuRefreshMode.None);
	public static readonly TunnelMenuActionResult ReloadCards = new(TunnelMenuRefreshMode.ReloadCards);
	public static readonly TunnelMenuActionResult LoadFresh = new(TunnelMenuRefreshMode.LoadFresh);
}
