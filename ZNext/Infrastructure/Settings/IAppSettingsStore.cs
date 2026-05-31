namespace ZNext.Infrastructure.Settings;

internal interface IAppSettingsStore
{
	string? GetString(string key);

	void SetString(string key, string value);

	bool GetBool(string key, bool fallback = false);

	void SetBool(string key, bool value);

	void Remove(string key);
}
