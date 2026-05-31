using Microsoft.UI.Xaml.Controls;

namespace ZNext.Services;

internal sealed class TopSuccessToastController : IDisposable
{
	private CancellationTokenSource? _hideCts;

	public void Show(InfoBar? infoBar, string message)
	{
		if (infoBar == null)
		{
			return;
		}

		infoBar.Severity = InfoBarSeverity.Success;
		infoBar.Message = string.IsNullOrWhiteSpace(message) ? "操作成功" : message;
		infoBar.Opacity = 1.0;
		infoBar.IsOpen = true;

		_hideCts?.Cancel();
		_hideCts?.Dispose();
		_hideCts = new CancellationTokenSource();
		_ = HideLaterAsync(infoBar, _hideCts.Token);
	}

	public void Hide(InfoBar? infoBar)
	{
		_hideCts?.Cancel();
		_hideCts?.Dispose();
		_hideCts = null;
		if (infoBar != null)
		{
			infoBar.IsOpen = false;
		}
	}

	public void Dispose()
	{
		_hideCts?.Cancel();
		_hideCts?.Dispose();
		_hideCts = null;
	}

	private static async Task HideLaterAsync(InfoBar infoBar, CancellationToken cancellationToken)
	{
		try
		{
			await Task.Delay(1800, cancellationToken);
		}
		catch (TaskCanceledException)
		{
			return;
		}

		if (!cancellationToken.IsCancellationRequested)
		{
			infoBar.IsOpen = false;
		}
	}
}
