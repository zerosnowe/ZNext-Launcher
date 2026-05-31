using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext.Services.Dialogs;

internal static class ModernDialogFactory
{
	public static ContentDialog Create(
		XamlRoot? xamlRoot,
		string title,
		object content,
		string? primaryButtonText = null,
		string closeButtonText = "取消",
		ContentDialogButton defaultButton = ContentDialogButton.Primary)
	{
		return new ContentDialog
		{
			Title = title,
			Content = content,
			PrimaryButtonText = primaryButtonText ?? string.Empty,
			CloseButtonText = closeButtonText,
			DefaultButton = defaultButton,
			XamlRoot = xamlRoot
		};
	}

	public static ScrollViewer Scrollable(UIElement content, double maxHeight = 560)
	{
		return new ScrollViewer
		{
			MaxHeight = maxHeight,
			HorizontalScrollMode = ScrollMode.Disabled,
			VerticalScrollMode = ScrollMode.Auto,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			Content = content
		};
	}
}
