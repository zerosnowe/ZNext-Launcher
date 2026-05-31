namespace ZNext.Services;

internal sealed record TunnelUpdateRequestBuildResult(
	bool Success,
	string ErrorMessage,
	TunnelUpdateRequest? Request)
{
	public static TunnelUpdateRequestBuildResult Failed(string errorMessage)
	{
		return new TunnelUpdateRequestBuildResult(false, errorMessage, null);
	}

	public static TunnelUpdateRequestBuildResult Created(TunnelUpdateRequest request)
	{
		return new TunnelUpdateRequestBuildResult(true, string.Empty, request);
	}
}
