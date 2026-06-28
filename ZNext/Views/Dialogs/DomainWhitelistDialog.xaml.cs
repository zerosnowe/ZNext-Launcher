using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ZNext.Services;

namespace ZNext.Views.Dialogs;

public sealed partial class DomainWhitelistDialog : ContentDialog
{
	public DomainWhitelistDialog()
	{
		InitializeComponent();
	}

	public bool ShouldOpenAddDialog { get; private set; }

	public string DomainCountText => $"已添加 {DomainItems.Count} 个域名";

	public bool HasDomains => DomainItems.Count > 0;

	public bool IsEmpty => DomainItems.Count == 0;

	public List<DomainWhitelistItem> DomainItems { get; } = new();

	internal void SetDomains(IEnumerable<IcpDomainInfo> domains)
	{
		DomainItems.Clear();
		DomainItems.AddRange(domains.Select(d => new DomainWhitelistItem(d.Domain, d.IcpId, d.UnitName, d.NatureName)));
		Bindings.Update();
	}

	private void AddDomainButton_Click(object sender, RoutedEventArgs e)
	{
		ShouldOpenAddDialog = true;
		Hide();
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Hide();
	}
}

public sealed class DomainWhitelistItem
{
	public DomainWhitelistItem(string domain, string icpId, string unitName, string natureName)
	{
		Domain = domain;
		IcpId = icpId;
		UnitName = unitName;
		NatureName = natureName;
	}

	public string Domain { get; }

	public string IcpId { get; }

	public string UnitName { get; }

	public string NatureName { get; }

	public string DetailText => $"{IcpId}  {UnitName}  {NatureName}";
}
