using ZNext.Infrastructure.Settings;

namespace ZNext.Services;

internal sealed class UserProfileSettingsService
{
	private const string UserGroupKey = "UserGroup";

	private readonly IAppSettingsStore _settingsStore;

	public UserProfileSettingsService()
		: this(new AppSettingsStore())
	{
	}

	public UserProfileSettingsService(IAppSettingsStore settingsStore)
	{
		_settingsStore = settingsStore;
	}

	public string? LoadUserGroup()
	{
		return _settingsStore.GetString(UserGroupKey);
	}

	public void SaveUserGroup(string? userGroup)
	{
		_settingsStore.SetString(UserGroupKey, userGroup ?? string.Empty);
	}

	public void ClearUserGroup()
	{
		_settingsStore.Remove(UserGroupKey);
	}
}
