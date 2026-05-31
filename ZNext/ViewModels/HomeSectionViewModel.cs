using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using ZNext.Services;

namespace ZNext.ViewModels;

internal sealed class HomeSectionViewModel : ObservableObject
{
	private static readonly SolidColorBrush RealnameOkBackgroundBrush = new(Color.FromArgb(255, 220, 252, 231));
	private static readonly SolidColorBrush RealnameOkForegroundBrush = new(Color.FromArgb(255, 22, 101, 52));
	private static readonly SolidColorBrush RealnameWarnBackgroundBrush = new(Color.FromArgb(255, 254, 226, 226));
	private static readonly SolidColorBrush RealnameWarnForegroundBrush = new(Color.FromArgb(255, 153, 27, 27));
	private static readonly SolidColorBrush RealnameNeutralBackgroundBrush = new(Color.FromArgb(255, 243, 244, 246));
	private static readonly SolidColorBrush RealnameNeutralForegroundBrush = new(Color.FromArgb(255, 55, 65, 81));
	private static readonly SolidColorBrush StatusHealthyBrush = new(Color.FromArgb(255, 16, 124, 65));
	private static readonly SolidColorBrush StatusErrorBrush = new(Color.FromArgb(255, 197, 15, 31));

	private string _welcomeText = "欢迎回来";
	private string _usernameText = "未登录";
	private string _userIdText = "# -";
	private string _bannerUserIdText = "用户ID -";
	private string _realnameText = "未认证";
	private Brush _realnameBadgeBackground = RealnameNeutralBackgroundBrush;
	private Brush _realnameTextForeground = RealnameNeutralForegroundBrush;
	private string _emailText = "未登录";
	private string _levelText = "未登录";
	private string _homeUserGroupText = "-";
	private Visibility _homeUserGroupTextVisibility = Visibility.Visible;
	private Visibility _homeUserGroupOfficialVisibility = Visibility.Collapsed;
	private string _regTimeText = "-";
	private string _proxyCountText = "- / -";
	private string _homeTunnelUsedText = "-";
	private string _homeTunnelMaxText = "-";
	private string _trafficText = "-";
	private string _homeTrafficStatsSubtitleText = "当前流量：-";
	private string _inBoundText = "-";
	private string _outBoundText = "-";
	private string _todaySignedText = "-";
	private string _signInButtonText = "签到";
	private bool _isSignInEnabled;
	private bool _isTunnelCountLoading;
	private Visibility _importantAnnouncementVisibility = Visibility.Collapsed;
	private string _importantAnnouncementTitle = "维护预告";
	private string _systemStatusTitle = "系统状态未知";
	private string _systemStatusRemark = "请先登录后获取系统状态";
	private Brush _systemStatusBrush = StatusErrorBrush;
	private string _systemStatusIconGlyph = "\uea39";

	public string WelcomeText
	{
		get => _welcomeText;
		private set => SetProperty(ref _welcomeText, value);
	}

	public string UsernameText
	{
		get => _usernameText;
		private set => SetProperty(ref _usernameText, value);
	}

	public string UserIdText
	{
		get => _userIdText;
		private set => SetProperty(ref _userIdText, value);
	}

	public string BannerUserIdText
	{
		get => _bannerUserIdText;
		private set => SetProperty(ref _bannerUserIdText, value);
	}

	public string RealnameText
	{
		get => _realnameText;
		private set => SetProperty(ref _realnameText, value);
	}

	public Brush RealnameBadgeBackground
	{
		get => _realnameBadgeBackground;
		private set => SetProperty(ref _realnameBadgeBackground, value);
	}

	public Brush RealnameTextForeground
	{
		get => _realnameTextForeground;
		private set => SetProperty(ref _realnameTextForeground, value);
	}

	public string EmailText
	{
		get => _emailText;
		private set => SetProperty(ref _emailText, value);
	}

	public string LevelText
	{
		get => _levelText;
		private set => SetProperty(ref _levelText, value);
	}

	public string HomeUserGroupText
	{
		get => _homeUserGroupText;
		private set => SetProperty(ref _homeUserGroupText, value);
	}

	public Visibility HomeUserGroupTextVisibility
	{
		get => _homeUserGroupTextVisibility;
		private set => SetProperty(ref _homeUserGroupTextVisibility, value);
	}

	public Visibility HomeUserGroupOfficialVisibility
	{
		get => _homeUserGroupOfficialVisibility;
		private set => SetProperty(ref _homeUserGroupOfficialVisibility, value);
	}

	public string RegTimeText
	{
		get => _regTimeText;
		private set => SetProperty(ref _regTimeText, value);
	}

	public string ProxyCountText
	{
		get => _proxyCountText;
		private set => SetProperty(ref _proxyCountText, value);
	}

	public string HomeTunnelUsedText
	{
		get => _homeTunnelUsedText;
		private set => SetProperty(ref _homeTunnelUsedText, value);
	}

	public string HomeTunnelMaxText
	{
		get => _homeTunnelMaxText;
		private set => SetProperty(ref _homeTunnelMaxText, value);
	}

	public string TrafficText
	{
		get => _trafficText;
		private set => SetProperty(ref _trafficText, value);
	}

	public string HomeTrafficStatsSubtitleText
	{
		get => _homeTrafficStatsSubtitleText;
		private set => SetProperty(ref _homeTrafficStatsSubtitleText, value);
	}

	public string InBoundText
	{
		get => _inBoundText;
		private set => SetProperty(ref _inBoundText, value);
	}

	public string OutBoundText
	{
		get => _outBoundText;
		private set => SetProperty(ref _outBoundText, value);
	}

	public string TodaySignedText
	{
		get => _todaySignedText;
		private set => SetProperty(ref _todaySignedText, value);
	}

	public string SignInButtonText
	{
		get => _signInButtonText;
		private set => SetProperty(ref _signInButtonText, value);
	}

	public bool IsSignInEnabled
	{
		get => _isSignInEnabled;
		set => SetProperty(ref _isSignInEnabled, value);
	}

	public bool IsTunnelCountLoading
	{
		get => _isTunnelCountLoading;
		set => SetProperty(ref _isTunnelCountLoading, value);
	}

	public Visibility ImportantAnnouncementVisibility
	{
		get => _importantAnnouncementVisibility;
		set => SetProperty(ref _importantAnnouncementVisibility, value);
	}

	public string ImportantAnnouncementTitle
	{
		get => _importantAnnouncementTitle;
		set => SetProperty(ref _importantAnnouncementTitle, value);
	}

	public string SystemStatusTitle
	{
		get => _systemStatusTitle;
		private set => SetProperty(ref _systemStatusTitle, value);
	}

	public string SystemStatusRemark
	{
		get => _systemStatusRemark;
		private set => SetProperty(ref _systemStatusRemark, value);
	}

	public Brush SystemStatusBrush
	{
		get => _systemStatusBrush;
		private set => SetProperty(ref _systemStatusBrush, value);
	}

	public string SystemStatusIconGlyph
	{
		get => _systemStatusIconGlyph;
		private set => SetProperty(ref _systemStatusIconGlyph, value);
	}

	public void ApplyUser(UserInfoData user)
	{
		string username = string.IsNullOrWhiteSpace(user.Username) ? "未设置" : user.Username;
		string email = string.IsNullOrWhiteSpace(user.Email) ? "未设置" : user.Email;
		string traffic = DisplayFormatter.FormatTraffic(user.Traffic);

		WelcomeText = "欢迎回来，" + username;
		UsernameText = username;
		UserIdText = $"# {user.UserId}";
		BannerUserIdText = $"用户ID #{user.UserId}";
		RealnameText = user.IsRealname ? "已实名" : "未实名";
		RealnameBadgeBackground = user.IsRealname ? RealnameOkBackgroundBrush : RealnameWarnBackgroundBrush;
		RealnameTextForeground = user.IsRealname ? RealnameOkForegroundBrush : RealnameWarnForegroundBrush;
		EmailText = email;
		LevelText = user.FriendlyGroup ?? "普通用户";
		ApplyHomeUserGroup(user.FriendlyGroup);
		RegTimeText = DisplayFormatter.FormatUnixTime(user.RegTime);
		ProxyCountText = $"{user.UsedProxies} / {user.MaxProxies}";
		HomeTunnelUsedText = user.UsedProxies.ToString();
		HomeTunnelMaxText = user.MaxProxies.ToString();
		TrafficText = traffic;
		HomeTrafficStatsSubtitleText = "当前流量：" + traffic;
		InBoundText = DisplayFormatter.FormatBandwidthFromApi(user.InBound);
		OutBoundText = DisplayFormatter.FormatBandwidthFromApi(user.OutBound);
		TodaySignedText = user.TodaySigned ? "今日已签到" : "今日未签到";
		SignInButtonText = user.TodaySigned ? "已签到" : "签到";
		IsSignInEnabled = !user.TodaySigned;
	}

	public void ApplySignedInTokenState()
	{
		WelcomeText = "欢迎回来";
		UsernameText = "已登录";
		EmailText = "正在获取账户信息";
		LevelText = "已登录";
		SetSystemStatus(false, "系统状态加载中", "正在获取系统状态");
	}

	public void ResetForLogout()
	{
		WelcomeText = "欢迎回来";
		UsernameText = "未登录";
		UserIdText = "# -";
		BannerUserIdText = "用户ID -";
		RealnameText = "未认证";
		RealnameBadgeBackground = RealnameNeutralBackgroundBrush;
		RealnameTextForeground = RealnameNeutralForegroundBrush;
		EmailText = "未登录";
		LevelText = "未登录";
		ApplyHomeUserGroup(null);
		RegTimeText = "-";
		ProxyCountText = "- / -";
		HomeTunnelUsedText = "-";
		HomeTunnelMaxText = "-";
		TrafficText = "-";
		HomeTrafficStatsSubtitleText = "当前流量：-";
		InBoundText = "-";
		OutBoundText = "-";
		TodaySignedText = "-";
		SignInButtonText = "签到";
		IsSignInEnabled = false;
		IsTunnelCountLoading = false;
		ImportantAnnouncementVisibility = Visibility.Collapsed;
		SetSystemStatus(false, "系统状态未知", "请先登录后获取系统状态");
	}

	public void ApplyHomeUserGroup(string? friendlyGroup)
	{
		string group = string.IsNullOrWhiteSpace(friendlyGroup) ? "-" : friendlyGroup.Trim();
		bool isOfficial = string.Equals(group, "正式用户", StringComparison.Ordinal);
		HomeUserGroupText = group;
		HomeUserGroupTextVisibility = isOfficial ? Visibility.Collapsed : Visibility.Visible;
		HomeUserGroupOfficialVisibility = isOfficial ? Visibility.Visible : Visibility.Collapsed;
	}

	public void SetSystemStatus(bool isHealthy, string title, string remark)
	{
		SystemStatusBrush = isHealthy ? StatusHealthyBrush : StatusErrorBrush;
		SystemStatusIconGlyph = isHealthy ? "\ue73e" : "\uea39";
		SystemStatusTitle = title;
		SystemStatusRemark = remark;
	}
}
