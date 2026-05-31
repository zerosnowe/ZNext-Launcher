using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace ZNext.Services;

internal sealed class TrayIconService : IDisposable
{
	private const uint WM_TRAYICON = 0x04C8;
	private const uint WM_NULL = 0x0000;
	private const uint WM_RBUTTONUP = 0x0205;
	private const uint WM_CONTEXTMENU = 0x007B;
	private const uint WM_LBUTTONDBLCLK = 0x0203;
	private const int GWLP_WNDPROC = -4;
	private const uint NIM_ADD = 0x00000000;
	private const uint NIM_DELETE = 0x00000002;
	private const uint NIF_MESSAGE = 0x00000001;
	private const uint NIF_ICON = 0x00000002;
	private const uint NIF_TIP = 0x00000004;
	private const uint IMAGE_ICON = 1;
	private const uint LR_LOADFROMFILE = 0x00000010;
	private const uint MF_STRING = 0x00000000;
	private const uint TPM_RIGHTBUTTON = 0x0002;
	private const uint TPM_RETURNCMD = 0x0100;
	private const uint TPM_NONOTIFY = 0x0080;
	private const uint TrayIconId = 1;
	private const uint TrayMenuOpenId = 1001;
	private const uint TrayMenuExitId = 1002;
	private static readonly nint IDI_APPLICATION = new(32512);

	private readonly Window _window;
	private readonly Func<AppWindow?> _appWindowProvider;
	private readonly Func<nint> _windowHandleProvider;
	private readonly Action _requestExit;
	private WndProcDelegate? _trayWndProcDelegate;
	private nint _windowHwnd;
	private nint _originalWndProc;
	private nint _trayMenuHandle;
	private nint _trayIconHandle;
	private bool _isTrayIconAdded;
	private bool _isDisposed;

	public TrayIconService(
		Window window,
		Func<AppWindow?> appWindowProvider,
		Func<nint> windowHandleProvider,
		Action requestExit)
	{
		_window = window;
		_appWindowProvider = appWindowProvider;
		_windowHandleProvider = windowHandleProvider;
		_requestExit = requestExit;
	}

	public bool CanHideToTray => !_isDisposed
		&& _isTrayIconAdded
		&& _windowHwnd != nint.Zero
		&& _appWindowProvider() != null;

	public void Initialize()
	{
		if (_isDisposed || _isTrayIconAdded)
		{
			return;
		}

		try
		{
			_windowHwnd = _windowHandleProvider();
			if (_windowHwnd == nint.Zero)
			{
				return;
			}

			_trayWndProcDelegate ??= TrayWndProc;
			if (_originalWndProc == nint.Zero)
			{
				_originalWndProc = SetWindowLongPtr(
					_windowHwnd,
					GWLP_WNDPROC,
					Marshal.GetFunctionPointerForDelegate(_trayWndProcDelegate));
			}

			_trayMenuHandle = CreatePopupMenu();
			if (_trayMenuHandle != nint.Zero)
			{
				AppendMenu(_trayMenuHandle, MF_STRING, TrayMenuOpenId, "打开");
				AppendMenu(_trayMenuHandle, MF_STRING, TrayMenuExitId, "退出");
			}

			string? iconPath = ApplicationMetadata.ResolveIconPath();
			if (!string.IsNullOrWhiteSpace(iconPath))
			{
				_trayIconHandle = LoadImage(nint.Zero, iconPath, IMAGE_ICON, 0, 0, LR_LOADFROMFILE);
			}

			if (_trayIconHandle == nint.Zero)
			{
				_trayIconHandle = LoadIcon(nint.Zero, IDI_APPLICATION);
			}

			NOTIFYICONDATA data = new()
			{
				cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
				hWnd = _windowHwnd,
				uID = TrayIconId,
				uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
				uCallbackMessage = WM_TRAYICON,
				hIcon = _trayIconHandle,
				szTip = ApplicationMetadata.DisplayName
			};

			_isTrayIconAdded = Shell_NotifyIcon(NIM_ADD, ref data);
		}
		catch (Exception ex)
		{
			Debug.WriteLine("TrayIconService.Initialize failed: " + ex.Message);
		}
	}

	public bool TryHideToTray()
	{
		try
		{
			if (!CanHideToTray)
			{
				return false;
			}

			_appWindowProvider()?.Hide();
			return true;
		}
		catch (Exception ex)
		{
			Debug.WriteLine("TrayIconService.TryHideToTray failed: " + ex.Message);
			return false;
		}
	}

	public void HideToTrayOrMinimize()
	{
		if (TryHideToTray())
		{
			return;
		}

		try
		{
			if (_appWindowProvider()?.Presenter is OverlappedPresenter presenter)
			{
				presenter.Minimize();
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("TrayIconService.HideToTrayOrMinimize fallback failed: " + ex.Message);
		}
	}

	public void RestoreFromTray()
	{
		_window.DispatcherQueue.TryEnqueue(() =>
		{
			try
			{
				AppWindow? appWindow = _appWindowProvider();
				if (appWindow?.Presenter is OverlappedPresenter presenter)
				{
					presenter.Restore();
				}

				appWindow?.Show();
				_window.Activate();
				if (_windowHwnd != nint.Zero)
				{
					SetForegroundWindow(_windowHwnd);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("TrayIconService.RestoreFromTray failed: " + ex.Message);
			}
		});
	}

	public void RequestExitFromTray()
	{
		_window.DispatcherQueue.TryEnqueue(() =>
		{
			try
			{
				Dispose();
				_requestExit();
			}
			catch (Exception ex)
			{
				Debug.WriteLine("TrayIconService.RequestExitFromTray failed: " + ex.Message);
			}
		});
	}

	public void Dispose()
	{
		if (_isDisposed)
		{
			return;
		}

		try
		{
			if (_isTrayIconAdded && _windowHwnd != nint.Zero)
			{
				NOTIFYICONDATA data = new()
				{
					cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
					hWnd = _windowHwnd,
					uID = TrayIconId
				};
				Shell_NotifyIcon(NIM_DELETE, ref data);
				_isTrayIconAdded = false;
			}

			if (_trayMenuHandle != nint.Zero)
			{
				DestroyMenu(_trayMenuHandle);
				_trayMenuHandle = nint.Zero;
			}

			if (_trayIconHandle != nint.Zero)
			{
				DestroyIcon(_trayIconHandle);
				_trayIconHandle = nint.Zero;
			}

			if (_windowHwnd != nint.Zero && _originalWndProc != nint.Zero)
			{
				SetWindowLongPtr(_windowHwnd, GWLP_WNDPROC, _originalWndProc);
				_originalWndProc = nint.Zero;
			}
		}
		catch
		{
		}
		finally
		{
			_trayWndProcDelegate = null;
			_isDisposed = true;
		}
	}

	private nint TrayWndProc(nint hWnd, uint msg, nint wParam, nint lParam)
	{
		if (msg == WM_TRAYICON && wParam == TrayIconId)
		{
			uint trayMessage = unchecked((uint)lParam.ToInt64());
			if (trayMessage == WM_RBUTTONUP || trayMessage == WM_CONTEXTMENU)
			{
				ShowTrayMenu();
			}
			else if (trayMessage == WM_LBUTTONDBLCLK)
			{
				RestoreFromTray();
			}

			return nint.Zero;
		}

		if (_originalWndProc != nint.Zero)
		{
			return CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
		}

		return DefWindowProc(hWnd, msg, wParam, lParam);
	}

	private void ShowTrayMenu()
	{
		if (_trayMenuHandle == nint.Zero || _windowHwnd == nint.Zero || !GetCursorPos(out POINT point))
		{
			return;
		}

		SetForegroundWindow(_windowHwnd);
		uint command = TrackPopupMenuEx(
			_trayMenuHandle,
			TPM_RIGHTBUTTON | TPM_RETURNCMD | TPM_NONOTIFY,
			point.X,
			point.Y,
			_windowHwnd,
			nint.Zero);

		switch (command)
		{
			case TrayMenuOpenId:
				RestoreFromTray();
				break;
			case TrayMenuExitId:
				RequestExitFromTray();
				break;
		}

		PostMessage(_windowHwnd, WM_NULL, nint.Zero, nint.Zero);
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct NOTIFYICONDATA
	{
		public uint cbSize;
		public nint hWnd;
		public uint uID;
		public uint uFlags;
		public uint uCallbackMessage;
		public nint hIcon;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string szTip;

		public uint dwState;
		public uint dwStateMask;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
		public string szInfo;

		public uint uTimeoutOrVersion;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
		public string szInfoTitle;

		public uint dwInfoFlags;
		public Guid guidItem;
		public nint hBalloonIcon;
	}

	private struct POINT
	{
		public int X;
		public int Y;
	}

	private delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern nint CreatePopupMenu();

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool DestroyMenu(nint hMenu);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool AppendMenu(nint hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

	[DllImport("user32.dll")]
	private static extern uint TrackPopupMenuEx(nint hmenu, uint fuFlags, int x, int y, nint hwnd, nint lptpm);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetCursorPos(out POINT lpPoint);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetForegroundWindow(nint hWnd);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern nint LoadImage(nint hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern nint LoadIcon(nint hInstance, nint lpIconName);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool DestroyIcon(nint hIcon);

	[DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
	private static extern nint CallWindowProc(nint lpPrevWndFunc, nint hWnd, uint msg, nint wParam, nint lParam);

	[DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
	private static extern nint DefWindowProc(nint hWnd, uint msg, nint wParam, nint lParam);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
	private static extern nint SetWindowLongPtr64(nint hWnd, int nIndex, nint dwNewLong);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
	private static extern int SetWindowLong32(nint hWnd, int nIndex, int dwNewLong);

	private static nint SetWindowLongPtr(nint hWnd, int nIndex, nint newLong)
	{
		if (IntPtr.Size == 8)
		{
			return SetWindowLongPtr64(hWnd, nIndex, newLong);
		}

		return new nint(SetWindowLong32(hWnd, nIndex, newLong.ToInt32()));
	}
}
