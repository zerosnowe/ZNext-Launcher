using System.Diagnostics;
using System.Text;

namespace ZNext.Services;

internal sealed class ConsoleProcessFactory
{
	public ProcessStartInfo CreateShellStartInfo(string workingDirectory, Encoding encoding)
	{
		return new ProcessStartInfo
		{
			FileName = "powershell.exe",
			Arguments = "-NoLogo -NoProfile -NoExit -ExecutionPolicy Bypass -Command \"[Console]::InputEncoding=[System.Text.UTF8Encoding]::new($false);[Console]::OutputEncoding=[System.Text.UTF8Encoding]::new($false);$OutputEncoding=[Console]::OutputEncoding\"",
			WorkingDirectory = workingDirectory,
			UseShellExecute = false,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			StandardInputEncoding = encoding,
			StandardOutputEncoding = encoding,
			StandardErrorEncoding = encoding,
			CreateNoWindow = true
		};
	}

	public ProcessStartInfo CreateTunnelStartInfo(string command, string workingDirectory, Encoding encoding)
	{
		string trimmedCommand = (command ?? string.Empty).Trim();
		string normalizedCommand = trimmedCommand.Replace("./", ".\\");
		string escapedCommand = normalizedCommand.Replace("\"", "`\"");
		string script =
			"[Console]::InputEncoding=[System.Text.UTF8Encoding]::new($false);" +
			"[Console]::OutputEncoding=[System.Text.UTF8Encoding]::new($false);" +
			"$OutputEncoding=[Console]::OutputEncoding;" +
			"$env:NO_COLOR='1';" +
			"$env:TERM='dumb';" +
			"$ProgressPreference='SilentlyContinue';" +
			"if ($PSStyle) { $PSStyle.OutputRendering='PlainText' };" +
			"& { " + escapedCommand + " } 2>&1 | ForEach-Object { $_.ToString(); [Console]::Out.Flush() }";

		ProcessStartInfo startInfo = new()
		{
			FileName = "powershell.exe",
			Arguments = "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"" + script + "\"",
			WorkingDirectory = workingDirectory,
			UseShellExecute = false,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			StandardInputEncoding = encoding,
			StandardOutputEncoding = encoding,
			StandardErrorEncoding = encoding,
			CreateNoWindow = true
		};
		startInfo.Environment["ZNEXT_TUNNEL_ISOLATED"] = "1";
		return startInfo;
	}
}
