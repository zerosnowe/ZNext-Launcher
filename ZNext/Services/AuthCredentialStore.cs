using ZNext.Infrastructure.Settings;

namespace ZNext.Services;

internal sealed class AuthCredentialStore
{
	private const string TokenSettingKey = "AuthToken";
	private const string RememberLoginSettingKey = "AuthRememberLogin";
	private const string RememberedUsernameSettingKey = "AuthRememberedUsername";

	private readonly IAppSettingsStore _settingsStore;

	public AuthCredentialStore(IAppSettingsStore settingsStore)
	{
		_settingsStore = settingsStore;
	}

	public string? LoadToken()
	{
		return _settingsStore.GetString(TokenSettingKey);
	}

	public void SaveToken(string token)
	{
		if (string.IsNullOrWhiteSpace(token))
		{
			ClearToken();
			return;
		}

		_settingsStore.SetString(TokenSettingKey, token);
	}

	public void ClearToken()
	{
		_settingsStore.Remove(TokenSettingKey);
	}

	public bool LoadRememberLogin()
	{
		return _settingsStore.GetBool(RememberLoginSettingKey, fallback: true);
	}

	public string? LoadRememberedUsername()
	{
		return _settingsStore.GetString(RememberedUsernameSettingKey);
	}

	public void SaveLoginPreferences(string? username, bool rememberLogin)
	{
		_settingsStore.SetBool(RememberLoginSettingKey, rememberLogin);
		if (rememberLogin && !string.IsNullOrWhiteSpace(username))
		{
			_settingsStore.SetString(RememberedUsernameSettingKey, username.Trim());
			return;
		}

		_settingsStore.Remove(RememberedUsernameSettingKey);
	}
}
