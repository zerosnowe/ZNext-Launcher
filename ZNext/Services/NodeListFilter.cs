namespace ZNext.Services;

internal sealed record NodeListFilter(
	string Keyword,
	bool HideOffline,
	bool HideOverloaded);
