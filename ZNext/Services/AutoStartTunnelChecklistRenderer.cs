using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext.Services;

internal sealed class AutoStartTunnelChecklistRenderer
{
	public void RenderMessage(StackPanel? panel, TextBlock? statusText, string message)
	{
		panel?.Children.Clear();

		if (statusText != null)
		{
			statusText.Text = message;
		}
	}

	public void RenderItems(
		StackPanel? panel,
		TextBlock? statusText,
		IReadOnlyList<AutoStartTunnelChecklistItem> items,
		RoutedEventHandler selectionChanged)
	{
		if (panel == null || statusText == null)
		{
			return;
		}

		panel.Children.Clear();
		foreach (AutoStartTunnelChecklistItem item in items)
		{
			CheckBox checkBox = new CheckBox
			{
				Content = item.Label,
				Tag = item.Id,
				IsChecked = item.IsChecked
			};
			checkBox.Checked += selectionChanged;
			checkBox.Unchecked += selectionChanged;
			panel.Children.Add(checkBox);
		}

		statusText.Text = $"已加载 {items.Count} 条隧道。";
	}
}
