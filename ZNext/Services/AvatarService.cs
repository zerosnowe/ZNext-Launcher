using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using ZNext.Infrastructure.Settings;
using IOPath = System.IO.Path;

namespace ZNext.Services;

internal sealed class AvatarService
{
	private const string AvatarSettingKey = "TitleBarAvatarPath";
	private const string AvatarFolderName = "Avatar";

	private readonly IAppSettingsStore _settingsStore;

	public AvatarService()
		: this(new AppSettingsStore())
	{
	}

	public AvatarService(IAppSettingsStore settingsStore)
	{
		_settingsStore = settingsStore;
	}

	public string? LoadAvatarPath()
	{
		return _settingsStore.GetString(AvatarSettingKey);
	}

	public async Task<string> SaveAvatarAsync(StorageFile pickedFile)
	{
		StorageFolder avatarFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(AvatarFolderName, CreationCollisionOption.OpenIfExists);
		IReadOnlyList<StorageFile> existingFiles = await avatarFolder.GetFilesAsync();
		foreach (StorageFile existingFile in existingFiles)
		{
			await existingFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
		}

		string extension = IOPath.GetExtension(pickedFile.Name);
		if (string.IsNullOrWhiteSpace(extension))
		{
			extension = ".png";
		}

		string avatarFileName = $"titlebar-avatar-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{extension.ToLowerInvariant()}";
		StorageFile savedFile = await pickedFile.CopyAsync(
			avatarFolder,
			avatarFileName,
			NameCollisionOption.ReplaceExisting);

		_settingsStore.SetString(AvatarSettingKey, savedFile.Path);
		return savedFile.Path;
	}

}
