using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ZNext.Services;

internal sealed class ConsoleSessionProcessService
{
	private readonly ConsoleProcessFactory _processFactory = new();
	private readonly ProcessLifetimeService _processLifetimeService;

	public ConsoleSessionProcessService(ProcessLifetimeService processLifetimeService)
	{
		_processLifetimeService = processLifetimeService;
	}

	public ConsoleProcessStartResult StartShell(ConsoleSession session, Encoding encoding, ConsoleProcessHandlers handlers)
	{
		try
		{
			DisposeExistingProcess(session, handlers, killProcess: false);
			session.Process = new Process
			{
				StartInfo = _processFactory.CreateShellStartInfo(session.WorkingDirectory, encoding),
				EnableRaisingEvents = true
			};
			AttachHandlers(session.Process, handlers);
			session.Process.Start();
			_processLifetimeService.TryAssign(session.Process);
			session.Process.BeginOutputReadLine();
			session.Process.BeginErrorReadLine();
			return ConsoleProcessStartResult.FromSuccess(session.Process.Id);
		}
		catch (Exception ex)
		{
			return ConsoleProcessStartResult.FromError(ex.Message);
		}
	}

	public ConsoleProcessStartResult StartTunnel(ConsoleSession session, string command, Encoding encoding, ConsoleProcessHandlers handlers)
	{
		if (string.IsNullOrWhiteSpace(command))
		{
			return ConsoleProcessStartResult.FromError("启动命令为空。");
		}

		try
		{
			DisposeExistingProcess(session, handlers, killProcess: true);
			session.Process = new Process
			{
				StartInfo = _processFactory.CreateTunnelStartInfo(command, session.WorkingDirectory, encoding),
				EnableRaisingEvents = true
			};
			AttachHandlers(session.Process, handlers);
			session.Process.Start();
			_processLifetimeService.TryAssign(session.Process);
			session.Process.BeginOutputReadLine();
			session.Process.BeginErrorReadLine();
			return ConsoleProcessStartResult.FromSuccess(session.Process.Id);
		}
		catch (Exception ex)
		{
			return ConsoleProcessStartResult.FromError(ex.Message);
		}
	}

	public async Task StopAsync(ConsoleSession session, ConsoleProcessHandlers handlers)
	{
		if (session.Process == null)
		{
			return;
		}

		try
		{
			TrySendCtrlC(session);
		}
		catch
		{
		}

		await Task.Delay(120);
		Process process = session.Process;
		DetachHandlers(process, handlers);

		if (process.HasExited)
		{
			return;
		}

		TryCancelReads(process);
		try
		{
			process.Kill(entireProcessTree: true);
		}
		catch
		{
		}
	}

	public void StopImmediately(ConsoleSession session, ConsoleProcessHandlers handlers)
	{
		Process? process = session.Process;
		if (process == null)
		{
			return;
		}

		DetachHandlers(process, handlers);
		if (process.HasExited)
		{
			return;
		}

		TryCancelReads(process);
		try
		{
			process.Kill(entireProcessTree: true);
		}
		catch
		{
		}
	}

	public void DisposeProcess(ConsoleSession session, ConsoleProcessHandlers handlers)
	{
		DisposeExistingProcess(session, handlers, killProcess: false);
	}

	public bool TryWriteInput(ConsoleSession session, string command)
	{
		if (session.Process == null || session.Process.HasExited)
		{
			return false;
		}

		session.Process.StandardInput.WriteLine(command);
		session.Process.StandardInput.Flush();
		return true;
	}

	public bool TrySendCtrlC(ConsoleSession session)
	{
		if (session.Process == null || session.Process.HasExited)
		{
			return false;
		}

		Stream baseStream = session.Process.StandardInput.BaseStream;
		baseStream.WriteByte(3);
		baseStream.Flush();
		session.Process.StandardInput.Flush();
		return true;
	}

	public bool IsRunning(ConsoleSession? session)
	{
		return session?.Process != null && !session.Process.HasExited;
	}

	public int GetExitCode(ConsoleSession session, int fallback = -1)
	{
		try
		{
			return session.Process?.ExitCode ?? fallback;
		}
		catch
		{
			return fallback;
		}
	}

	public bool TryTerminateChildProcesses(ConsoleSession session)
	{
		return _processLifetimeService.TryTerminateChildProcesses(session.Process);
	}

	public bool TryTerminateMainProcess(ConsoleSession session)
	{
		if (session.Process == null || session.Process.HasExited)
		{
			return false;
		}

		try
		{
			session.Process.Kill(entireProcessTree: true);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void DisposeExistingProcess(ConsoleSession session, ConsoleProcessHandlers handlers, bool killProcess)
	{
		Process? process = session.Process;
		if (process == null)
		{
			return;
		}

		DetachHandlers(process, handlers);
		if (killProcess && !process.HasExited)
		{
			try
			{
				process.Kill(entireProcessTree: true);
			}
			catch
			{
			}
		}

		process.Dispose();
		session.Process = null;
	}

	private static void AttachHandlers(Process process, ConsoleProcessHandlers handlers)
	{
		process.OutputDataReceived += handlers.OutputReceived;
		process.ErrorDataReceived += handlers.ErrorReceived;
		process.Exited += handlers.Exited;
	}

	private static void DetachHandlers(Process process, ConsoleProcessHandlers handlers)
	{
		try
		{
			process.OutputDataReceived -= handlers.OutputReceived;
			process.ErrorDataReceived -= handlers.ErrorReceived;
			process.Exited -= handlers.Exited;
		}
		catch
		{
		}
	}

	private static void TryCancelReads(Process process)
	{
		try
		{
			process.CancelOutputRead();
		}
		catch
		{
		}

		try
		{
			process.CancelErrorRead();
		}
		catch
		{
		}
	}
}
