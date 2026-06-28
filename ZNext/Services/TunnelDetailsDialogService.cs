using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using ZNext.Services.Dialogs;

namespace ZNext.Services;

internal sealed class TunnelDetailsDialogService
{
	private readonly Func<XamlRoot?> _xamlRootProvider;
	private readonly UserSessionService _userSessionService;
	private readonly TunnelLinkCopyService _tunnelLinkCopyService;
	private readonly ClipboardService _clipboardService;
	private readonly UserDialogService _userDialogService;
	private readonly Action<string> _showSuccessToast;
	private int _isDialogShowing;

	public TunnelDetailsDialogService(
		Func<XamlRoot?> xamlRootProvider,
		UserSessionService userSessionService,
		TunnelLinkCopyService tunnelLinkCopyService,
		ClipboardService clipboardService,
		UserDialogService userDialogService,
		Action<string> showSuccessToast)
	{
		_xamlRootProvider = xamlRootProvider;
		_userSessionService = userSessionService;
		_tunnelLinkCopyService = tunnelLinkCopyService;
		_clipboardService = clipboardService;
		_userDialogService = userDialogService;
		_showSuccessToast = showSuccessToast;
	}

	public async Task ShowAsync(TunnelInfo tunnel)
	{
		if (Interlocked.Exchange(ref _isDialogShowing, 1) == 1)
		{
			return;
		}

		try
		{
			XamlRoot? xamlRoot = _xamlRootProvider();
			if (xamlRoot == null)
			{
				return;
			}

			string? resolvedLink = await ResolveTunnelLinkAsync(tunnel);
			string content = TunnelDetailsFormatter.Format(tunnel, resolvedLink);
			UIElement detailsContent = CreateDetailsContent(tunnel, resolvedLink);
			ContentDialog dialog = ModernDialogFactory.Create(
				xamlRoot,
				"隧道详情",
				ModernDialogFactory.Scrollable(detailsContent),
				primaryButtonText: "复制",
				closeButtonText: "关闭",
				defaultButton: ContentDialogButton.Close);

			if (await DialogHost.ShowAsync(dialog) == ContentDialogResult.Primary)
			{
				_clipboardService.SetText(content);
				_showSuccessToast("隧道详情已复制到剪贴板");
			}
		}
		catch (COMException ex) when (ex.Message.Contains("Only a single ContentDialog can be open", StringComparison.OrdinalIgnoreCase))
		{
			Debug.WriteLine("TunnelDetailsDialogService skipped duplicated dialog open.");
		}
		catch (Exception ex)
		{
			await _userDialogService.ShowInfoAsync("隧道详情异常", ex.Message);
		}
		finally
		{
			Interlocked.Exchange(ref _isDialogShowing, 0);
		}
	}

	private async Task<string?> ResolveTunnelLinkAsync(TunnelInfo tunnel)
	{
		if (!_userSessionService.IsSignedIn)
		{
			return null;
		}

		TunnelLinkResult tunnelLinkResult = await _tunnelLinkCopyService.GetLinkWithRetryAsync(tunnel);
		return tunnelLinkResult.Success && !string.IsNullOrWhiteSpace(tunnelLinkResult.Link)
			? tunnelLinkResult.Link
			: null;
	}

	private static UIElement CreateDetailsContent(TunnelInfo tunnel, string? resolvedLink)
	{
		StackPanel panel = new StackPanel
		{
			Spacing = 10,
			MinWidth = 460
		};

		panel.Children.Add(CreateSummaryCard(tunnel));

		foreach (TunnelDetailsItem item in CreateDetailsItems(tunnel, resolvedLink))
		{
			panel.Children.Add(CreateDetailCard(item));
		}

		return panel;
	}

	private static SettingsCard CreateSummaryCard(TunnelInfo tunnel)
	{
		SettingsCard card = new SettingsCard
		{
			Header = string.IsNullOrWhiteSpace(tunnel.Name) ? "未命名隧道" : tunnel.Name,
			Description = $"{NormalizeText(tunnel.Type)} · {tunnel.IdDisplayText}",
			HeaderIcon = CreateFontIcon("\uE81B", 18),
			Content = CreateStatusBadges(tunnel)
		};

		return card;
	}

	private static StackPanel CreateStatusBadges(TunnelInfo tunnel)
	{
		StackPanel badges = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 6,
			VerticalAlignment = VerticalAlignment.Center
		};

		badges.Children.Add(CreateBadge(
			tunnel.OnlineStatusText,
			tunnel.IsOnlineResolved ? Color.FromArgb(255, 15, 133, 72) : Color.FromArgb(255, 96, 96, 96),
			Colors.White));

		if (tunnel.IsDisabledResolved)
		{
			badges.Children.Add(CreateBadge("禁用", Color.FromArgb(255, 245, 158, 11), Color.FromArgb(255, 31, 41, 55)));
		}

		if (tunnel.IsLocalRunning)
		{
			badges.Children.Add(CreateBadge("本地运行", Color.FromArgb(255, 0, 95, 184), Colors.White));
		}

		return badges;
	}

	private static Border CreateBadge(string text, Color background, Color foreground)
	{
		return new Border
		{
			Background = new SolidColorBrush(background),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(8, 3, 8, 3),
			Child = new TextBlock
			{
				Text = text,
				FontSize = 11,
				FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
				Foreground = new SolidColorBrush(foreground)
			}
		};
	}

	private static IEnumerable<TunnelDetailsItem> CreateDetailsItems(TunnelInfo tunnel, string? resolvedLink)
	{
		string domainText = ResolveDomainText(tunnel, resolvedLink);
		yield return new TunnelDetailsItem("\uE8A5", "隧道 ID", tunnel.IdDisplayText);
		yield return new TunnelDetailsItem("\uE8D4", "协议类型", NormalizeText(tunnel.Type));
		yield return new TunnelDetailsItem("\uE968", "本地地址", NormalizeText(tunnel.LocalAddr));
		yield return new TunnelDetailsItem("\uE8C8", "访问地址", domainText);
		yield return new TunnelDetailsItem("\uE7AD", "远程端口", tunnel.remotePort > 0 ? tunnel.remotePort.ToString() : "-");
		yield return new TunnelDetailsItem("\uE95A", "节点", NormalizeText(tunnel.NodeDisplayText));
		yield return new TunnelDetailsItem("\uE946", "状态", tunnel.OnlineStatusText);
		yield return new TunnelDetailsItem("\uE895", "客户端版本", NormalizeText(tunnel.clientVersion));
	}

	private static SettingsCard CreateDetailCard(TunnelDetailsItem item)
	{
		return new SettingsCard
		{
			Header = item.Label,
			Description = string.Empty,
			HeaderIcon = CreateFontIcon(item.IconGlyph, 16),
			Content = new TextBlock
			{
				Text = item.Value,
				TextWrapping = TextWrapping.Wrap,
				TextTrimming = TextTrimming.CharacterEllipsis,
				IsTextSelectionEnabled = true,
				MaxWidth = 300,
				Foreground = Application.Current.Resources.TryGetValue("TextFillColorPrimaryBrush", out object brush) && brush is Brush textBrush
					? textBrush
					: null
			}
		};
	}

	private static FontIcon CreateFontIcon(string glyph, double fontSize)
	{
		FontFamily fontFamily = Application.Current.Resources.TryGetValue("AppSymbolFontFamily", out object resource)
			&& resource is FontFamily appSymbolFont
				? appSymbolFont
				: new FontFamily("Segoe Fluent Icons");

		return new FontIcon
		{
			Glyph = glyph,
			FontFamily = fontFamily,
			FontSize = fontSize
		};
	}

	private static string ResolveDomainText(TunnelInfo tunnel, string? resolvedLink)
	{
		if (!string.IsNullOrWhiteSpace(resolvedLink))
		{
			return resolvedLink;
		}

		string[] domains = TunnelDetailsFormatter.ParseDomains(tunnel.domain);
		if (domains.Length > 0)
		{
			return string.Join(", ", domains);
		}

		return NormalizeText(tunnel.domain);
	}

	private static string NormalizeText(string? value)
	{
		return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
	}
}

internal sealed record TunnelDetailsItem(string IconGlyph, string Label, string Value);
