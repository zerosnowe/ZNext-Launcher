using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ZNext.Services;

internal sealed class AvatarVisualController
{
	public void RefreshHomeAvatar(PersonPicture? avatarPicture, Border? fallback, string? avatarPath)
	{
		if (avatarPicture == null || fallback == null)
		{
			return;
		}

		if (HasAvatar(avatarPath))
		{
			avatarPicture.ProfilePicture = new BitmapImage(new Uri(avatarPath!, UriKind.Absolute));
			avatarPicture.Visibility = Visibility.Visible;
			fallback.Visibility = Visibility.Collapsed;
			return;
		}

		avatarPicture.ProfilePicture = null;
		avatarPicture.Visibility = Visibility.Collapsed;
		fallback.Visibility = Visibility.Visible;
	}

	public void RefreshTitleBarAvatar(
		PersonPicture? avatarPicture,
		FontIcon? userGlyph,
		PersonPicture? flyoutAvatarPicture,
		Border? flyoutGlyphHost,
		TextBlock? avatarStatusText,
		string? avatarPath)
	{
		try
		{
			if (HasAvatar(avatarPath) && avatarPicture != null && userGlyph != null)
			{
				BitmapImage bitmapImage = new BitmapImage
				{
					DecodePixelType = DecodePixelType.Logical,
					DecodePixelWidth = 160,
					DecodePixelHeight = 160,
					UriSource = new Uri(avatarPath!, UriKind.Absolute)
				};

				avatarPicture.ProfilePicture = null;
				avatarPicture.ProfilePicture = bitmapImage;
				avatarPicture.Visibility = Visibility.Visible;
				userGlyph.Visibility = Visibility.Collapsed;

				if (flyoutAvatarPicture != null)
				{
					flyoutAvatarPicture.ProfilePicture = null;
					flyoutAvatarPicture.ProfilePicture = bitmapImage;
					flyoutAvatarPicture.Visibility = Visibility.Visible;
				}

				if (flyoutGlyphHost != null)
				{
					flyoutGlyphHost.Visibility = Visibility.Collapsed;
				}

				if (avatarStatusText != null)
				{
					avatarStatusText.Text = "已设置";
				}

				return;
			}
		}
		catch
		{
		}

		ClearTitleBarAvatar(avatarPicture, userGlyph, flyoutAvatarPicture, flyoutGlyphHost, avatarStatusText);
	}

	private static bool HasAvatar(string? avatarPath)
	{
		return !string.IsNullOrWhiteSpace(avatarPath) && File.Exists(avatarPath);
	}

	private static void ClearTitleBarAvatar(
		PersonPicture? avatarPicture,
		FontIcon? userGlyph,
		PersonPicture? flyoutAvatarPicture,
		Border? flyoutGlyphHost,
		TextBlock? avatarStatusText)
	{
		if (avatarPicture != null)
		{
			avatarPicture.ProfilePicture = null;
			avatarPicture.Visibility = Visibility.Collapsed;
		}

		if (userGlyph != null)
		{
			userGlyph.Visibility = Visibility.Visible;
		}

		if (flyoutAvatarPicture != null)
		{
			flyoutAvatarPicture.ProfilePicture = null;
			flyoutAvatarPicture.Visibility = Visibility.Collapsed;
		}

		if (flyoutGlyphHost != null)
		{
			flyoutGlyphHost.Visibility = Visibility.Visible;
		}

		if (avatarStatusText != null)
		{
			avatarStatusText.Text = "未设置";
		}
	}
}
