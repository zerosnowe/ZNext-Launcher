using Microsoft.UI.Xaml;

namespace ZNext.Navigation;

internal sealed class SectionProviderBuilder
{
	private readonly Dictionary<string, SectionDescriptor> _registeredSections = new(StringComparer.OrdinalIgnoreCase);

	public SectionProviderBuilder WithSection(string key, FrameworkElement section, Action? onNavigatedTo = null)
	{
		_registeredSections.Add(key, new SectionDescriptor(key, section, onNavigatedTo));
		return this;
	}

	public SectionProviderBuilder WithSection(string key, Func<FrameworkElement> sectionFactory, Action? onNavigatedTo = null)
	{
		_registeredSections.Add(key, new SectionDescriptor(key, sectionFactory, onNavigatedTo));
		return this;
	}

	public SectionProvider Build()
	{
		return new SectionProvider(new Dictionary<string, SectionDescriptor>(_registeredSections, StringComparer.OrdinalIgnoreCase));
	}
}
