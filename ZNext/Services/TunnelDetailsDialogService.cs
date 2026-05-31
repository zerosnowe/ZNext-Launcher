using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ZNext.Services.Dialogs;

namespace ZNext.Services;

internal sealed class TunnelDetailsDialogService
{
	private readonly Func<XamlRoot?> _xamlRootProvider;
	private readonly UserSessionService _userSessionService;
	private readonly TunnelLinkCopyService _tunnelLinkCopyService;
	private readonly ClipboardService _clipboardService;
	private readonly UserDialogService _userDialogService;
	private readonly Action<string> _showSuccessToast;
	private int _isDialogShowing;

	public TunnelDetailsDialogService(
		Func<XamlRoot?> xamlRootProvider,
		UserSessionService userSessionService,
		TunnelLinkCopyService tunnelLinkCopyService,
		ClipboardService clipboardService,
		UserDialogService userDialogService,
		Action<string> showSuccessToast)
	{
		_xamlRootProvider = xamlRootProvider;
		_userSessionService = userSessionService;
		_tunnelLinkCopyService = tunnelLinkCopyService;
		_clipboardService = clipboardService;
		_userDialogService = userDialogService;
		_showSuccessToast = showSuccessToast;
	}

	public async Task ShowAsync(TunnelInfo tunnel)
	{
		if (Interlocked.Exchange(ref _isDialogShowing, 1) == 1)
		{
			return;
		}

		try
		{
			XamlRoot? xamlRoot = _xamlRootProvider();
			if (xamlRoot == null)
			{
				return;
			}

			string? resolvedLink = await ResolveTunnelLinkAsync(tunnel);
			string content = TunnelDetailsFormatter.Format(tunnel, resolvedLink);
			ContentDialog dialog = ModernDialogFactory.Create(
				xamlRoot,
				"隧道详情",
				ModernDialogFactory.Scrollable(new TextBlock
				{
					Text = content,
					TextWrapping = TextWrapping.Wrap,
					IsTextSelectionEnabled = true
				}),
				primaryButtonText: "复制",
				closeButtonText: "关闭",
				defaultButton: ContentDialogButton.Close);

			if (await DialogHost.ShowAsync(dialog) == ContentDialogResult.Primary)
			{
				_clipboardService.SetText(content);
				_showSuccessToast("隧道详情已复制到剪贴板");
			}
		}
		catch (COMException ex) when (ex.Message.Contains("Only a single ContentDialog can be open", StringComparison.OrdinalIgnoreCase))
		{
			Debug.WriteLine("TunnelDetailsDialogService skipped duplicated dialog open.");
		}
		catch (Exception ex)
		{
			await _userDialogService.ShowInfoAsync("隧道详情异常", ex.Message);
		}
		finally
		{
			Interlocked.Exchange(ref _isDialogShowing, 0);
		}
	}

	private async Task<string?> ResolveTunnelLinkAsync(TunnelInfo tunnel)
	{
		if (!_userSessionService.IsSignedIn)
		{
			return null;
		}

		TunnelLinkResult tunnelLinkResult = await _tunnelLinkCopyService.GetLinkWithRetryAsync(tunnel);
		return tunnelLinkResult.Success && !string.IsNullOrWhiteSpace(tunnelLinkResult.Link)
			? tunnelLinkResult.Link
			: null;
	}
}
