using System;
using System.Diagnostics;
using System.IO;

namespace ZNext.Services;

internal sealed class ApplicationRestartService
{
	public bool RestartCurrentApplication()
	{
		try
		{
			string? processPath = Environment.ProcessPath;
			if (string.IsNullOrWhiteSpace(processPath) || !File.Exists(processPath))
			{
				return false;
			}

			Process? newProcess = Process.Start(new ProcessStartInfo
			{
				FileName = processPath,
				WorkingDirectory = Path.GetDirectoryName(processPath),
				UseShellExecute = true
			});

			int currentPid = Environment.ProcessId;
			int newPid = newProcess?.Id ?? -1;
			TryKillOldProcessInstances(processPath, currentPid, newPid);
			Process.GetCurrentProcess().Kill(entireProcessTree: true);
			return true;
		}
		catch (Exception ex)
		{
			Debug.WriteLine("RestartCurrentApplication failed: " + ex.Message);
			return false;
		}
	}

	private static void TryKillOldProcessInstances(string processPath, int currentPid, int newPid)
	{
		try
		{
			string processName = Path.GetFileNameWithoutExtension(processPath);
			foreach (Process process in Process.GetProcessesByName(processName))
			{
				try
				{
					if (process.Id == currentPid || process.Id == newPid)
					{
						continue;
					}

					string? otherPath = null;
					try
					{
						otherPath = process.MainModule?.FileName;
					}
					catch
					{
					}

					if (!string.IsNullOrWhiteSpace(otherPath)
						&& string.Equals(otherPath, processPath, StringComparison.OrdinalIgnoreCase)
						&& !process.HasExited)
					{
						process.Kill(entireProcessTree: true);
					}
				}
				catch
				{
				}
				finally
				{
					process.Dispose();
				}
			}
		}
		catch
		{
		}
	}
}
