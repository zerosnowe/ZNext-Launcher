using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.WinUI.Controls;
using ZNext.Services;
using ZNext.ViewModels;
using ZNext.Views.Settings;

namespace ZNext.Views;

public sealed partial class SettingsSectionView : UserControl
{
	private string _currentRoute = SettingsRoutes.Settings;
	private HomeSectionViewModel? _homeViewModel;
	private AvatarService? _avatarService;

	public SettingsSectionView()
	{
		InitializeComponent();
		DataContextChanged += SettingsSectionView_DataContextChanged;
		NavigateTo(SettingsRoutes.Settings, useTransition: false);
	}

	public event RoutedEventHandler? AutoStartToggled;
	public event RoutedEventHandler? AutoStartTunnelsToggled;
	public event RoutedEventHandler? UploadAvatarRequested;
	public event RoutedEventHandler? ClearAvatarRequested;
	public event RoutedEventHandler? SelectCustomBackgroundRequested;
	public event RoutedEventHandler? ClearCustomBackgroundRequested;
	public event RoutedEventHandler? FrpcInstallRequested;
	public event RoutedEventHandler? FrpcOpenDirectoryRequested;
	public event RoutedEventHandler? LaunchArgsRequested;
	public event RoutedEventHandler? SecurityPasswordToggled;
	public event RoutedEventHandler? SetSecurityPasswordRequested;
	public event RoutedEventHandler? FetchUpdateRequested;
	public event EventHandler<string>? UserCenterActionRequested;
	public event RoutedEventHandler? CopyAccessTokenRequested;
	public event RoutedEventHandler? LogoutRequested;
	public event EventHandler? PageControlsRegistered;
	public event EventHandler? NavigationStateChanged;

	public TextBlock? AvatarStatusText { get; private set; }
	public TextBlock? AutoStartTunnelListStatusText { get; private set; }
	public SettingsExpander? AutoStartTunnelsExpander { get; private set; }
	public ProgressRing? FrpcInstallBusyRing { get; private set; }
	public TextBlock? FrpcInstallBusyText { get; private set; }
	public Button? FrpcInstallButton { get; private set; }
	public FontIcon? FrpcStatusIcon { get; private set; }
	public bool CanGoBack => _currentRoute != SettingsRoutes.Settings && SettingsContentFrame.CanGoBack;
	public bool IsRootRoute => _currentRoute == SettingsRoutes.Settings;
	public bool HasRegisteredPageControls =>
		AvatarStatusText != null
		|| AutoStartTunnelListStatusText != null
		|| AutoStartTunnelsExpander != null
		|| FrpcInstallBusyRing != null
		|| FrpcInstallBusyText != null
		|| FrpcInstallButton != null
		|| FrpcStatusIcon != null;

	public void SetHomeViewModel(HomeSectionViewModel homeViewModel)
	{
		_homeViewModel = homeViewModel;
		ApplyHomeViewModelToCurrentPage();
	}

	public void SetAvatarServices(AvatarService avatarService)
	{
		_avatarService = avatarService;
		DispatcherQueue.TryEnqueue(RefreshSettingsAvatar);
	}

	public void RefreshSettingsAvatar()
	{
		if (_avatarService == null)
		{
			return;
		}

		string? avatarPath = _avatarService.LoadAvatarPath();
		GetCurrentProfileControl()?.RefreshAvatar(avatarPath, AvatarStatusText);
		UpdateAvatarStatus(avatarPath);
	}

	public void NavigateTo(string route, bool useTransition = true)
	{
		Type pageType = SettingsPageRegistry.GetPageType(route);
		if (_currentRoute == route && SettingsContentFrame.Content?.GetType() == pageType)
		{
			return;
		}

		SettingsNavigationTransition.DefaultNavigationTransitionInfo = CreateNavigationTransitionInfo(_currentRoute, route, useTransition);
		SettingsContentFrame.Navigate(pageType);
	}

	public bool TryGoBack()
	{
		if (!CanGoBack)
		{
			return false;
		}

		SettingsNavigationTransition.DefaultNavigationTransitionInfo = CreateBackNavigationTransitionInfo();
		SettingsContentFrame.GoBack();
		return true;
	}

	internal void RegisterGeneralControls(TextBlock avatarStatusText)
	{
		AvatarStatusText = avatarStatusText;
		OnPageControlsRegistered();
	}

	internal void RegisterStartupControls(TextBlock autoStartTunnelListStatusText, SettingsExpander autoStartTunnelsExpander)
	{
		AutoStartTunnelListStatusText = autoStartTunnelListStatusText;
		AutoStartTunnelsExpander = autoStartTunnelsExpander;
		OnPageControlsRegistered();
	}

	internal void RegisterCoreControls(
		ProgressRing frpcInstallBusyRing,
		TextBlock frpcInstallBusyText,
		Button frpcInstallButton,
		FontIcon frpcStatusIcon)
	{
		FrpcInstallBusyRing = frpcInstallBusyRing;
		FrpcInstallBusyText = frpcInstallBusyText;
		FrpcInstallButton = frpcInstallButton;
		FrpcStatusIcon = frpcStatusIcon;
		OnPageControlsRegistered();
	}

	internal void RaiseAutoStartToggled(object sender, RoutedEventArgs e)
	{
		AutoStartToggled?.Invoke(sender, e);
	}

	internal void RaiseAutoStartTunnelsToggled(object sender, RoutedEventArgs e)
	{
		AutoStartTunnelsToggled?.Invoke(sender, e);
	}

	internal void RaiseUploadAvatarRequested(object sender, RoutedEventArgs e)
	{
		UploadAvatarRequested?.Invoke(sender, e);
	}

	internal void RaiseClearAvatarRequested(object sender, RoutedEventArgs e)
	{
		ClearAvatarRequested?.Invoke(sender, e);
	}

	internal void RaiseSelectCustomBackgroundRequested(object sender, RoutedEventArgs e)
	{
		SelectCustomBackgroundRequested?.Invoke(sender, e);
	}

	internal void RaiseClearCustomBackgroundRequested(object sender, RoutedEventArgs e)
	{
		ClearCustomBackgroundRequested?.Invoke(sender, e);
	}

	internal void RaiseFrpcInstallRequested(object sender, RoutedEventArgs e)
	{
		FrpcInstallRequested?.Invoke(sender, e);
	}

	internal void RaiseFrpcOpenDirectoryRequested(object sender, RoutedEventArgs e)
	{
		FrpcOpenDirectoryRequested?.Invoke(sender, e);
	}

	internal void RaiseLaunchArgsRequested(object sender, RoutedEventArgs e)
	{
		LaunchArgsRequested?.Invoke(sender, e);
	}

	internal void RaiseSecurityPasswordToggled(object sender, RoutedEventArgs e)
	{
		SecurityPasswordToggled?.Invoke(sender, e);
	}

	internal void RaiseSetSecurityPasswordRequested(object sender, RoutedEventArgs e)
	{
		SetSecurityPasswordRequested?.Invoke(sender, e);
	}

	internal void RaiseFetchUpdateRequested(object sender, RoutedEventArgs e)
	{
		FetchUpdateRequested?.Invoke(sender, e);
	}

	internal void RaiseUserCenterActionRequested(string action)
	{
		UserCenterActionRequested?.Invoke(this, action);
	}

	internal void RaiseCopyAccessTokenRequested(object sender, RoutedEventArgs e)
	{
		CopyAccessTokenRequested?.Invoke(sender, e);
	}

	internal void RaiseLogoutRequested(object sender, RoutedEventArgs e)
	{
		LogoutRequested?.Invoke(sender, e);
	}

	private void SettingsSectionView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		ApplyDataContextToCurrentPage();
	}

	private void SettingsContentFrame_Navigated(object sender, NavigationEventArgs e)
	{
		_currentRoute = SettingsRoutes.Settings;
		if (SettingsContentFrame.Content is ISettingsBreadcrumbAware breadcrumbAware)
		{
			_currentRoute = breadcrumbAware.Route;
		}

		ResetRegisteredPageControls();
		ApplyDataContextToCurrentPage();
		AttachHostToCurrentPage();
		UpdateTextNavigation(_currentRoute);
		NavigationStateChanged?.Invoke(this, EventArgs.Empty);

		ApplyHomeViewModelToCurrentPage();
		RefreshSettingsAvatar();
	}

	private void ApplyHomeViewModelToCurrentPage()
	{
		if (_homeViewModel == null)
		{
			return;
		}

		if (SettingsContentFrame.Content is DefaultPage defaultPage)
		{
			defaultPage.HomeViewModel = _homeViewModel;
		}
		else if (SettingsContentFrame.Content is UserCenterPage userCenterPage)
		{
			userCenterPage.HomeViewModel = _homeViewModel;
		}
	}

	private ProfileControl? GetCurrentProfileControl()
	{
		return SettingsContentFrame.Content switch
		{
			DefaultPage defaultPage => defaultPage.GetProfileControl(),
			UserCenterPage userCenterPage => userCenterPage.GetProfileControl(),
			_ => null
		};
	}

	private void SettingsHomeNavigationButton_Click(object sender, RoutedEventArgs e)
	{
		if (_currentRoute == SettingsRoutes.Settings)
		{
			return;
		}

		if (SettingsContentFrame.CanGoBack)
		{
			SettingsNavigationTransition.DefaultNavigationTransitionInfo = CreateBackNavigationTransitionInfo();
			SettingsContentFrame.GoBack();
			return;
		}

		NavigateTo(SettingsRoutes.Settings);
	}

	private static NavigationTransitionInfo CreateNavigationTransitionInfo(string sourceRoute, string targetRoute, bool useTransition)
	{
		if (!useTransition)
		{
			return new SuppressNavigationTransitionInfo();
		}

		int sourceIndex = GetRouteIndex(sourceRoute);
		int targetIndex = GetRouteIndex(targetRoute);
		if (targetIndex > sourceIndex)
		{
			return new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight };
		}

		if (targetIndex < sourceIndex)
		{
			return CreateBackNavigationTransitionInfo();
		}

		return new EntranceNavigationTransitionInfo();
	}

	private static SlideNavigationTransitionInfo CreateBackNavigationTransitionInfo()
	{
		return new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft };
	}

	private static int GetRouteIndex(string route)
	{
		return route switch
		{
			SettingsRoutes.Settings => 0,
			SettingsRoutes.General => 1,
			SettingsRoutes.Startup => 2,
			SettingsRoutes.Core => 3,
			SettingsRoutes.Security => 4,
			SettingsRoutes.UserCenter => 5,
			SettingsRoutes.About => 6,
			_ => 0
		};
	}

	private void ApplyDataContextToCurrentPage()
	{
		if (SettingsContentFrame.Content is FrameworkElement element)
		{
			element.DataContext = DataContext;
		}
	}

	private void AttachHostToCurrentPage()
	{
		if (SettingsContentFrame.Content is ISettingsPageHostAware hostAware)
		{
			hostAware.Attach(this);
		}
	}

	private void UpdateTextNavigation(string route)
	{
		bool isRoot = route == SettingsRoutes.Settings;
		SettingsHomeNavigationButton.IsEnabled = !isRoot;
		SettingsHomeNavigationButton.Opacity = isRoot ? 1.0 : 0.82;
		SettingsNavigationSeparatorText.Visibility = isRoot ? Visibility.Collapsed : Visibility.Visible;
		SettingsCurrentNavigationText.Visibility = isRoot ? Visibility.Collapsed : Visibility.Visible;
		SettingsCurrentNavigationText.Text = isRoot ? string.Empty : SettingsPageRegistry.GetTitle(route);
	}

	private void OnPageControlsRegistered()
	{
		PageControlsRegistered?.Invoke(this, EventArgs.Empty);
	}

	private void ResetRegisteredPageControls()
	{
		AvatarStatusText = null;
		AutoStartTunnelListStatusText = null;
		AutoStartTunnelsExpander = null;
		FrpcInstallBusyRing = null;
		FrpcInstallBusyText = null;
		FrpcInstallButton = null;
		FrpcStatusIcon = null;
	}

	private void UpdateAvatarStatus(string? avatarPath)
	{
		if (AvatarStatusText != null)
		{
			AvatarStatusText.Text = !string.IsNullOrWhiteSpace(avatarPath) && System.IO.File.Exists(avatarPath)
				? "已设置"
				: "未设置";
		}
	}
}
