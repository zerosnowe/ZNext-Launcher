using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace ZNext.Services;

public sealed class AvatarVisualController
{
	private int _homeAvatarRequestVersion;
	private string? _lastHomeAvatarRenderKey;

	public async void RefreshHomeAvatar(PersonPicture? avatarPicture, Border? fallback, string? avatarPath)
	{
		if (avatarPicture == null || fallback == null)
		{
			return;
		}

		int requestVersion = ++_homeAvatarRequestVersion;
		string? avatarRenderKey = GetAvatarRenderKey(avatarPath);
		if (avatarRenderKey == null)
		{
			if (!string.Equals(_lastHomeAvatarRenderKey, "none", StringComparison.Ordinal)
				|| avatarPicture.Visibility == Visibility.Visible)
			{
				ClearHomeAvatar(avatarPicture, fallback);
			}

			_lastHomeAvatarRenderKey = "none";
			return;
		}

		if (string.Equals(_lastHomeAvatarRenderKey, avatarRenderKey, StringComparison.Ordinal)
			&& avatarPicture.ProfilePicture != null)
		{
			return;
		}

		_lastHomeAvatarRenderKey = avatarRenderKey;
		try
		{
			if (HasAvatar(avatarPath))
			{
				ImageSource? avatarImage = await DpiAwareImageSourceFactory.CreateSquareThumbnailAsync(avatarPath!, avatarPicture, 72);
				if (requestVersion != _homeAvatarRequestVersion)
				{
					return;
				}
				if (avatarImage == null)
				{
					ClearHomeAvatar(avatarPicture, fallback);
					return;
				}

				avatarPicture.ProfilePicture = null;
				avatarPicture.ProfilePicture = avatarImage;
				avatarPicture.Visibility = Visibility.Visible;
				fallback.Visibility = Visibility.Collapsed;
				return;
			}
		}
		catch
		{
			// Silently handle exceptions to prevent unhandled error dialog
			if (requestVersion == _homeAvatarRequestVersion)
			{
				_lastHomeAvatarRenderKey = null;
				ClearHomeAvatar(avatarPicture, fallback);
			}
			return;
		}

		ClearHomeAvatar(avatarPicture, fallback);
	}

	private static void ClearHomeAvatar(PersonPicture avatarPicture, Border fallback)
	{
		avatarPicture.ProfilePicture = null;
		avatarPicture.Visibility = Visibility.Collapsed;
		fallback.Visibility = Visibility.Visible;
	}

	private static bool HasAvatar(string? avatarPath)
	{
		return !string.IsNullOrWhiteSpace(avatarPath) && File.Exists(avatarPath);
	}

	private static string? GetAvatarRenderKey(string? avatarPath)
	{
		if (!HasAvatar(avatarPath))
		{
			return null;
		}

		FileInfo fileInfo = new FileInfo(avatarPath!);
		return $"{fileInfo.FullName}:{fileInfo.LastWriteTimeUtc.Ticks}:{fileInfo.Length}";
	}
}
