#define DEBUG
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;
using Windows.ApplicationModel;
using ZNext.Infrastructure.Settings;
using ZNext.Navigation;
using ZNext.Services;
using ZNext.ViewModels;
using ZNext.Views;

namespace ZNext;

public sealed partial class MainWindow : Window
{
	private readonly AnnouncementService _announcementService;

	private readonly AuthService _authService;

	private readonly UserInfoService _userInfoService;

	private readonly SystemStatusService _systemStatusService;

	private readonly NodeService _nodeService;

	private readonly TunnelService _tunnelService;

	private readonly CreateProxyService _createProxyService;

	private readonly UserSessionService _userSessionService;

	private readonly HttpService _apiHttpService = new HttpService();

	private readonly AppNotificationService _appNotificationService = new AppNotificationService();

	private readonly AnnouncementContentRenderer _announcementContentRenderer = new AnnouncementContentRenderer();

	private readonly AnnouncementSectionController _announcementSectionController;

	private readonly ExternalLauncherService _externalLauncherService = new ExternalLauncherService();

	private readonly AppVersionService _appVersionService;

	private readonly ProcessLifetimeService _processLifetimeService = new ProcessLifetimeService();

	private readonly ClipboardService _clipboardService = new ClipboardService();

	private readonly UserActionCoordinator _userActionCoordinator;

	private readonly TopSuccessToastController _topSuccessToastController = new TopSuccessToastController();

	private readonly TitleBarVisualController _titleBarVisualController = new TitleBarVisualController();

	private readonly AvatarVisualController _avatarVisualController = new AvatarVisualController();

	private readonly ConsoleSessionProcessService _consoleSessionProcessService;

	private readonly ConsoleSessionCoordinator _consoleSessionCoordinator;

	private readonly TunnelConsoleSessionService _tunnelConsoleSessionService;

	private readonly ConsoleProcessExitCoordinator _consoleProcessExitCoordinator;

	private readonly WindowShutdownCoordinator _windowShutdownCoordinator;

	private readonly TunnelSessionCoordinator _tunnelSessionCoordinator;

	private readonly TunnelActionService _tunnelActionService;

	private readonly TunnelLinkCopyService _tunnelLinkCopyService;

	private readonly LauncherUpdateService _launcherUpdateService = new LauncherUpdateService();

	private readonly LauncherUpdateCoordinatorService _launcherUpdateCoordinatorService;

	private readonly LauncherUpdateOperationCoordinator _launcherUpdateOperationCoordinator;

	private readonly FrpcManagerService _frpcManagerService = new FrpcManagerService();

	private readonly FrpcSettingsService _frpcSettingsService;

	private readonly FrpcSettingsOperationCoordinator _frpcSettingsOperationCoordinator;

	private readonly SecuritySettingsCoordinator _securitySettingsCoordinator;

	private readonly AutoStartApplicationService _autoStartApplicationService = new AutoStartApplicationService();

	private readonly AutoStartTunnelSettingsService _autoStartTunnelSettingsService = new AutoStartTunnelSettingsService();

	private readonly AutoStartSettingsCoordinator _autoStartSettingsCoordinator;

	private readonly AutoStartTunnelLaunchCoordinator _autoStartTunnelLaunchCoordinator;

	private readonly AvatarService _avatarService = new AvatarService();

	private readonly AvatarSettingsCoordinator _avatarSettingsCoordinator;

	private readonly UserProfileSettingsService _userProfileSettingsService = new UserProfileSettingsService();

	private readonly CreateTunnelNodeCardMapper _createTunnelNodeCardMapper = new CreateTunnelNodeCardMapper();

	private readonly PageLoadStateCoordinator _pageLoadStateCoordinator = new PageLoadStateCoordinator();

	private readonly SettingsViewModel _settingsViewModel = new SettingsViewModel(new AppSettingsStore());

	private readonly HomeSectionViewModel _homeViewModel = new HomeSectionViewModel();

	private readonly NodeSectionViewModel _nodesViewModel = new NodeSectionViewModel(new NodeListQueryService());

	private readonly TunnelsSectionViewModel _tunnelsViewModel = new TunnelsSectionViewModel(new TunnelListQueryService());

	private readonly CreateTunnelSectionViewModel _createTunnelSectionViewModel = new CreateTunnelSectionViewModel(new CreateTunnelNodeQueryService());

	private readonly UserDialogService _userDialogService;

	private readonly SignCaptchaDialogService _signCaptchaDialogService = new SignCaptchaDialogService();

	private LoginDialogFlowService? _loginDialogFlowService;
	private TunnelDetailsDialogService? _tunnelDetailsDialogService;
	private CreateTunnelDialogService? _createTunnelDialogService;
	private EditTunnelDialogService? _editTunnelDialogService;
	private TunnelMenuActionCoordinator? _tunnelMenuActionCoordinator;
	private TunnelRunCoordinator? _tunnelRunCoordinator;
	private TunnelRunInteractionCoordinator? _tunnelRunInteractionCoordinator;
	private SettingsToggleCoordinator? _settingsToggleCoordinator;
	private SettingsOperationCoordinator? _settingsOperationCoordinator;
	private HomeActivationCoordinator? _homeActivationCoordinator;
	private HomeDataCoordinator? _homeDataCoordinator;
	private UserSessionOperationCoordinator? _userSessionOperationCoordinator;
	private PageDataLoadCoordinator? _pageDataLoadCoordinator;
	private PageSectionLoadCoordinator? _pageSectionLoadCoordinator;
	private ConsoleSectionCoordinator? _consoleSectionCoordinator;
	private TunnelCardInteractionCoordinator? _tunnelCardInteractionCoordinator;
	private SettingsSectionCoordinator? _settingsSectionCoordinator;
	private SectionInteractionCoordinator? _sectionInteractionCoordinator;
	private HomeSectionCoordinator? _homeSectionCoordinator;
	private AnnouncementInteractionCoordinator? _announcementInteractionCoordinator;
	private HomeInteractionCoordinator? _homeInteractionCoordinator;
	private CreateTunnelInteractionCoordinator? _createTunnelInteractionCoordinator;
	private HelpSectionCoordinator? _helpSectionCoordinator;
	private WindowChromeCoordinator? _windowChromeCoordinator;
	private WindowLifecycleCoordinator? _windowLifecycleCoordinator;
	private TrayIconService? _trayIconService;
	private ThemeApplicationCoordinator? _themeApplicationCoordinator;
	private AnnouncementSectionView? _announcementPanel;
	private ConsoleSectionView? _consolePanel;
	private CreateTunnelSectionView? _createTunnelPanel;
	private HelpSectionView? _helpPanel;
	private HomeSectionView? _homePanel;
	private NodeSectionView? _nodePanel;
	private SettingsSectionView? _settingsPanel;
	private TunnelsSectionView? _tunnelPanel;

	private ShellNavigationCoordinator? _shellNavigationCoordinator;

	private LoginDialogFlowService LoginDialogFlow => _loginDialogFlowService ??= new LoginDialogFlowService(
		GetCurrentXamlRoot,
		_userSessionService);

	private TunnelDetailsDialogService TunnelDetailsDialogService => _tunnelDetailsDialogService ??= new TunnelDetailsDialogService(
		GetCurrentXamlRoot,
		_userSessionService,
		_tunnelLinkCopyService,
		_clipboardService,
		_userDialogService,
		ShowTopSuccessToast);

	private CreateTunnelDialogService CreateTunnelDialogService => _createTunnelDialogService ??= new CreateTunnelDialogService(
		_createProxyService,
		_userSessionService);

	private EditTunnelDialogService EditTunnelDialogService => _editTunnelDialogService ??= new EditTunnelDialogService(_tunnelActionService);

	private TunnelMenuActionCoordinator TunnelMenuActionCoordinator => _tunnelMenuActionCoordinator ??= new TunnelMenuActionCoordinator(
		_userSessionService,
		_userDialogService,
		_tunnelActionService,
		_tunnelLinkCopyService,
		() => TunnelDetailsDialogService,
		() => EditTunnelDialogService,
		GetCurrentXamlRoot,
		TunnelRunInteractionCoordinator.StopAsync,
		ShowTopSuccessToast,
		() => _topSuccessToastController.Hide(TopSuccessInfoBar));

	private TunnelRunCoordinator TunnelRunCoordinator => _tunnelRunCoordinator ??= new TunnelRunCoordinator(
		_userSessionService,
		_userDialogService,
		_frpcSettingsService,
		_tunnelService,
		_tunnelConsoleSessionService,
		_tunnelSessionCoordinator,
		ShowTopSuccessToast);

	private TunnelRunInteractionCoordinator TunnelRunInteractionCoordinator => _tunnelRunInteractionCoordinator ??= new TunnelRunInteractionCoordinator(
		TunnelRunCoordinator,
		ConsoleSectionCoordinator,
		() => ShellNavigationCoordinator.Select("Settings"),
		() => ShellNavigationCoordinator.Select("Console"),
		() => ShellNavigationCoordinator.NavigateTo("Console"));

	private SettingsToggleCoordinator SettingsToggleCoordinator => _settingsToggleCoordinator ??= new SettingsToggleCoordinator(
		_settingsViewModel,
		_autoStartSettingsCoordinator,
		_securitySettingsCoordinator);

	private SettingsOperationCoordinator SettingsOperationCoordinator => _settingsOperationCoordinator ??= new SettingsOperationCoordinator(
		_frpcSettingsOperationCoordinator,
		_launcherUpdateOperationCoordinator,
		_avatarSettingsCoordinator);

	private HomeActivationCoordinator HomeActivationCoordinator => _homeActivationCoordinator ??= new HomeActivationCoordinator(
		_autoStartSettingsCoordinator.IsAutoStartLaunch(Environment.GetCommandLineArgs()),
		_userSessionService);

	private HomeDataCoordinator HomeDataCoordinator => _homeDataCoordinator ??= new HomeDataCoordinator(
		_userInfoService,
		_systemStatusService,
		_announcementService);

	private HomeSectionCoordinator HomeSectionCoordinator => _homeSectionCoordinator ??= new HomeSectionCoordinator(
		_homeViewModel,
		HomeDataCoordinator,
		_userDialogService,
		_userProfileSettingsService,
		_avatarService,
		_avatarVisualController,
		_announcementContentRenderer,
		AnnouncementInteractionCoordinator.LoadAsync,
		ResetCachedSectionsAfterLogout,
		() => _homePanel?.HomeBannerAvatarPicture,
		() => _homePanel?.HomeBannerAvatarFallback,
		() => _homePanel?.ImportantAnnouncementBodyRichTextBlock,
		() => _homePanel?.LoginButton,
		() => _homePanel?.LogoutButton,
		() => TitleBarFlyoutUsernameText,
		() => TitleBarFlyoutEmailText,
		() => StartupLoadingOverlay,
		() => StartupLoadingRing,
		() => StartupLoadingIconImage);

	private AnnouncementInteractionCoordinator AnnouncementInteractionCoordinator => _announcementInteractionCoordinator ??= new AnnouncementInteractionCoordinator(
		_userSessionService,
		_announcementSectionController,
		() => AnnouncementPanel,
		(panel, backButtonKey) => ShellNavigationCoordinator.ShowStandalone(panel, backButtonKey));

	private HomeInteractionCoordinator HomeInteractionCoordinator => _homeInteractionCoordinator ??= new HomeInteractionCoordinator(
		() => LoginDialogFlow,
		UserSessionOperationCoordinator,
		HomeSectionCoordinator,
		AnnouncementInteractionCoordinator,
		_pageLoadStateCoordinator,
		HomeActivationCoordinator,
		_homeViewModel,
		GetCurrentXamlRoot,
		() => ShellNavigationCoordinator.Select("Home"));

	private UserSessionOperationCoordinator UserSessionOperationCoordinator => _userSessionOperationCoordinator ??= new UserSessionOperationCoordinator(
		_userSessionService,
		_userInfoService,
		_userProfileSettingsService,
		_signCaptchaDialogService);

	private PageDataLoadCoordinator PageDataLoadCoordinator => _pageDataLoadCoordinator ??= new PageDataLoadCoordinator(
		_pageLoadStateCoordinator,
		_userSessionService,
		_nodeService,
		_tunnelService,
		_createProxyService,
		_userProfileSettingsService,
		_createTunnelNodeCardMapper,
		_nodesViewModel,
		_tunnelsViewModel,
		_createTunnelSectionViewModel);

	private PageSectionLoadCoordinator PageSectionLoadCoordinator => _pageSectionLoadCoordinator ??= new PageSectionLoadCoordinator(
		PageDataLoadCoordinator,
		_pageLoadStateCoordinator,
		_tunnelsViewModel,
		() => _ = NodePanel,
		() => SectionInteractionCoordinator.HasVisibleTunnelData,
		ConsoleSectionCoordinator.SyncTunnelLocalRunStates,
		SettingsSectionCoordinator.RebuildAutoStartTunnelChecklistUi,
		SectionInteractionCoordinator.ApplyTunnelFiltersAndLayout,
		SectionInteractionCoordinator.ApplyCreateTunnelNodeFilters);

	private ConsoleSectionCoordinator ConsoleSectionCoordinator => _consoleSectionCoordinator ??= new ConsoleSectionCoordinator(
		_consoleSessionProcessService,
		_consoleSessionCoordinator,
		_consoleProcessExitCoordinator,
		_tunnelSessionCoordinator,
		_tunnelConsoleSessionService,
		_frpcManagerService,
		_appNotificationService,
		base.DispatcherQueue,
		() => _consolePanel,
		() => base.Content is FrameworkElement frameworkElement ? frameworkElement.ActualTheme : ElementTheme.Default,
		() => WindowLifecycleCoordinator.IsWindowClosing,
		ShowTopSuccessToast);

	private TunnelCardInteractionCoordinator TunnelCardInteractionCoordinator => _tunnelCardInteractionCoordinator ??= new TunnelCardInteractionCoordinator(
		TunnelRunCoordinator,
		TunnelMenuActionCoordinator,
		TunnelRunInteractionCoordinator.CreateRunContext,
		() => PageSectionLoadCoordinator.IsRefreshingTunnelCards,
		PageSectionLoadCoordinator.ReloadTunnelCardsAsync,
		_pageLoadStateCoordinator.MarkTunnelsNotLoaded,
		() => PageSectionLoadCoordinator.LoadTunnelsAsync(),
		TunnelRunInteractionCoordinator.ApplyStartResult);

	private SettingsSectionCoordinator SettingsSectionCoordinator => _settingsSectionCoordinator ??= new SettingsSectionCoordinator(
		_settingsViewModel,
		SettingsToggleCoordinator,
		SettingsOperationCoordinator,
		_securitySettingsCoordinator,
		_autoStartSettingsCoordinator,
		_userSessionService,
		_tunnelsViewModel,
		_pageLoadStateCoordinator,
		_frpcSettingsService,
		_appVersionService,
		_userDialogService,
		_userActionCoordinator,
		() => _settingsPanel,
		() => SecurityLockOverlay,
		() => NavView,
		() => SecurityUnlockPasswordBox,
		() => SecurityUnlockErrorText,
		GetCurrentXamlRoot,
		GetOwnerHwnd,
		forceReload => PageSectionLoadCoordinator.LoadTunnelsAsync(forceReload),
		ThemeApplicationCoordinator.Apply,
		ShowTopSuccessToast,
		RefreshTitleBarUserAvatar,
		base.DispatcherQueue);

	private SectionInteractionCoordinator SectionInteractionCoordinator => _sectionInteractionCoordinator ??= new SectionInteractionCoordinator(
		_nodesViewModel,
		_tunnelsViewModel,
		_createTunnelSectionViewModel,
		() => _nodePanel,
		() => _tunnelPanel,
		() => _createTunnelPanel,
		() => MainContentScrollViewer);

	private CreateTunnelInteractionCoordinator CreateTunnelInteractionCoordinator => _createTunnelInteractionCoordinator ??= new CreateTunnelInteractionCoordinator(
		_userSessionService,
		_userDialogService,
		() => CreateTunnelDialogService,
		_pageLoadStateCoordinator,
		GetCurrentXamlRoot,
		ShowTopSuccessToast,
		() => PageSectionLoadCoordinator.LoadTunnelsAsync(),
		() => PageSectionLoadCoordinator.LoadCreateTunnelNodesAsync(forceReload: true));

	private HelpSectionCoordinator HelpSectionCoordinator => _helpSectionCoordinator ??= new HelpSectionCoordinator(
		_userActionCoordinator,
		_userDialogService);

	private WindowChromeCoordinator WindowChromeCoordinator => _windowChromeCoordinator ??= new WindowChromeCoordinator(
		this,
		AppTitleBar,
		TitleBarBackButton,
		TitleBarBackIcon,
		() => base.Content as FrameworkElement,
		_titleBarVisualController);

	private WindowLifecycleCoordinator WindowLifecycleCoordinator => _windowLifecycleCoordinator ??= new WindowLifecycleCoordinator(
		this,
		WindowChromeCoordinator,
		_windowShutdownCoordinator,
		_appNotificationService,
		() => _trayIconService?.CanHideToTray == true,
		() => _trayIconService?.TryHideToTray() == true,
		() => _trayIconService?.Dispose(),
		() => ConsoleSectionCoordinator.Sessions,
		ConsoleSectionCoordinator.CreateProcessHandlers,
		ConsoleSectionCoordinator.ResetState,
		() => _tunnelsViewModel.Tunnels,
		Activate,
		() =>
		{
			ShellNavigationCoordinator.Select("Console");
			ShellNavigationCoordinator.NavigateTo("Console");
		},
		ConsoleSectionCoordinator.EnsureInitialized);

	private TrayIconService TrayIconService => _trayIconService ??= new TrayIconService(
		this,
		() => WindowChromeCoordinator.MainAppWindow,
		GetOwnerHwnd,
		RequestExitFromTray);

	private ThemeApplicationCoordinator ThemeApplicationCoordinator => _themeApplicationCoordinator ??= new ThemeApplicationCoordinator(
		() => base.Content as FrameworkElement,
		_announcementSectionController,
		UpdateTitleBarVisuals,
		() => _consolePanel != null,
		ConsoleSectionCoordinator.RefreshUi);

	private ShellNavigationCoordinator ShellNavigationCoordinator => _shellNavigationCoordinator ??= new ShellNavigationCoordinator(
		NavView,
		SectionFrame,
		UpdateTitleBarBackButton,
		PrepareSectionHost,
		() => HomePanel,
		() => NodePanel,
		() => TunnelPanel,
		() => CreateTunnelPanel,
		() => HelpPanel,
		() => ConsolePanel,
		() => SettingsPanel,
		() =>
		{
			if (_pageLoadStateCoordinator.ShouldLoadNodes())
			{
				_ = PageSectionLoadCoordinator.LoadNodesAsync();
			}
		},
		() =>
		{
			if (_pageLoadStateCoordinator.ShouldLoadTunnels())
			{
				_ = PageSectionLoadCoordinator.LoadTunnelsAsync();
			}
		},
		() =>
		{
			if (_pageLoadStateCoordinator.ShouldLoadCreateTunnelNodes(forceReload: false))
			{
				_ = PageSectionLoadCoordinator.LoadCreateTunnelNodesAsync();
			}
		},
		() => ConsoleSectionCoordinator.EnsureInitialized(),
		RefreshSettingsSectionUi);

	private AnnouncementSectionView AnnouncementPanel => _announcementPanel ??= CreateAnnouncementSectionView();

	private HomeSectionView HomePanel => _homePanel ??= CreateHomeSectionView();

	private NodeSectionView NodePanel => _nodePanel ??= CreateNodeSectionView();

	private ConsoleSectionView ConsolePanel => _consolePanel ??= CreateConsoleSectionView();

	private CreateTunnelSectionView CreateTunnelPanel => _createTunnelPanel ??= CreateCreateTunnelSectionView();

	private HelpSectionView HelpPanel => _helpPanel ??= CreateHelpSectionView();

	private SettingsSectionView SettingsPanel => _settingsPanel ??= CreateSettingsSectionView();

	private TunnelsSectionView TunnelPanel => _tunnelPanel ??= CreateTunnelsSectionView();

	private TextBlock? AvatarStatusText => _settingsPanel?.AvatarStatusText;

	private bool _hasExecutedStartupTunnelAutoRun;

	public MainWindow(string? startupToken = null)
	{
		_appVersionService = new AppVersionService(typeof(MainWindow).Assembly);
		_consoleSessionProcessService = new ConsoleSessionProcessService(_processLifetimeService);
		_consoleSessionCoordinator = new ConsoleSessionCoordinator(_consoleSessionProcessService);
		_tunnelConsoleSessionService = new TunnelConsoleSessionService(_consoleSessionProcessService);
		_consoleProcessExitCoordinator = new ConsoleProcessExitCoordinator(_consoleSessionProcessService, _tunnelConsoleSessionService);
		_windowShutdownCoordinator = new WindowShutdownCoordinator(
			_consoleSessionProcessService,
			_frpcManagerService,
			_processLifetimeService,
			_appNotificationService,
			_apiHttpService,
			_topSuccessToastController);
		_tunnelSessionCoordinator = new TunnelSessionCoordinator(_tunnelConsoleSessionService, _consoleSessionProcessService);
		_userActionCoordinator = new UserActionCoordinator(_externalLauncherService, _clipboardService, _frpcManagerService);
		_launcherUpdateCoordinatorService = new LauncherUpdateCoordinatorService(_launcherUpdateService, _externalLauncherService, _appVersionService);
		_launcherUpdateOperationCoordinator = new LauncherUpdateOperationCoordinator(_launcherUpdateCoordinatorService);
		_frpcSettingsService = new FrpcSettingsService(_frpcManagerService);
		_frpcSettingsOperationCoordinator = new FrpcSettingsOperationCoordinator(_frpcSettingsService);
		_securitySettingsCoordinator = new SecuritySettingsCoordinator(new SecurityAccessService());
		_avatarSettingsCoordinator = new AvatarSettingsCoordinator(_avatarService);
		_autoStartSettingsCoordinator = new AutoStartSettingsCoordinator(_autoStartApplicationService, _autoStartTunnelSettingsService);
		_autoStartTunnelLaunchCoordinator = new AutoStartTunnelLaunchCoordinator(_autoStartTunnelSettingsService);
		_userDialogService = new UserDialogService(GetCurrentXamlRoot);
		_settingsViewModel.PropertyChanged += SettingsViewModel_PropertyChanged;
		if (DesignMode.DesignModeEnabled)
		{
			ApplyPlatformSymbolFont();
			_announcementService = new AnnouncementService();
			_announcementSectionController = new AnnouncementSectionController(_announcementService, _announcementContentRenderer, () => _announcementPanel);
			_authService = new AuthService();
			_userInfoService = new UserInfoService(_apiHttpService);
			_systemStatusService = new SystemStatusService(_apiHttpService);
			_nodeService = new NodeService(_apiHttpService);
			_tunnelService = new TunnelService(_apiHttpService);
			_createProxyService = new CreateProxyService(_apiHttpService);
			_userSessionService = new UserSessionService(_authService, _announcementService, _userInfoService, _systemStatusService, _nodeService, _tunnelService, _createProxyService);
			_tunnelActionService = new TunnelActionService(_tunnelService, _userSessionService);
			_tunnelLinkCopyService = new TunnelLinkCopyService(_tunnelService, _userSessionService, _clipboardService);
			InitializeComponent();
			InitializeViewModels();
			return;
		}
		ApplyPlatformSymbolFont();
		InitializeComponent();
		InitializeViewModels();
		_ = ShellNavigationCoordinator;
		WindowChromeCoordinator.Configure(WindowLifecycleCoordinator.HandleAppWindowClosing);
		TrayIconService.Initialize();
		_processLifetimeService.Initialize();
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		_announcementService = new AnnouncementService();
		_announcementSectionController = new AnnouncementSectionController(_announcementService, _announcementContentRenderer, () => _announcementPanel);
		_authService = new AuthService();
		if (!string.IsNullOrWhiteSpace(startupToken))
		{
			_authService.SetToken(startupToken, persist: true);
		}
		_userInfoService = new UserInfoService(_apiHttpService);
		_systemStatusService = new SystemStatusService(_apiHttpService);
		_nodeService = new NodeService(_apiHttpService);
		_tunnelService = new TunnelService(_apiHttpService);
		_createProxyService = new CreateProxyService(_apiHttpService);
		_userSessionService = new UserSessionService(_authService, _announcementService, _userInfoService, _systemStatusService, _nodeService, _tunnelService, _createProxyService);
		_tunnelActionService = new TunnelActionService(_tunnelService, _userSessionService);
		_tunnelLinkCopyService = new TunnelLinkCopyService(_tunnelService, _userSessionService, _clipboardService);
		_userSessionService.SynchronizeTokens();
		SettingsSectionCoordinator.InitializeStartupSettings();
		base.Activated += MainWindow_Activated;
		base.Closed += WindowLifecycleCoordinator.HandleWindowClosed;
		WindowLifecycleCoordinator.StartNotifications();
		HomeSectionCoordinator.EnsureStartupLoadingIconVisible();
		ShellNavigationCoordinator.Select("Home");
		UpdateTitleBarBackButton("Home");
	}

	private void InitializeViewModels()
	{
		if (_homePanel != null)
		{
			_homePanel.DataContext = _homeViewModel;
		}
		if (_settingsPanel != null)
		{
			_settingsPanel.DataContext = _settingsViewModel;
		}
	}

	private HomeSectionView CreateHomeSectionView()
	{
		HomeSectionView view = new HomeSectionView
		{
			DataContext = _homeViewModel
		};
		view.AnnouncementRequested += HomeBannerAnnouncementButton_Click;
		view.SignInRequested += HomeBannerSignInButton_Click;
		view.CloseImportantAnnouncementRequested += CloseImportantAnnouncementButton_Click;
		view.LoginRequested += LoginButton_Click;
		view.LogoutRequested += LogoutButton_Click;
		_homePanel = view;
		HomeSectionCoordinator.RefreshHomeBannerAvatar();
		HomeSectionCoordinator.UpdateButtonVisibility(_userSessionService.IsSignedIn);
		return view;
	}

	private AnnouncementSectionView CreateAnnouncementSectionView()
	{
		AnnouncementSectionView view = new AnnouncementSectionView();
		view.RefreshRequested += RefreshAnnouncementButton_Click;
		_announcementPanel = view;
		RefreshAnnouncementSectionUi();
		return view;
	}

	private NodeSectionView CreateNodeSectionView()
	{
		NodeSectionView view = new NodeSectionView
		{
			DataContext = _nodesViewModel
		};
		view.FilterChanged += (_, _) => SectionInteractionCoordinator.ApplyNodeFilters();
		view.SearchTextChanged += (_, _) => SectionInteractionCoordinator.ApplyNodeFilters();
		view.RetryRequested += RetryNodesButton_Click;
		_nodePanel = view;
		return view;
	}

	private TunnelsSectionView CreateTunnelsSectionView()
	{
		TunnelsSectionView view = new TunnelsSectionView
		{
			DataContext = _tunnelsViewModel
		};
		view.SectionSizeChanged += (_, _) => SectionInteractionCoordinator.UpdateTunnelsRepeaterWidth();
		view.SearchTextChanged += (_, _) => SectionInteractionCoordinator.HandleTunnelSearchChanged();
		view.GridModeRequested += (_, _) => SectionInteractionCoordinator.UseTunnelGridMode();
		view.ListModeRequested += (_, _) => SectionInteractionCoordinator.UseTunnelListMode();
		view.RefreshRequested += RefreshTunnelsButton_Click;
		view.RetryRequested += RetryTunnelsButton_Click;
		view.ViewDetailsRequested += async (sender, _) => await TunnelCardInteractionCoordinator.ShowDetailsAsync(sender);
		view.EditRequested += async (sender, _) => await TunnelCardInteractionCoordinator.EditAsync(sender);
		view.EnableRequested += async (sender, _) => await TunnelCardInteractionCoordinator.EnableAsync(sender);
		view.ForceOfflineRequested += async (sender, _) => await TunnelCardInteractionCoordinator.ForceOfflineAsync(sender);
		view.DeleteRequested += async (sender, _) => await TunnelCardInteractionCoordinator.DeleteAsync(sender);
		view.RunToggleLoaded += (sender, _) => TunnelCardInteractionCoordinator.HandleRunToggleLoaded(sender);
		view.RunToggleToggled += async (sender, _) => await TunnelCardInteractionCoordinator.HandleRunToggleToggledAsync(sender);
		view.CopyLinkRequested += async (sender, _) => await TunnelCardInteractionCoordinator.CopyLinkAsync(sender);
		_tunnelPanel = view;
		SectionInteractionCoordinator.RefreshTunnelsSectionUi();
		return view;
	}

	private CreateTunnelSectionView CreateCreateTunnelSectionView()
	{
		CreateTunnelSectionView view = new CreateTunnelSectionView
		{
			DataContext = _createTunnelSectionViewModel
		};
		view.SectionSizeChanged += (_, _) => SectionInteractionCoordinator.UpdateCreateTunnelRepeaterWidth();
		view.CountrySelectionChanged += (_, _) => SectionInteractionCoordinator.HandleCreateTunnelCountryChanged();
		view.FilterToggled += (sender, _) => SectionInteractionCoordinator.HandleCreateTunnelFilterToggled(sender);
		view.RefreshRequested += RefreshCreateTunnelButton_Click;
		view.SearchTextChanged += (_, _) => SectionInteractionCoordinator.HandleCreateTunnelSearchChanged();
		view.RetryRequested += RetryCreateTunnelButton_Click;
		view.CardClicked += CreateTunnelCard_Click;
		_createTunnelPanel = view;
		SectionInteractionCoordinator.RefreshCreateTunnelSectionUi();
		return view;
	}

	private HelpSectionView CreateHelpSectionView()
	{
		HelpSectionView view = new HelpSectionView();
		view.OpenLinkRequested += HelpOpenLinkButton_Click;
		view.CopyTextRequested += HelpCopyTextButton_Click;
		_helpPanel = view;
		return view;
	}

	private ConsoleSectionView CreateConsoleSectionView()
	{
		ConsoleSectionView view = new ConsoleSectionView();
		view.SessionSelectionChanged += (_, args) => ConsoleSectionCoordinator.HandleSessionSelectionChanged(args);
		view.InputKeyDown += (_, args) => ConsoleSectionCoordinator.HandleInputKeyDown(args);
		view.RunRequested += (_, _) => ConsoleSectionCoordinator.ExecuteCurrentInput();
		view.InterruptRequested += async (_, _) => await ConsoleSectionCoordinator.InterruptActiveAsync();
		_consolePanel = view;
		RefreshConsoleSectionUi();
		return view;
	}

	private SettingsSectionView CreateSettingsSectionView()
	{
		SettingsSectionView view = new SettingsSectionView
		{
			DataContext = _settingsViewModel
		};
		view.AutoStartToggled += async (sender, _) => await SettingsSectionCoordinator.HandleAutoStartToggledAsync(sender);
		view.AutoStartTunnelsToggled += async (sender, _) => await SettingsSectionCoordinator.HandleAutoStartTunnelsToggledAsync(sender);
		view.UploadAvatarRequested += async (_, _) => await SettingsSectionCoordinator.HandleUploadAvatarAsync();
		view.FrpcInstallRequested += async (_, _) => await SettingsSectionCoordinator.HandleFrpcInstallAsync();
		view.FrpcOpenDirectoryRequested += async (_, _) => await SettingsSectionCoordinator.HandleOpenFrpcDirectoryAsync();
		view.SecurityPasswordToggled += async (sender, _) => await SettingsSectionCoordinator.HandleSecurityPasswordToggledAsync(sender);
		view.SetSecurityPasswordRequested += async (_, _) => await SettingsSectionCoordinator.HandleSetSecurityPasswordAsync();
		view.FetchUpdateRequested += async (_, _) => await SettingsSectionCoordinator.HandleFetchUpdateAsync();

		_settingsPanel = view;
		RefreshSettingsSectionUi();
		return view;
	}

	private void RefreshSettingsSectionUi()
	{
		SettingsSectionCoordinator.RefreshUi();
	}

	private void RefreshConsoleSectionUi()
	{
		ConsoleSectionCoordinator.RefreshUi();
	}

	private static void ApplyPlatformSymbolFont()
	{
		try
		{
			FontFamily symbolFont = new FontFamily("Segoe Fluent Icons");
			if (Application.Current?.Resources != null)
			{
				Application.Current.Resources["AppSymbolFontFamily"] = symbolFont;
				Application.Current.Resources["SymbolThemeFontFamily"] = symbolFont;
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("ApplyPlatformSymbolFont failed: " + ex.Message);
		}
	}

	public void NavigateToConsoleFromNotification()
	{
		WindowLifecycleCoordinator.NavigateToConsoleFromNotification();
	}

	public async Task ShowStartupLoginDialogAsync()
	{
		_userSessionService.SynchronizeTokens();
		if (_userSessionService.IsSignedIn)
		{
			return;
		}

		await HomeInteractionCoordinator.LoginAsync(exitOnCancel: false);
	}

	private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
	{
		HomeActivationResult result = await HomeActivationCoordinator.ActivateAsync(CreateHomeActivationContext());
		if (result.ShouldApplyAutoStartMinimize)
		{
			TrayIconService.HideToTrayOrMinimize();
		}
	}

	private HomeActivationContext CreateHomeActivationContext()
	{
		return new HomeActivationContext(
			AnnouncementInteractionCoordinator.RefreshIfViewCreatedAsync,
			HomeSectionCoordinator.UpdateUserInfoAndUIAsync,
			HomeSectionCoordinator.LoadSystemStatusAsync,
			HomeSectionCoordinator.LoadImportantAnnouncementAsync,
			TryAutoStartConfiguredTunnelsOnLaunchAsync,
			HomeSectionCoordinator.ApplySignedInTokenState,
			HomeSectionCoordinator.ApplySignedOutState,
			HomeSectionCoordinator.HideStartupLoadingOverlayAsync);
	}

	private void RefreshAnnouncementSectionUi()
	{
		AnnouncementInteractionCoordinator.RefreshView();
	}

	private async void LoginButton_Click(object sender, RoutedEventArgs e)
	{
		await HomeInteractionCoordinator.LoginAsync();
	}

	private async void SignInButton_Click(object sender, RoutedEventArgs e)
	{
		await HomeInteractionCoordinator.SignInAsync();
	}

	private async void LogoutButton_Click(object sender, RoutedEventArgs e)
	{
		await HomeInteractionCoordinator.LogoutAsync();
	}

	private void ResetCachedSectionsAfterLogout()
	{
		_pageLoadStateCoordinator.ResetAll();
		_nodesViewModel.Clear();
		_tunnelsViewModel.Clear();
		_createTunnelSectionViewModel.Clear();
	}

	private async void HomeBannerAnnouncementButton_Click(object sender, RoutedEventArgs e)
	{
		await AnnouncementInteractionCoordinator.OpenAndLoadAsync();
	}

	private async void RefreshAnnouncementButton_Click(object sender, RoutedEventArgs e)
	{
		await AnnouncementInteractionCoordinator.LoadAsync();
	}

	private void HomeBannerSignInButton_Click(object sender, RoutedEventArgs e)
	{
		SignInButton_Click(sender, e);
	}

	private void CloseImportantAnnouncementButton_Click(object sender, RoutedEventArgs e)
	{
		HomeSectionCoordinator.DismissImportantAnnouncement();
	}

	private async void RetryNodesButton_Click(object sender, RoutedEventArgs e)
	{
		await PageSectionLoadCoordinator.LoadNodesAsync();
	}

	private async void RefreshTunnelsButton_Click(object sender, RoutedEventArgs e)
	{
		await PageSectionLoadCoordinator.ReloadTunnelCardsAsync();
	}

	private async void RetryTunnelsButton_Click(object sender, RoutedEventArgs e)
	{
		await PageSectionLoadCoordinator.ReloadTunnelCardsAsync();
	}

	private async void RefreshCreateTunnelButton_Click(object sender, RoutedEventArgs e)
	{
		await PageSectionLoadCoordinator.LoadCreateTunnelNodesAsync(forceReload: true);
	}

	private async void RetryCreateTunnelButton_Click(object sender, RoutedEventArgs e)
	{
		await PageSectionLoadCoordinator.LoadCreateTunnelNodesAsync(forceReload: true);
	}

	private async void CreateTunnelCard_Click(object sender, RoutedEventArgs e)
	{
		if (sender is not Button { Tag: CreateTunnelNodeCard node })
		{
			return;
		}

		await CreateTunnelInteractionCoordinator.ShowAsync(node);
	}
	private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
	{
		ShellNavigationCoordinator.HandleSelectionChanged(args);
	}

	private void ShowTopSuccessToast(string message)
	{
		_topSuccessToastController.Show(TopSuccessInfoBar, message);
	}

	private async Task TryAutoStartConfiguredTunnelsOnLaunchAsync()
	{
		if (_hasExecutedStartupTunnelAutoRun)
		{
			return;
		}
		_hasExecutedStartupTunnelAutoRun = true;

		AutoStartTunnelLaunchResult result = await _autoStartTunnelLaunchCoordinator.TryLaunchAsync(
			_userSessionService.IsSignedIn,
			() => PageSectionLoadCoordinator.LoadTunnelsAsync(forceReload: true),
			() => _tunnelsViewModel.HasCachedTunnels,
			() => _tunnelsViewModel.Tunnels,
			tunnel => TunnelRunInteractionCoordinator.StartAsync(tunnel, showConsolePanel: false));

		if (result.HasSelectionSnapshot && result.SelectedTunnelIds != null)
		{
			SettingsSectionCoordinator.UpdateAutoStartTunnelSelectionSnapshot(result.SelectedTunnelIds);
		}

		if (result.ShouldShowToast)
		{
			ShowTopSuccessToast(result.ToastMessage);
		}
	}

	private XamlRoot? GetCurrentXamlRoot()
	{
		if (_shellNavigationCoordinator?.ActivePageRootElement?.XamlRoot != null)
		{
			return _shellNavigationCoordinator.ActivePageRootElement.XamlRoot;
		}
		if (NavView?.XamlRoot != null)
		{
			return NavView.XamlRoot;
		}
		return (base.Content as FrameworkElement)?.XamlRoot;
	}

	private nint GetOwnerHwnd()
	{
		return WindowChromeCoordinator.WindowHwnd != IntPtr.Zero
			? WindowChromeCoordinator.WindowHwnd
			: WindowNative.GetWindowHandle(this);
	}

	private void SecurityUnlockButton_Click(object sender, RoutedEventArgs e)
	{
		SettingsSectionCoordinator.HandleSecurityUnlock();
	}

	private void SecurityUnlockPasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
	{
		SettingsSectionCoordinator.HandleSecurityUnlockKeyDown(e);
	}

	private void SettingsViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		SettingsSectionCoordinator.HandleViewModelPropertyChanged(e.PropertyName);
	}

	private void RefreshTitleBarUserAvatar()
	{
		string? avatarPath = _avatarService.LoadAvatarPath();
		_avatarVisualController.RefreshTitleBarAvatar(
			TitleBarAvatarPicture,
			TitleBarUserGlyph,
			TitleBarFlyoutAvatarPicture,
			TitleBarFlyoutUserGlyphHost,
			AvatarStatusText,
			avatarPath);
		HomeSectionCoordinator.RefreshHomeBannerAvatar();
	}

	private void TitleBarBackButton_Click(object sender, RoutedEventArgs e)
	{
		if (string.Equals(_shellNavigationCoordinator?.CurrentKey, "Announcement", StringComparison.OrdinalIgnoreCase))
		{
			ShellNavigationCoordinator.NavigateTo("Home");
		}
		else
		{
			ShellNavigationCoordinator.Select("Home");
		}
	}

	private void TitleBarLogoutMenuItem_Click(object sender, RoutedEventArgs e)
	{
		if (_userSessionService.IsSignedIn)
		{
			LogoutButton_Click(sender, e);
		}
	}

	private async void HelpOpenLinkButton_Click(object sender, RoutedEventArgs e)
	{
		string? link = (sender as FrameworkElement)?.Tag as string;
		await HelpSectionCoordinator.OpenLinkAsync(link);
	}

	private async void HelpCopyTextButton_Click(object sender, RoutedEventArgs e)
	{
		string? text = (sender as FrameworkElement)?.Tag as string;
		await HelpSectionCoordinator.CopyTextAsync(text);
	}

	private void UpdateTitleBarBackButton(string? currentTag)
	{
		WindowChromeCoordinator.UpdateBackButton(currentTag);
	}

	private void RequestExitFromTray()
	{
		WindowLifecycleCoordinator.RequestExit();
	}

	private void PrepareSectionHost(string key)
	{
		bool useDirectHost = string.Equals(key, "CreateTunnel", StringComparison.OrdinalIgnoreCase);
		Panel targetHost = useDirectHost ? DirectSectionFrameHost : ScrolledSectionFrameHost;

		if (SectionFrame.Parent is Panel currentHost && !ReferenceEquals(currentHost, targetHost))
		{
			currentHost.Children.Remove(SectionFrame);
			targetHost.Children.Add(SectionFrame);
		}

		SectionFrame.Margin = useDirectHost ? new Thickness(0) : new Thickness(20);
		MainContentScrollViewer.Visibility = useDirectHost ? Visibility.Collapsed : Visibility.Visible;
		DirectSectionFrameHost.Visibility = useDirectHost ? Visibility.Visible : Visibility.Collapsed;
	}

	private void UpdateTitleBarVisuals()
	{
		WindowChromeCoordinator.UpdateVisuals();
	}

}
