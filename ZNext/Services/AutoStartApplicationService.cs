using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Win32;

namespace ZNext.Services;

internal sealed class AutoStartApplicationService
{
	private const string RunSubKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
	private const string RunValueName = "ZNext Launcher";
	private const string LegacyRunValueName = "ZNext";
	private const string LaunchArgument = "--autostart";

	public bool IsAutoStartLaunch(string[] args)
	{
		return args.Any(arg => string.Equals(arg?.Trim(), LaunchArgument, StringComparison.OrdinalIgnoreCase));
	}

	public bool IsEnabled()
	{
		try
		{
			using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunSubKeyPath, writable: false);
			return HasRunValue(key, RunValueName) || HasRunValue(key, LegacyRunValueName);
		}
		catch (Exception ex)
		{
			Debug.WriteLine("AutoStartApplicationService.IsEnabled failed: " + ex.Message);
			return false;
		}
	}

	public AutoStartChangeResult SetEnabled(bool enabled)
	{
		try
		{
			using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunSubKeyPath, writable: true)
				?? throw new InvalidOperationException("无法打开当前用户启动项注册表。");

			if (enabled)
			{
				key.SetValue(RunValueName, BuildRunCommand(), RegistryValueKind.String);
				key.DeleteValue(LegacyRunValueName, throwOnMissingValue: false);
				return AutoStartChangeResult.Success("已开启开机自启动");
			}

			key.DeleteValue(RunValueName, throwOnMissingValue: false);
			key.DeleteValue(LegacyRunValueName, throwOnMissingValue: false);
			return AutoStartChangeResult.Success("已关闭开机自启动");
		}
		catch (Exception ex)
		{
			return AutoStartChangeResult.Failure(enabled ? "未能启用开机自启动: " + ex.Message : "未能关闭开机自启动: " + ex.Message);
		}
	}

	private static string BuildRunCommand()
	{
		string executablePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
		if (string.IsNullOrWhiteSpace(executablePath))
		{
			throw new InvalidOperationException("无法解析当前应用程序路径。");
		}

		return $"\"{executablePath}\" {LaunchArgument}";
	}

	private static bool HasRunValue(RegistryKey? key, string valueName)
	{
		return key?.GetValue(valueName) is string value && !string.IsNullOrWhiteSpace(value);
	}
}

internal sealed record AutoStartChangeResult(bool Succeeded, string Message)
{
	public static AutoStartChangeResult Success(string message)
	{
		return new AutoStartChangeResult(true, message);
	}

	public static AutoStartChangeResult Failure(string message)
	{
		return new AutoStartChangeResult(false, message);
	}
}
