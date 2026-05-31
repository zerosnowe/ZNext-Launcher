using System.Threading.Tasks;

namespace ZNext.Services;

internal sealed class TunnelLinkCopyService
{
	private readonly TunnelService _tunnelService;
	private readonly UserSessionService _userSessionService;
	private readonly ClipboardService _clipboardService;

	public TunnelLinkCopyService(
		TunnelService tunnelService,
		UserSessionService userSessionService,
		ClipboardService clipboardService)
	{
		_tunnelService = tunnelService;
		_userSessionService = userSessionService;
		_clipboardService = clipboardService;
	}

	public async Task<TunnelLinkResult> GetLinkWithRetryAsync(TunnelInfo tunnel)
	{
		_userSessionService.SynchronizeTokens();
		TunnelLinkResult firstTry = await _tunnelService.GetTunnelLinkAsync(tunnel);
		if (firstTry.Success && !string.IsNullOrWhiteSpace(firstTry.Link))
		{
			return firstTry;
		}

		await Task.Delay(120);
		_userSessionService.SynchronizeTokens();
		TunnelLinkResult secondTry = await _tunnelService.GetTunnelLinkAsync(tunnel);
		if (secondTry.Success && !string.IsNullOrWhiteSpace(secondTry.Link))
		{
			return secondTry;
		}

		if (string.IsNullOrWhiteSpace(secondTry.Message))
		{
			secondTry.Message = string.IsNullOrWhiteSpace(firstTry.Message) ? "获取链接失败" : firstTry.Message;
		}

		return secondTry;
	}

	public async Task<TunnelLinkCopyResult> CopyLinkAsync(TunnelInfo tunnel, bool allowSecondRound)
	{
		TunnelLinkResult linkResult = await GetLinkWithRetryAsync(tunnel);
		if (linkResult.Success && !string.IsNullOrWhiteSpace(linkResult.Link))
		{
			_clipboardService.SetText(linkResult.Link);
			return TunnelLinkCopyResult.FromSuccess();
		}

		if (allowSecondRound)
		{
			return await CopyLinkAsync(tunnel, allowSecondRound: false);
		}

		return TunnelLinkCopyResult.FromFailure(linkResult.Message);
	}
}
