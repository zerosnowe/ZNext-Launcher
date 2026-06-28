using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using ZNext.Infrastructure.Settings;
using IOPath = System.IO.Path;

namespace ZNext.Services;

public sealed class AvatarService
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
		StorageFolder avatarFolder = await ApplicationData.Current.LocalFolder
			.CreateFolderAsync(AvatarFolderName, CreationCollisionOption.OpenIfExists)
			.AsTask().ConfigureAwait(false);

		IReadOnlyList<StorageFile> existingFiles = await avatarFolder
			.GetFilesAsync()
			.AsTask().ConfigureAwait(false);

		foreach (StorageFile existingFile in existingFiles)
		{
			await existingFile
				.DeleteAsync(StorageDeleteOption.PermanentDelete)
				.AsTask().ConfigureAwait(false);
		}

		string extension = IOPath.GetExtension(pickedFile.Name);
		if (string.IsNullOrWhiteSpace(extension))
		{
			extension = ".png";
		}

		string avatarFileName = $"titlebar-avatar-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{extension.ToLowerInvariant()}";
		StorageFile savedFile = await pickedFile
			.CopyAsync(avatarFolder, avatarFileName, NameCollisionOption.ReplaceExisting)
			.AsTask().ConfigureAwait(false);

		_settingsStore.SetString(AvatarSettingKey, savedFile.Path);
		return savedFile.Path;
	}

	public async Task ClearAvatarAsync()
	{
		_settingsStore.Remove(AvatarSettingKey);

		StorageFolder avatarFolder;
		try
		{
			avatarFolder = await ApplicationData.Current.LocalFolder
				.GetFolderAsync(AvatarFolderName)
				.AsTask().ConfigureAwait(false);
		}
		catch
		{
			return;
		}

		IReadOnlyList<StorageFile> existingFiles = await avatarFolder
			.GetFilesAsync()
			.AsTask().ConfigureAwait(false);

		foreach (StorageFile existingFile in existingFiles)
		{
			await existingFile
				.DeleteAsync(StorageDeleteOption.PermanentDelete)
				.AsTask().ConfigureAwait(false);
		}
	}

}
