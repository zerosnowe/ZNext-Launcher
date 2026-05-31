using System.Diagnostics;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using ZNext.Views;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace ZNext.Services;

internal sealed class ConsoleSectionCoordinator
{
	private readonly ConsoleSessionProcessService _processService;
	private readonly ConsoleSessionCoordinator _sessionCoordinator;
	private readonly ConsoleSessionBuffer _sessionBuffer = new ConsoleSessionBuffer();
	private readonly ConsoleSessionTabRenderer _tabRenderer = new ConsoleSessionTabRenderer();
	private readonly ConsoleProcessOutputCoordinator _outputCoordinator = new ConsoleProcessOutputCoordinator();
	private readonly ConsoleProcessExitCoordinator _exitCoordinator;
	private readonly TunnelSessionCoordinator _tunnelSessionCoordinator;
	private readonly TunnelConsoleSessionService _tunnelConsoleSessionService;
	private readonly FrpcManagerService _frpcManagerService;
	private readonly AppNotificationService _appNotificationService;
	private readonly DispatcherQueue _dispatcherQueue;
	private readonly Func<ConsoleSectionView?> _viewAccessor;
	private readonly Func<ElementTheme> _getActualTheme;
	private readonly Func<bool> _isWindowClosing;
	private readonly Action<string> _showSuccessToast;
	private readonly Encoding _encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
	private readonly List<ConsoleSession> _sessions = new List<ConsoleSession>();

	private ConsoleOutputRenderer? _outputRenderer;
	private ConsoleSession? _activeSession;
	private bool _isInitialized;

	public ConsoleSectionCoordinator(
		ConsoleSessionProcessService processService,
		ConsoleSessionCoordinator sessionCoordinator,
		ConsoleProcessExitCoordinator exitCoordinator,
		TunnelSessionCoordinator tunnelSessionCoordinator,
		TunnelConsoleSessionService tunnelConsoleSessionService,
		FrpcManagerService frpcManagerService,
		AppNotificationService appNotificationService,
		DispatcherQueue dispatcherQueue,
		Func<ConsoleSectionView?> viewAccessor,
		Func<ElementTheme> getActualTheme,
		Func<bool> isWindowClosing,
		Action<string> showSuccessToast)
	{
		_processService = processService;
		_sessionCoordinator = sessionCoordinator;
		_exitCoordinator = exitCoordinator;
		_tunnelSessionCoordinator = tunnelSessionCoordinator;
		_tunnelConsoleSessionService = tunnelConsoleSessionService;
		_frpcManagerService = frpcManagerService;
		_appNotificationService = appNotificationService;
		_dispatcherQueue = dispatcherQueue;
		_viewAccessor = viewAccessor;
		_getActualTheme = getActualTheme;
		_isWindowClosing = isWindowClosing;
		_showSuccessToast = showSuccessToast;
	}

	public ICollection<ConsoleSession> Sessions => _sessions;

	public void RefreshUi()
	{
		InitializeOutputRenderer();
		RenderTabs();
		if (_activeSession != null)
		{
			SwitchActiveSession(_activeSession);
			return;
		}

		if (ConsolePathText != null)
		{
			ConsolePathText.Text = "工作目录: -";
		}
	}

	public void EnsureInitialized()
	{
		if (_isInitialized)
		{
			return;
		}

		_isInitialized = true;
		CreateSessionAndActivate();
	}

	public void SyncTunnelLocalRunStates(IEnumerable<TunnelInfo> tunnels)
	{
		_tunnelConsoleSessionService.SyncLocalRunStates(tunnels, _sessions);
	}

	public TunnelRunContext CreateTunnelRunContext()
	{
		return new TunnelRunContext(
			_sessions,
			title => CreateSessionAndActivate(title, startShell: false),
			ResolveWorkingDirectory,
			_encoding,
			CreateProcessHandlers,
			AppendOutput,
			SwitchActiveSession,
			() => _isInitialized = true);
	}

	public ConsoleProcessHandlers CreateProcessHandlers()
	{
		return new ConsoleProcessHandlers(ProcessOutputReceived, ProcessErrorReceived, ProcessExited);
	}

	public void ResetState()
	{
		_activeSession = null;
		_outputRenderer = null;
		_isInitialized = false;
	}

	public void HandleSessionSelectionChanged(NavigationViewSelectionChangedEventArgs args)
	{
		if (!_tabRenderer.IsUpdatingSelection
			&& args.SelectedItemContainer?.Tag is ConsoleSession session
			&& !ReferenceEquals(_activeSession, session))
		{
			SwitchActiveSession(session);
		}
	}

	public void HandleInputKeyDown(KeyRoutedEventArgs e)
	{
		if (e.Key != VirtualKey.Enter)
		{
			return;
		}

		e.Handled = true;
		ExecuteCurrentInput();
	}

	public void ExecuteCurrentInput()
	{
		if (ConsoleInputTextBox == null)
		{
			return;
		}

		string? command = ConsoleInputTextBox.Text?.Trim();
		if (string.IsNullOrWhiteSpace(command))
		{
			return;
		}

		ExecuteCommand(command);
		ConsoleInputTextBox.Text = string.Empty;
	}

	public async Task InterruptActiveAsync()
	{
		if (_activeSession == null)
		{
			return;
		}

		await _sessionCoordinator.InterruptAsync(_activeSession, AppendOutput);
	}

	private TextBlock? ConsolePathText => _viewAccessor()?.ConsolePathText;

	private NavigationView? ConsoleSessionsNavView => _viewAccessor()?.ConsoleSessionsNavView;

	private ScrollViewer? ConsoleOutputScrollViewer => _viewAccessor()?.ConsoleOutputScrollViewer;

	private RichTextBlock? ConsoleOutputRichText => _viewAccessor()?.ConsoleOutputRichText;

	private TextBox? ConsoleInputTextBox => _viewAccessor()?.ConsoleInputTextBox;

	private void InitializeOutputRenderer()
	{
		if (ConsoleOutputRichText == null || ConsoleOutputScrollViewer == null)
		{
			_outputRenderer = null;
			return;
		}

		_outputRenderer = new ConsoleOutputRenderer(ConsoleOutputRichText, ConsoleOutputScrollViewer);
	}

	private ConsoleSession CreateSessionAndActivate(string? title = null, bool startShell = true)
	{
		ConsoleSession session = _sessionCoordinator.CreateSession(title, ResolveWorkingDirectory());
		_sessions.Add(session);
		if (startShell)
		{
			_sessionCoordinator.StartShell(session, _encoding, CreateProcessHandlers(), AppendOutput);
		}

		SwitchActiveSession(session);
		RenderTabs();
		return session;
	}

	private string ResolveWorkingDirectory()
	{
		return _frpcManagerService.GetApplicationRootDirectory();
	}

	private void ProcessOutputReceived(object sender, DataReceivedEventArgs e)
	{
		ConsoleSession? session = GetSessionByProcessSender(sender);
		if (session == null)
		{
			return;
		}

		ConsoleProcessOutputResult result = _outputCoordinator.ProcessOutput(session, e.Data);
		if (!result.ShouldAppend)
		{
			return;
		}

		if (result.PromptUpdated)
		{
			_dispatcherQueue.TryEnqueue(delegate
			{
				if (_activeSession == session && ConsolePathText != null)
				{
					ConsolePathText.Text = "工作目录: " + session.WorkingDirectory;
				}
			});
		}

		AppendOutput(session, result.Line);
		if (result.TunnelReady)
		{
			_dispatcherQueue.TryEnqueue(delegate
			{
				string message = string.IsNullOrWhiteSpace(session.TunnelDisplayName)
					? "隧道启动成功"
					: "隧道 " + session.TunnelDisplayName + " 启动成功";
				_showSuccessToast(message);
			});
			_appNotificationService.ShowTunnelStarted(session.TunnelDisplayName);
		}
	}

	private void ProcessErrorReceived(object sender, DataReceivedEventArgs e)
	{
		ConsoleSession? session = GetSessionByProcessSender(sender);
		if (session == null || string.IsNullOrEmpty(e.Data))
		{
			return;
		}

		string sanitized = _outputCoordinator.SanitizeError(e.Data);
		if (!string.IsNullOrWhiteSpace(sanitized))
		{
			AppendOutput(session, "[ERR] " + sanitized);
		}
	}

	private void ProcessExited(object? sender, EventArgs e)
	{
		ConsoleSession? session = sender == null ? null : GetSessionByProcessSender(sender);
		if (session == null)
		{
			return;
		}

		ConsoleProcessExitResult exitResult = _exitCoordinator.ProcessExit(session, _isWindowClosing());
		AppendOutput(session, exitResult.ExitLine);
		if (!string.IsNullOrWhiteSpace(exitResult.RestartLine))
		{
			AppendOutput(session, exitResult.RestartLine);
		}

		if (exitResult.ShouldScheduleRestart)
		{
			ScheduleTunnelAutoRestart(session);
		}
	}

	private void ScheduleTunnelAutoRestart(ConsoleSession session)
	{
		Task.Run(async delegate
		{
			await Task.Delay(1200);
			_dispatcherQueue.TryEnqueue(delegate
			{
				string? tunnelStartCommand = session.TunnelStartCommand;
				if (_exitCoordinator.CanRunScheduledRestart(session, _isWindowClosing())
					&& !string.IsNullOrWhiteSpace(tunnelStartCommand))
				{
					_tunnelSessionCoordinator.StartProcess(
						session,
						tunnelStartCommand,
						ResolveWorkingDirectory(),
						_encoding,
						CreateProcessHandlers(),
						AppendOutput);
				}
			});
		});
	}

	private void AppendOutput(ConsoleSession session, string line)
	{
		if (_isWindowClosing())
		{
			return;
		}

		ConsoleSessionBufferAppendResult appendResult = _sessionBuffer.AppendLine(
			session,
			line,
			captureSnapshotWhenTrimmed: _activeSession == session);
		if (_activeSession != session)
		{
			return;
		}

		_dispatcherQueue.TryEnqueue(delegate
		{
			if (_activeSession != session || _isWindowClosing())
			{
				return;
			}

			if (appendResult.WasTrimmed)
			{
				RefreshOutput(appendResult.Snapshot ?? string.Empty);
			}
			else
			{
				AppendLineToActiveView(line);
			}
		});
	}

	private void RefreshOutput(string text)
	{
		_outputRenderer?.Render(text);
	}

	private void AppendLineToActiveView(string line)
	{
		string fallback = _activeSession == null ? string.Empty : _sessionBuffer.GetSnapshot(_activeSession);
		_outputRenderer?.AppendLine(line, fallback);
	}

	private ConsoleSession? GetSessionByProcessSender(object sender)
	{
		Process? process = sender as Process;
		if (process == null)
		{
			return null;
		}

		return _sessions.FirstOrDefault(session => session.Process == process);
	}

	private void SwitchActiveSession(ConsoleSession session)
	{
		bool hasChanged = !ReferenceEquals(_activeSession, session);
		_activeSession = session;
		if (ConsolePathText != null)
		{
			ConsolePathText.Text = "工作目录: " + session.WorkingDirectory;
		}

		RefreshOutput(_sessionBuffer.GetSnapshot(session));
		if (hasChanged)
		{
			RenderTabs();
		}
	}

	private void RenderTabs()
	{
		if (ConsoleSessionsNavView == null)
		{
			return;
		}

		_tabRenderer.Render(
			ConsoleSessionsNavView,
			_sessions,
			_activeSession,
			_getActualTheme(),
			ConsoleTabCloseButtonClick);
	}

	private async void ConsoleTabCloseButtonClick(object sender, RoutedEventArgs e)
	{
		if (sender is not Button { Tag: ConsoleSession session } button || session.IsClosing)
		{
			return;
		}

		session.IsClosing = true;
		button.IsEnabled = false;
		try
		{
			await CloseSessionAsync(session);
		}
		finally
		{
			session.IsClosing = false;
		}
	}

	private async Task CloseSessionAsync(ConsoleSession session)
	{
		if (_isWindowClosing() || !_sessions.Contains(session))
		{
			return;
		}

		await StopSessionAsync(session);
		_sessions.Remove(session);
		_processService.DisposeProcess(session, CreateProcessHandlers());
		if (_sessions.Count == 0)
		{
			_isInitialized = true;
			CreateSessionAndActivate();
			return;
		}

		ConsoleSession nextSession = _activeSession != session
			? _activeSession ?? _sessions[^1]
			: _sessions[^1];
		SwitchActiveSession(nextSession);
		RenderTabs();
	}

	private async Task StopSessionAsync(ConsoleSession session)
	{
		if (!_processService.IsRunning(session))
		{
			return;
		}

		if (session.IsTunnelSession)
		{
			session.TunnelStopRequested = true;
		}

		await _processService.StopAsync(session, CreateProcessHandlers());
	}

	private void ExecuteCommand(string command)
	{
		if (string.IsNullOrWhiteSpace(command))
		{
			return;
		}

		EnsureInitialized();
		if (_activeSession != null)
		{
			_sessionCoordinator.ExecuteCommand(_activeSession, command, _encoding, CreateProcessHandlers(), AppendOutput);
		}
	}
}
