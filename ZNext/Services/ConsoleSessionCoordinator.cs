using System.Text;

namespace ZNext.Services;

internal sealed class ConsoleSessionCoordinator
{
	private const string PromptMarker = "__ZNEXT_PROMPT__:";

	private readonly ConsoleSessionProcessService _processService;
	private int _nextSessionId = 1;

	public ConsoleSessionCoordinator(ConsoleSessionProcessService processService)
	{
		_processService = processService;
	}

	public ConsoleSession CreateSession(string? title, string workingDirectory)
	{
		int sessionId = _nextSessionId++;
		return new ConsoleSession
		{
			SessionId = sessionId,
			Title = string.IsNullOrWhiteSpace(title) ? $"控制台 {sessionId}" : title,
			WorkingDirectory = workingDirectory
		};
	}

	public void StartShell(
		ConsoleSession session,
		Encoding encoding,
		ConsoleProcessHandlers handlers,
		Action<ConsoleSession, string> appendOutput)
	{
		ConsoleProcessStartResult result = _processService.StartShell(session, encoding, handlers);
		if (!result.Success)
		{
			appendOutput(session, "[启动失败] " + result.ErrorMessage);
			return;
		}

		appendOutput(session, $"[PowerShell 已启动] {DateTime.Now:HH:mm:ss}");
		InitializeSession(session);
	}

	public void ExecuteCommand(
		ConsoleSession session,
		string command,
		Encoding encoding,
		ConsoleProcessHandlers handlers,
		Action<ConsoleSession, string> appendOutput)
	{
		if (string.IsNullOrWhiteSpace(command))
		{
			return;
		}

		if (!_processService.IsRunning(session))
		{
			appendOutput(session, "[提示] 控制台未启动，正在重启...");
			StartShell(session, encoding, handlers, appendOutput);
		}

		appendOutput(session, session.CurrentPrompt + " " + command);
		session.LastIssuedCommand = command;
		try
		{
			if (!_processService.TryWriteInput(session, command))
			{
				appendOutput(session, "[执行失败] 控制台不可用");
			}
			else
			{
				RequestPrompt(session);
			}
		}
		catch (Exception ex)
		{
			appendOutput(session, "[执行失败] " + ex.Message);
		}
	}

	public async Task InterruptAsync(ConsoleSession session, Action<ConsoleSession, string> appendOutput)
	{
		if (!_processService.IsRunning(session))
		{
			return;
		}

		try
		{
			if (!_processService.TrySendCtrlC(session))
			{
				appendOutput(session, "[Ctrl+C 失败] 控制台不可用");
				return;
			}
		}
		catch (Exception ex)
		{
			appendOutput(session, "[Ctrl+C 失败] " + ex.Message);
			return;
		}

		await Task.Delay(250);
		try
		{
			if (_processService.TryTerminateChildProcesses(session))
			{
				appendOutput(session, "[Ctrl+C 回退] 已终止当前前台进程");
				RequestPrompt(session);
			}
		}
		catch (Exception ex)
		{
			appendOutput(session, "[Ctrl+C 回退失败] " + ex.Message);
		}
	}

	public void RequestPrompt(ConsoleSession session)
	{
		_processService.TryWriteInput(session, $"Write-Output \"{PromptMarker}$((Get-Location).Path)\"");
	}

	private void InitializeSession(ConsoleSession session)
	{
		_processService.TryWriteInput(session, "Set-Location -LiteralPath '" + session.WorkingDirectory.Replace("'", "''") + "'");
		RequestPrompt(session);
	}
}
