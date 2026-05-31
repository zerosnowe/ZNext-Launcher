namespace ZNext.Services;

internal sealed class TunnelLinkCopyResult
{
	public bool Success { get; init; }

	public string Message { get; init; } = string.Empty;

	public static TunnelLinkCopyResult FromSuccess()
	{
		return new TunnelLinkCopyResult
		{
			Success = true,
			Message = "隧道链接已复制到剪贴板"
		};
	}

	public static TunnelLinkCopyResult FromFailure(string? message)
	{
		return new TunnelLinkCopyResult
		{
			Success = false,
			Message = string.IsNullOrWhiteSpace(message) ? "获取链接失败" : message
		};
	}
}
