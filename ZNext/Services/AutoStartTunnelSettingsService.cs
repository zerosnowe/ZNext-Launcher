using System;
using System.Collections.Generic;
using System.Linq;
using ZNext.Infrastructure.Settings;

namespace ZNext.Services;

internal sealed class AutoStartTunnelSettingsService
{
	private const string AutoStartTunnelsEnabledKey = "AutoStartTunnelsEnabled";
	private const string AutoStartTunnelIdsKey = "AutoStartTunnelIds";

	private readonly IAppSettingsStore _settingsStore;

	public AutoStartTunnelSettingsService()
		: this(new AppSettingsStore())
	{
	}

	public AutoStartTunnelSettingsService(IAppSettingsStore settingsStore)
	{
		_settingsStore = settingsStore;
	}

	public bool IsEnabled => _settingsStore.GetBool(AutoStartTunnelsEnabledKey);

	public void SetEnabled(bool enabled)
	{
		_settingsStore.SetBool(AutoStartTunnelsEnabledKey, enabled);
	}

	public HashSet<int> LoadTunnelIds()
	{
		HashSet<int> result = new HashSet<int>();
		string raw = _settingsStore.GetString(AutoStartTunnelIdsKey) ?? string.Empty;
		foreach (string part in raw.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries))
		{
			if (int.TryParse(part.Trim(), out int id) && id > 0)
			{
				result.Add(id);
			}
		}

		return result;
	}

	public void SaveTunnelIds(IEnumerable<int> values)
	{
		string text = string.Join(",", values.Where(v => v > 0).Distinct().OrderBy(v => v));
		_settingsStore.SetString(AutoStartTunnelIdsKey, text);
	}

	public IReadOnlyList<AutoStartTunnelChecklistItem> BuildChecklistItems(
		IEnumerable<TunnelInfo> tunnels,
		IReadOnlySet<int> selectedTunnelIds)
	{
		return tunnels
			.OrderBy(tunnel => string.IsNullOrWhiteSpace(tunnel.Name) ? $"#{tunnel.Id}" : tunnel.Name, StringComparer.OrdinalIgnoreCase)
			.Select(tunnel => new AutoStartTunnelChecklistItem(
				tunnel.Id,
				string.IsNullOrWhiteSpace(tunnel.Name) ? $"隧道 #{tunnel.Id}" : $"{tunnel.Name} (#{tunnel.Id})",
				selectedTunnelIds.Contains(tunnel.Id)))
			.ToList();
	}

	public IReadOnlyList<TunnelInfo> GetStartupTargets(
		IEnumerable<TunnelInfo> tunnels,
		IReadOnlySet<int> selectedTunnelIds)
	{
		return tunnels
			.Where(tunnel => selectedTunnelIds.Contains(tunnel.Id))
			.Where(tunnel => !tunnel.IsDisabledResolved)
			.ToList();
	}

	public void UpdateTunnelSelection(ISet<int> selectedTunnelIds, int tunnelId, bool isSelected)
	{
		if (isSelected)
		{
			selectedTunnelIds.Add(tunnelId);
		}
		else
		{
			selectedTunnelIds.Remove(tunnelId);
		}

		SaveTunnelIds(selectedTunnelIds);
	}
}

internal sealed record AutoStartTunnelChecklistItem(int Id, string Label, bool IsChecked);
