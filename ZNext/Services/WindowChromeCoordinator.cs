using System.Diagnostics;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using WinRT.Interop;

namespace ZNext.Services;

internal sealed class WindowChromeCoordinator
{
	private readonly Window _window;
	private readonly UIElement _titleBar;
	private readonly Button _backButton;
	private readonly FontIcon _backIcon;
	private readonly Func<FrameworkElement?> _contentProvider;
	private readonly TitleBarVisualController _titleBarVisualController;

	public WindowChromeCoordinator(
		Window window,
		UIElement titleBar,
		Button backButton,
		FontIcon backIcon,
		Func<FrameworkElement?> contentProvider,
		TitleBarVisualController titleBarVisualController)
	{
		_window = window;
		_titleBar = titleBar;
		_backButton = backButton;
		_backIcon = backIcon;
		_contentProvider = contentProvider;
		_titleBarVisualController = titleBarVisualController;
	}

	public AppWindow? MainAppWindow { get; private set; }

	public nint WindowHwnd { get; private set; }

	public void Configure(TypedEventHandler<AppWindow, AppWindowClosingEventArgs> closingHandler)
	{
		try
		{
			WindowHwnd = WindowNative.GetWindowHandle(_window);
			WindowId windowId = Win32Interop.GetWindowIdFromWindow(WindowHwnd);
			MainAppWindow = AppWindow.GetFromWindowId(windowId);
			_window.Title = ApplicationMetadata.DisplayName;
			_window.ExtendsContentIntoTitleBar = true;
			_window.SetTitleBar(_titleBar);

			if (MainAppWindow?.TitleBar != null)
			{
				MainAppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
				MainAppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
			}

			if (MainAppWindow != null)
			{
				MainAppWindow.Title = ApplicationMetadata.DisplayName;
				TrySetWindowIcon(MainAppWindow);
				MainAppWindow.Closing += closingHandler;
			}

			UpdateVisuals();
		}
		catch (Exception ex)
		{
			Debug.WriteLine("ConfigureWideTitleBar failed: " + ex.Message);
		}
	}

	private static void TrySetWindowIcon(AppWindow appWindow)
	{
		try
		{
			string? iconPath = ApplicationMetadata.ResolveIconPath();
			if (!string.IsNullOrWhiteSpace(iconPath))
			{
				appWindow.SetIcon(iconPath);
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("TrySetWindowIcon failed: " + ex.Message);
		}
	}

	public void DetachClosing(TypedEventHandler<AppWindow, AppWindowClosingEventArgs> closingHandler)
	{
		if (MainAppWindow != null)
		{
			MainAppWindow.Closing -= closingHandler;
		}
	}

	public void UpdateBackButton(string? currentTag)
	{
		_titleBarVisualController.UpdateBackButton(
			_backButton,
			_backIcon,
			MainAppWindow,
			_contentProvider(),
			currentTag);
	}

	public void UpdateVisuals()
	{
		_titleBarVisualController.UpdateVisuals(
			_backButton,
			_backIcon,
			MainAppWindow,
			_contentProvider());
	}

	public void ApplyAutoStartMinimize()
	{
		try
		{
			if (MainAppWindow?.Presenter is OverlappedPresenter presenter)
			{
				presenter.Minimize();
				return;
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("ApplyAutoStartMinimize failed: " + ex.Message);
		}

		Debug.WriteLine("Auto-start minimize fallback skipped because no overlapped presenter was available.");
	}
}
