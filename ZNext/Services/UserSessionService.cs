namespace ZNext.Services;

internal sealed class UserSessionService
{
	private readonly AuthService _authService;
	private readonly AnnouncementService _announcementService;
	private readonly UserInfoService _userInfoService;
	private readonly SystemStatusService _systemStatusService;
	private readonly NodeService _nodeService;
	private readonly TunnelService _tunnelService;
	private readonly CreateProxyService _createProxyService;

	public UserSessionService(
		AuthService authService,
		AnnouncementService announcementService,
		UserInfoService userInfoService,
		SystemStatusService systemStatusService,
		NodeService nodeService,
		TunnelService tunnelService,
		CreateProxyService createProxyService)
	{
		_authService = authService;
		_announcementService = announcementService;
		_userInfoService = userInfoService;
		_systemStatusService = systemStatusService;
		_nodeService = nodeService;
		_tunnelService = tunnelService;
		_createProxyService = createProxyService;
		SynchronizeTokens();
	}

	public string? Token => _authService.Token;

	public bool IsSignedIn => !string.IsNullOrWhiteSpace(Token);

	public bool TryGetToken(out string token)
	{
		token = Token ?? string.Empty;
		return !string.IsNullOrWhiteSpace(token);
	}

	public void SetToken(string token, bool persist = true)
	{
		if (string.IsNullOrWhiteSpace(token))
		{
			Clear();
			return;
		}

		_authService.SetToken(token, persist);
		SetServiceTokens(token);
	}

	public void Clear()
	{
		_authService.ClearToken();
		_announcementService.ClearToken();
		_userInfoService.ClearToken();
		_systemStatusService.ClearToken();
		_nodeService.ClearToken();
		_tunnelService.ClearToken();
		_createProxyService.ClearToken();
	}

	public void SynchronizeTokens()
	{
		if (TryGetToken(out string token))
		{
			SetServiceTokens(token);
			return;
		}

		ClearServiceTokens();
	}

	private void SetServiceTokens(string token)
	{
		_announcementService.SetToken(token);
		_userInfoService.SetToken(token);
		_systemStatusService.SetToken(token);
		_nodeService.SetToken(token);
		_tunnelService.SetToken(token);
		_createProxyService.SetToken(token);
	}

	private void ClearServiceTokens()
	{
		_announcementService.ClearToken();
		_userInfoService.ClearToken();
		_systemStatusService.ClearToken();
		_nodeService.ClearToken();
		_tunnelService.ClearToken();
		_createProxyService.ClearToken();
	}
}
