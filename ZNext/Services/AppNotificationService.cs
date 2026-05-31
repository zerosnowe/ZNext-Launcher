using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace ZNext.Services;

internal sealed class AppNotificationService : IDisposable
{
	private const string ActionArgumentKey = "action";
	private const string OpenConsoleAction = "open_console";
	private bool _isListening;

	public event Action? OpenConsoleRequested;

	public static void Register()
	{
		try
		{
			AppNotificationManager.Default.Register();
		}
		catch (Exception ex)
		{
			Debug.WriteLine("AppNotificationManager.Register failed: " + ex.Message);
		}
	}

	public static bool IsOpenConsoleLaunch(string? launchArguments)
	{
		return !string.IsNullOrWhiteSpace(launchArguments)
			&& launchArguments.Contains($"action={OpenConsoleAction}", StringComparison.OrdinalIgnoreCase);
	}

	public void Start()
	{
		if (_isListening)
		{
			return;
		}

		try
		{
			AppNotificationManager.Default.NotificationInvoked += AppNotificationManager_NotificationInvoked;
			_isListening = true;
		}
		catch (Exception ex)
		{
			Debug.WriteLine("AppNotificationService.Start failed: " + ex.Message);
		}
	}

	public void ShowTunnelStarted(string? tunnelDisplayName)
	{
		try
		{
			string title = string.IsNullOrWhiteSpace(tunnelDisplayName) ? "隧道" : tunnelDisplayName;
			AppNotification notification = new AppNotificationBuilder()
				.AddText(title)
				.AddText("隧道已启动")
				.AddButton(new AppNotificationButton("查看详情").AddArgument(ActionArgumentKey, OpenConsoleAction))
				.BuildNotification();
			AppNotificationManager.Default.Show(notification);
		}
		catch (Exception ex)
		{
			Debug.WriteLine("ShowTunnelStarted notification failed: " + ex.Message);
		}
	}

	public void Dispose()
	{
		if (!_isListening)
		{
			return;
		}

		AppNotificationManager.Default.NotificationInvoked -= AppNotificationManager_NotificationInvoked;
		_isListening = false;
	}

	private void AppNotificationManager_NotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
	{
		if (TryGetAction(args?.Arguments, out string action)
			&& string.Equals(action, OpenConsoleAction, StringComparison.OrdinalIgnoreCase))
		{
			OpenConsoleRequested?.Invoke();
		}
	}

	private static bool TryGetAction(IDictionary<string, string>? arguments, out string action)
	{
		action = string.Empty;
		if (arguments == null)
		{
			return false;
		}

		foreach (KeyValuePair<string, string> pair in arguments)
		{
			if (!string.Equals(pair.Key, ActionArgumentKey, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			action = pair.Value ?? string.Empty;
			return !string.IsNullOrWhiteSpace(action);
		}

		return false;
	}
}
