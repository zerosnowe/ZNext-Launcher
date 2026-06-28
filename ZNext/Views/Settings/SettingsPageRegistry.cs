using Microsoft.UI.Xaml.Controls;

namespace ZNext.Views.Settings;

internal static class SettingsRoutes
{
	public const string Settings = "Settings";
	public const string General = "General";
	public const string Startup = "Startup";
	public const string Core = "Core";
	public const string Security = "Security";
	public const string UserCenter = "UserCenter";
	public const string About = "About";
}

internal interface ISettingsBreadcrumbAware
{
	string Route { get; }
}

internal interface ISettingsPageHostAware
{
	void Attach(SettingsSectionView host);
}

internal static class SettingsPageRegistry
{
	public static Type GetPageType(string route)
	{
		return route switch
		{
			SettingsRoutes.General => typeof(GeneralPage),
			SettingsRoutes.Startup => typeof(StartupPage),
			SettingsRoutes.Core => typeof(CorePage),
			SettingsRoutes.Security => typeof(SecurityPage),
			SettingsRoutes.UserCenter => typeof(UserCenterPage),
			SettingsRoutes.About => typeof(AboutPage),
			_ => typeof(DefaultPage)
		};
	}

	public static string GetTitle(string route)
	{
		return route switch
		{
			SettingsRoutes.General => "个性化",
			SettingsRoutes.Startup => "启动管理",
			SettingsRoutes.Core => "核心管理",
			SettingsRoutes.Security => "安全访问",
			SettingsRoutes.UserCenter => "用户中心",
			SettingsRoutes.About => "关于应用",
			_ => "设置"
		};
	}

	public static string GetDescription(string route)
	{
		return route switch
		{
			SettingsRoutes.General => "主题、头像与个人偏好",
			SettingsRoutes.Startup => "应用自启动与隧道自动启动",
			SettingsRoutes.Core => "frpc 核心安装、状态与目录",
			SettingsRoutes.Security => "安全访问与密码保护",
			SettingsRoutes.UserCenter => "账户资料、资源配额与状态",
			SettingsRoutes.About => "版本信息与更新检查",
			_ => string.Empty
		};
	}
}
