using System.Collections.Generic;

namespace ZNext.Services;

internal sealed class CreateTunnelPortPresetService
{
	private static readonly string[] PresetLabels =
	{
		"自定义",
		"泰拉瑞亚（7777）",
		"饥荒-TCP（10999）",
		"饥荒-UDP（10998）",
		"幻兽帕鲁（8211）",
		"Hytale（5520）",
		"RDP远程桌面（3389）",
		"GSManger（3001）",
		"OpanelManger（3000）",
		"Cloudreve（5212）",
		"Alist/OpenList（5244）",
		"Emby/Jellyfin（8096）",
		"Plex（32400）"
	};

	public IReadOnlyList<string> Presets => PresetLabels;

	public CreateTunnelPortPresetResult Resolve(string presetText)
	{
		if (string.IsNullOrWhiteSpace(presetText))
		{
			return new CreateTunnelPortPresetResult(null, null);
		}

		int? port = TryExtractPort(presetText, out int parsedPort) && parsedPort is >= 1 and <= 65535
			? parsedPort
			: null;
		string preferredProtocol = presetText.Contains("udp", System.StringComparison.OrdinalIgnoreCase)
			? "UDP"
			: "TCP";

		return new CreateTunnelPortPresetResult(port, preferredProtocol);
	}

	private static bool TryExtractPort(string text, out int port)
	{
		port = 0;
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}

		int start = -1;
		for (int i = 0; i < text.Length; i++)
		{
			if (char.IsDigit(text[i]))
			{
				start = i;
				break;
			}
		}

		if (start < 0)
		{
			return false;
		}

		int end = start;
		while (end < text.Length && char.IsDigit(text[end]))
		{
			end++;
		}

		return int.TryParse(text.Substring(start, end - start), out port);
	}
}
