using Microsoft.UI.Xaml;

namespace ZNext.Navigation;

internal sealed class SectionProvider
{
	private readonly IReadOnlyDictionary<string, SectionDescriptor> _registeredSections;

	public IReadOnlyDictionary<string, SectionDescriptor> RegisteredSections => _registeredSections;

	public SectionProvider(IReadOnlyDictionary<string, SectionDescriptor> registeredSections)
	{
		_registeredSections = registeredSections;
	}

	public FrameworkElement? GetSection(string key)
	{
		return _registeredSections.TryGetValue(key, out SectionDescriptor? descriptor)
			? descriptor.Section
			: null;
	}
}
