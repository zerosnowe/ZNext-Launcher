using System.Diagnostics;
using System.Text;

namespace ZNext.Services;

internal sealed class ConsoleSession
{
	public int SessionId { get; set; }

	public string Title { get; set; } = string.Empty;

	public Process? Process { get; set; }

	public StringBuilder Buffer { get; } = new();

	public object BufferLock { get; } = new();

	public string WorkingDirectory { get; set; } = string.Empty;

	public string CurrentPrompt { get; set; } = "PS >";

	public string? LastIssuedCommand { get; set; }

	public bool IsClosing { get; set; }

	public bool IsTunnelSession { get; set; }

	public int TunnelProxyId { get; set; }

	public string TunnelDisplayName { get; set; } = string.Empty;

	public bool HasTunnelReadyNotified { get; set; }

	public string? TunnelStartCommand { get; set; }

	public bool TunnelStopRequested { get; set; }

	public int TunnelAutoRestartAttempts { get; set; }
}
