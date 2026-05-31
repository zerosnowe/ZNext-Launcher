using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ZNext.ViewModels;

public sealed class CreateTunnelNodeCard
{
	private static readonly SolidColorBrush LoadBarHighBrush = new SolidColorBrush(Color.FromArgb(255, 220, 38, 38));
	private static readonly SolidColorBrush LoadBarMediumBrush = new SolidColorBrush(Color.FromArgb(255, 245, 158, 11));
	private static readonly SolidColorBrush LoadBarLowBrush = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94));

	public int NodeId { get; set; }

	public string NodeIdText => $"#{NodeId}";

	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public string RegionCode { get; set; } = string.Empty;

	public string RegionText { get; set; } = string.Empty;

	public string CountryCategory { get; set; } = "其他";

	public string BandwidthText { get; set; } = string.Empty;

	public int BandwidthMbps { get; set; }

	public int LoadPercent { get; set; }

	public string LoadText => $"{LoadPercent}%";

	public bool IsOnline { get; set; }

	public string[] Protocols { get; set; } = Array.Empty<string>();

	public string ProtocolText => Protocols.Length == 0
		? "-"
		: string.Join(" / ", Protocols.Select(protocol => protocol.ToUpperInvariant()));

	public int PortMin { get; set; } = 1;

	public int PortMax { get; set; } = 65535;

	public string[] AllowedGroups { get; set; } = Array.Empty<string>();

	public bool CanUse { get; set; }

	public bool IsOverloaded => LoadPercent >= 90;

	public Visibility OverloadIndicatorVisibility => IsOverloaded ? Visibility.Visible : Visibility.Collapsed;

	public bool IsVipNode { get; set; }

	public bool IsVipRestricted { get; set; }

	public Visibility VipIndicatorVisibility => IsVipRestricted && !IsOverloaded
		? Visibility.Visible
		: Visibility.Collapsed;

	public double CardOpacity => 1.0;

	public Brush LoadBarBrush { get; set; } = LoadBarLowBrush;

	public static Brush ResolveLoadBarBrush(int loadPercent)
	{
		if (loadPercent >= 80)
		{
			return LoadBarHighBrush;
		}
		if (loadPercent >= 50)
		{
			return LoadBarMediumBrush;
		}
		return LoadBarLowBrush;
	}
}
