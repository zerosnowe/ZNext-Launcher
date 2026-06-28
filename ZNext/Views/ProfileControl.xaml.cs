using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using ZNext.Services;

namespace ZNext.Views;

public sealed partial class ProfileControl : UserControl
{
	private int _avatarRequestVersion;
	private bool _isSignedIn;
	private bool _isProfileFlyoutEnabled = true;
	private bool _pointerAnimationActive;
	private string? _lastAvatarRenderKey;
	private Storyboard? _profileButtonAnimation;
	private readonly MenuFlyout _signedOutProfileMenuFlyout;

	public ProfileControl()
	{
		InitializeComponent();
		_signedOutProfileMenuFlyout = CreateSignedOutProfileMenuFlyout();
		UpdateFlyoutState();
	}

	public event RoutedEventHandler? LogoutRequested;
	public event RoutedEventHandler? LoginRequested;
	public event RoutedEventHandler? ProfileClicked;

	public bool IsProfileFlyoutEnabled
	{
		get => _isProfileFlyoutEnabled;
		set
		{
			if (_isProfileFlyoutEnabled == value)
			{
				return;
			}

			_isProfileFlyoutEnabled = value;
			UpdateFlyoutState();
		}
	}

	public void SetProfileText(string username, string email)
	{
		string normalizedUsername = string.IsNullOrWhiteSpace(username) ? "未登录" : username;
		_isSignedIn = !string.Equals(normalizedUsername, "未登录", StringComparison.OrdinalIgnoreCase);
		UsernameText.Text = normalizedUsername;
		EmailText.Text = string.IsNullOrWhiteSpace(email) ? "-" : email;
		UpdateFlyoutState();
	}

	public async void RefreshAvatar(string? avatarPath, TextBlock? avatarStatusText)
	{
		int requestVersion = ++_avatarRequestVersion;
		string? avatarRenderKey = GetAvatarRenderKey(avatarPath);
		if (avatarRenderKey == null)
		{
			if (!string.Equals(_lastAvatarRenderKey, "none", StringComparison.Ordinal)
				|| AvatarPicture.Visibility == Visibility.Visible
				|| FlyoutAvatarPicture.Visibility == Visibility.Visible)
			{
				ClearAvatar(avatarStatusText);
			}
			else
			{
				SetAvatarStatus(avatarStatusText, isSet: false);
			}

			_lastAvatarRenderKey = "none";
			return;
		}

		if (string.Equals(_lastAvatarRenderKey, avatarRenderKey, StringComparison.Ordinal)
			&& AvatarPicture.ProfilePicture != null
			&& FlyoutAvatarPicture.ProfilePicture != null)
		{
			SetAvatarStatus(avatarStatusText, isSet: true);
			return;
		}

		_lastAvatarRenderKey = avatarRenderKey;
		try
		{
			if (HasAvatar(avatarPath))
			{
				// Load both images in parallel for better performance
				Task<SoftwareBitmapSource?> avatarTask = DpiAwareImageSourceFactory.CreateSquareThumbnailAsync(avatarPath!, AvatarPicture, 28);
				Task<SoftwareBitmapSource?> flyoutAvatarTask = DpiAwareImageSourceFactory.CreateSquareThumbnailAsync(avatarPath!, FlyoutAvatarPicture, 36);

				await Task.WhenAll(avatarTask, flyoutAvatarTask);

				if (requestVersion != _avatarRequestVersion)
				{
					return;
				}

				SoftwareBitmapSource? avatarImage = avatarTask.Result;
				SoftwareBitmapSource? flyoutAvatarImage = flyoutAvatarTask.Result;

				if (avatarImage == null || flyoutAvatarImage == null)
				{
					ClearAvatar(avatarStatusText);
					return;
				}

				AvatarPicture.ProfilePicture = null;
				AvatarPicture.ProfilePicture = avatarImage;
				AvatarPicture.Visibility = Visibility.Visible;
				UserGlyph.Visibility = Visibility.Collapsed;

				FlyoutAvatarPicture.ProfilePicture = null;
				FlyoutAvatarPicture.ProfilePicture = flyoutAvatarImage;
				FlyoutAvatarPicture.Visibility = Visibility.Visible;
				FlyoutUserGlyphHost.Visibility = Visibility.Collapsed;

				SetAvatarStatus(avatarStatusText, isSet: true);

				return;
			}
		}
		catch
		{
		}

		if (requestVersion == _avatarRequestVersion)
		{
			_lastAvatarRenderKey = null;
			ClearAvatar(avatarStatusText);
		}
	}

	private void LogoutButton_Click(object sender, RoutedEventArgs e)
	{
		LogoutRequested?.Invoke(sender, e);
	}

	private void LoginEntryMenuItem_Click(object sender, RoutedEventArgs e)
	{
		LoginRequested?.Invoke(sender, e);
	}

	private void ProfileButton_Click(object sender, RoutedEventArgs e)
	{
		if (!_pointerAnimationActive)
		{
			PlayProfileButtonClickAnimation();
		}

		_pointerAnimationActive = false;
		ProfileClicked?.Invoke(sender, e);
	}

	private void ProfileButton_PointerPressed(object sender, PointerRoutedEventArgs e)
	{
		_pointerAnimationActive = true;
		AnimateProfileButtonScale(0.92, 90);
	}

	private void ProfileButton_PointerReleased(object sender, PointerRoutedEventArgs e)
	{
		AnimateProfileButtonScale(1.0, 150);
	}

	private void ProfileButton_PointerCanceled(object sender, PointerRoutedEventArgs e)
	{
		AnimateProfileButtonScale(1.0, 150);
		_pointerAnimationActive = false;
	}

	private void ProfileButton_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
	{
		AnimateProfileButtonScale(1.0, 150);
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

	private void UpdateFlyoutState()
	{
		if (!_isProfileFlyoutEnabled)
		{
			ProfileButton.Flyout = null;
			return;
		}

		ProfileButton.Flyout = _isSignedIn ? SignedInProfileFlyout : _signedOutProfileMenuFlyout;
	}

	private void ClearAvatar(TextBlock? avatarStatusText)
	{
		AvatarPicture.ProfilePicture = null;
		AvatarPicture.Visibility = Visibility.Collapsed;
		UserGlyph.Visibility = Visibility.Visible;

		FlyoutAvatarPicture.ProfilePicture = null;
		FlyoutAvatarPicture.Visibility = Visibility.Collapsed;
		FlyoutUserGlyphHost.Visibility = Visibility.Visible;

		SetAvatarStatus(avatarStatusText, isSet: false);
	}

	private static void SetAvatarStatus(TextBlock? avatarStatusText, bool isSet)
	{
		if (avatarStatusText != null)
		{
			avatarStatusText.Text = isSet ? "已设置" : "未设置";
		}
	}

	private async void PlayProfileButtonClickAnimation()
	{
		try
		{
			AnimateProfileButtonScale(0.92, 80);
			await Task.Delay(90);
			AnimateProfileButtonScale(1.0, 150);
		}
		catch
		{
			// Prevent unhandled exception in async void
		}
	}

	private void AnimateProfileButtonScale(double scale, double durationMilliseconds)
	{
		_profileButtonAnimation?.Stop();
		_profileButtonAnimation = new Storyboard();
		_profileButtonAnimation.Children.Add(CreateScaleAnimation(ProfileButtonScaleTransform, "ScaleX", scale, durationMilliseconds));
		_profileButtonAnimation.Children.Add(CreateScaleAnimation(ProfileButtonScaleTransform, "ScaleY", scale, durationMilliseconds));
		_profileButtonAnimation.Begin();
	}

	private static DoubleAnimation CreateScaleAnimation(
		DependencyObject target,
		string property,
		double scale,
		double durationMilliseconds)
	{
		DoubleAnimation animation = new DoubleAnimation
		{
			To = scale,
			Duration = TimeSpan.FromMilliseconds(durationMilliseconds),
			EnableDependentAnimation = true,
			EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
		};
		Storyboard.SetTarget(animation, target);
		Storyboard.SetTargetProperty(animation, property);
		return animation;
	}

	private MenuFlyout CreateSignedOutProfileMenuFlyout()
	{
		MenuFlyout menuFlyout = new MenuFlyout
		{
			Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.BottomEdgeAlignedRight
		};
		MenuFlyoutItem loginItem = new MenuFlyoutItem
		{
			Text = "登录账户",
			Width = 220,
			Icon = new FontIcon
			{
				Glyph = "\uE77B",
				FontFamily = GetSymbolFontFamily()
			}
		};
		loginItem.Click += LoginEntryMenuItem_Click;
		menuFlyout.Items.Add(loginItem);
		return menuFlyout;
	}

	private static FontFamily GetSymbolFontFamily()
	{
		return Application.Current?.Resources != null
			&& Application.Current.Resources.TryGetValue("AppSymbolFontFamily", out object resource)
			&& resource is FontFamily fontFamily
				? fontFamily
				: new FontFamily("Segoe Fluent Icons");
	}
}
