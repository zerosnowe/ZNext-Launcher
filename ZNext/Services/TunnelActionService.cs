using System.Threading.Tasks;

namespace ZNext.Services;

internal sealed class TunnelActionService
{
	private readonly TunnelService _tunnelService;
	private readonly UserSessionService _userSessionService;

	public TunnelActionService(TunnelService tunnelService, UserSessionService userSessionService)
	{
		_tunnelService = tunnelService;
		_userSessionService = userSessionService;
	}

	public Task<ServiceActionResult> EnableAsync(TunnelInfo tunnel)
	{
		_userSessionService.SynchronizeTokens();
		return _tunnelService.ToggleProxyDisabledAsync(tunnel.proxyId, isDisabled: false);
	}

	public Task<ServiceActionResult> ForceOfflineAsync(TunnelInfo tunnel)
	{
		_userSessionService.SynchronizeTokens();
		return _tunnelService.KickProxyAsync(tunnel.proxyId);
	}

	public Task<ServiceActionResult> UpdateAsync(TunnelUpdateRequest request)
	{
		_userSessionService.SynchronizeTokens();
		return _tunnelService.UpdateProxyAsync(request);
	}

	public Task<ServiceActionResult> DeleteAsync(TunnelInfo tunnel)
	{
		_userSessionService.SynchronizeTokens();
		return _tunnelService.DeleteProxyAsync(tunnel.proxyId);
	}
}
