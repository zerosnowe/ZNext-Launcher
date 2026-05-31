using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ZNext.Views.Dialogs;

namespace ZNext;

internal static class DialogHost
{
	private static readonly SemaphoreSlim DialogSemaphore = new SemaphoreSlim(1, 1);
	private static readonly TimeSpan DialogQueueTimeout = TimeSpan.FromSeconds(8);

	public static Task<ContentDialogResult> ShowAsync(ContentDialog dialog)
	{
		return ShowWithQueueAsync(dialog);
	}

	private static async Task<ContentDialogResult> ShowWithQueueAsync(ContentDialog dialog)
	{
		bool entered = await DialogSemaphore.WaitAsync(DialogQueueTimeout);
		if (!entered)
		{
			throw new TimeoutException("对话框排队等待超时。");
		}

		try
		{
			await EnsureXamlRootReadyAsync(dialog);
			ApplyStandardDialogStyle(dialog);
			NormalizeContent(dialog);
			ApplyThemeFromCurrentRoot(dialog);
			await WaitOneUiFrameAsync(dialog);
			return await dialog.ShowAsync().AsTask();
		}
		finally
		{
			DialogSemaphore.Release();
		}
	}

	private static void ApplyStandardDialogStyle(ContentDialog dialog)
	{
		if (Application.Current?.Resources.TryGetValue("DefaultContentDialogStyle", out object defaultStyle) == true
			&& defaultStyle is Style contentDialogStyle)
		{
			dialog.Style ??= contentDialogStyle;
		}

		if (!string.IsNullOrWhiteSpace(dialog.PrimaryButtonText)
			&& dialog.PrimaryButtonStyle == null
			&& Application.Current?.Resources.TryGetValue("AccentButtonStyle", out object accentStyle) == true
			&& accentStyle is Style primaryButtonStyle)
		{
			dialog.PrimaryButtonStyle = primaryButtonStyle;
		}
	}

	private static void NormalizeContent(ContentDialog dialog)
	{
		if (dialog.Content is string contentText)
		{
			dialog.Content = new ContentDialogContent(contentText);
		}

		if (dialog.Content is UIElement element && dialog.Content is not AnimatedDialogContent)
		{
			if (element is FrameworkElement frameworkElement && frameworkElement.DataContext == null)
			{
				frameworkElement.DataContext = dialog.DataContext;
			}

			dialog.Content = new AnimatedDialogContent
			{
				Body = element,
				DataContext = dialog.DataContext
			};
		}
	}

	private static void ApplyThemeFromCurrentRoot(ContentDialog dialog)
	{
		ElementTheme theme = ResolveCurrentTheme(dialog);
		dialog.RequestedTheme = theme;
		if (dialog.Content is FrameworkElement frameworkElement)
		{
			frameworkElement.RequestedTheme = theme;
		}
	}

	private static ElementTheme ResolveCurrentTheme(ContentDialog dialog)
	{
		if (dialog.XamlRoot?.Content is FrameworkElement rootElement)
		{
			return rootElement.ActualTheme;
		}

		if (App.CurrentAppWindow?.Content is FrameworkElement windowContent)
		{
			return windowContent.ActualTheme;
		}

		return ElementTheme.Default;
	}

	private static async Task EnsureXamlRootReadyAsync(ContentDialog dialog)
	{
		if (dialog.XamlRoot != null)
		{
			return;
		}

		for (int i = 0; i < 6 && dialog.XamlRoot == null; i++)
		{
			dialog.XamlRoot = App.TryGetCurrentXamlRoot();
			if (dialog.XamlRoot != null)
			{
				break;
			}

			await Task.Delay(16);
		}
	}

	private static async Task WaitOneUiFrameAsync(ContentDialog dialog)
	{
		_ = dialog;
		await Task.Yield();
		await Task.Delay(16);
	}
}
