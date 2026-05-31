namespace ZNext.Services;

internal sealed record CreateTunnelRequestBuildResult(
	bool Success,
	string ErrorMessage,
	CreateProxyRequest? Request)
{
	public static CreateTunnelRequestBuildResult Failed(string errorMessage)
	{
		return new CreateTunnelRequestBuildResult(false, errorMessage, null);
	}

	public static CreateTunnelRequestBuildResult Created(CreateProxyRequest request)
	{
		return new CreateTunnelRequestBuildResult(true, string.Empty, request);
	}
}
