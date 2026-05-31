using Microsoft.UI.Xaml;

namespace ZNext.Navigation;

internal sealed class SectionDescriptor
{
	private readonly Lazy<FrameworkElement> _section;

	public SectionDescriptor(string key, FrameworkElement section, Action? onNavigatedTo = null)
		: this(key, () => section, onNavigatedTo)
	{
	}

	public SectionDescriptor(string key, Func<FrameworkElement> sectionFactory, Action? onNavigatedTo = null)
	{
		Key = key;
		_section = new Lazy<FrameworkElement>(sectionFactory);
		OnNavigatedTo = onNavigatedTo;
	}

	public string Key { get; }

	public FrameworkElement Section => _section.Value;

	public bool IsCreated => _section.IsValueCreated;

	public Action? OnNavigatedTo { get; }
}
