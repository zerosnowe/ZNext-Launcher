using System.Diagnostics;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace ZNext.Services;

internal sealed class WindowLifecycleCoordinator
{
	private readonly Window _window;
	private readonly WindowChromeCoordinator _windowChromeCoordinator;
	private readonly WindowShutdownCoordinator _windowShutdownCoordinator;
	private readonly AppNotificationService _appNotificationService;
	private readonly Func<bool> _canHideToTray;
	private readonly Func<bool> _tryHideToTray;
	private readonly Action _disposeTrayIcon;
	private readonly Func<ICollection<ConsoleSession>> _consoleSessionsProvider;
	private readonly Func<ConsoleProcessHandlers> _consoleProcessHandlersProvider;
	private readonly Action _resetConsoleState;
	private readonly Func<IEnumerable<TunnelInfo>> _tunnelsProvider;
	private readonly Action _activateWindow;
	private readonly Action _navigateToConsole;
	private readonly Action _ensureConsoleInitialized;
	private bool _exitRequested;

	public WindowLifecycleCoordinator(
		Window window,
		WindowChromeCoordinator windowChromeCoordinator,
		WindowShutdownCoordinator windowShutdownCoordinator,
		AppNotificationService appNotificationService,
		Func<bool> canHideToTray,
		Func<bool> tryHideToTray,
		Action disposeTrayIcon,
		Func<ICollection<ConsoleSession>> consoleSessionsProvider,
		Func<ConsoleProcessHandlers> consoleProcessHandlersProvider,
		Action resetConsoleState,
		Func<IEnumerable<TunnelInfo>> tunnelsProvider,
		Action activateWindow,
		Action navigateToConsole,
		Action ensureConsoleInitialized)
	{
		_window = window;
		_windowChromeCoordinator = windowChromeCoordinator;
		_windowShutdownCoordinator = windowShutdownCoordinator;
		_appNotificationService = appNotificationService;
		_canHideToTray = canHideToTray;
		_tryHideToTray = tryHideToTray;
		_disposeTrayIcon = disposeTrayIcon;
		_consoleSessionsProvider = consoleSessionsProvider;
		_consoleProcessHandlersProvider = consoleProcessHandlersProvider;
		_resetConsoleState = resetConsoleState;
		_tunnelsProvider = tunnelsProvider;
		_activateWindow = activateWindow;
		_navigateToConsole = navigateToConsole;
		_ensureConsoleInitialized = ensureConsoleInitialized;
	}

	public bool IsWindowClosing { get; private set; }

	public void StartNotifications()
	{
		_appNotificationService.OpenConsoleRequested += NavigateToConsoleFromNotification;
		_appNotificationService.Start();
	}

	public void HandleAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
	{
		if (!_exitRequested && _canHideToTray() && _tryHideToTray())
		{
			args.Cancel = true;
			return;
		}

		if (_windowShutdownCoordinator.TryBeginClosePreparation())
		{
			args.Cancel = true;
			CloseAllTunnelsAndExitAsync();
		}
	}

	public void HandleWindowClosed(object sender, WindowEventArgs args)
	{
		IsWindowClosing = true;
		_windowChromeCoordinator.DetachClosing(HandleAppWindowClosing);
		_disposeTrayIcon();
		_windowShutdownCoordinator.CleanupResources(
			_consoleSessionsProvider(),
			_consoleProcessHandlersProvider(),
			() => _appNotificationService.OpenConsoleRequested -= NavigateToConsoleFromNotification,
			_resetConsoleState);
	}

	public void NavigateToConsoleFromNotification()
	{
		_window.DispatcherQueue.TryEnqueue(() =>
		{
			_activateWindow();
			_navigateToConsole();
			_ensureConsoleInitialized();
		});
	}

	public void RequestExit()
	{
		_exitRequested = true;
		_window.DispatcherQueue.TryEnqueue(() =>
		{
			try
			{
				_window.Close();
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Close from tray exit failed: " + ex.Message);
			}
		});
	}

	private async void CloseAllTunnelsAndExitAsync()
	{
		await _windowShutdownCoordinator.CompleteClosePreparationAsync(_tunnelsProvider());
		_window.DispatcherQueue.TryEnqueue(() =>
		{
			try
			{
				_window.Close();
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Close after tunnel shutdown failed: " + ex.Message);
			}
		});
	}
}
