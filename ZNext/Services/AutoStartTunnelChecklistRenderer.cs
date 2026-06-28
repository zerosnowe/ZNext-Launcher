using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.WinUI.Controls;

namespace ZNext.Services;

internal sealed class AutoStartTunnelChecklistRenderer
{
	public void RenderMessage(SettingsExpander? expander, TextBlock? statusText, string message)
	{
		expander?.Items.Clear();

		if (statusText != null)
		{
			statusText.Text = message;
		}
	}

	public void RenderItems(
		SettingsExpander? expander,
		TextBlock? statusText,
		IReadOnlyList<AutoStartTunnelChecklistItem> items,
		RoutedEventHandler selectionChanged)
	{
		if (expander == null || statusText == null)
		{
			return;
		}

		expander.Items.Clear();
		foreach (AutoStartTunnelChecklistItem item in items)
		{
			ToggleSwitch toggle = new ToggleSwitch
			{
				Tag = item.Id,
				IsOn = item.IsChecked,
				OffContent = "关闭",
				OnContent = "开启"
			};
			toggle.Toggled += selectionChanged;

			SettingsCard card = new SettingsCard
			{
				Header = item.Label,
				Content = toggle
			};

			expander.Items.Add(card);
		}

		statusText.Text = items.Count > 0 ? string.Empty : "暂无隧道";
	}
}
