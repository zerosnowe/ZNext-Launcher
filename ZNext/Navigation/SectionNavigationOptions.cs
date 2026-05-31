using Microsoft.UI.Xaml;

namespace ZNext.Navigation;

internal sealed class SectionNavigationOptions
{
	public Action<string>? UpdateBackButton { get; init; }

	public Action<FrameworkElement?>? UpdateActivePageRoot { get; init; }

	public Action<string>? PrepareSectionHost { get; init; }
}
