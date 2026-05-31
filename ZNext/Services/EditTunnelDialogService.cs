using System.Diagnostics;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using ZNext.Services.Dialogs;

namespace ZNext.Services;

internal sealed class EditTunnelDialogService
{
	private static readonly SolidColorBrush ErrorBrush = new SolidColorBrush(Color.FromArgb(255, 197, 15, 31));
	private static readonly SolidColorBrush SecondaryTextBrush = new SolidColorBrush(Color.FromArgb(255, 107, 114, 128));
	private readonly TunnelActionService _tunnelActionService;
	private readonly TunnelUpdateRequestBuilder _requestBuilder = new TunnelUpdateRequestBuilder();

	public EditTunnelDialogService(TunnelActionService tunnelActionService)
	{
		_tunnelActionService = tunnelActionService;
	}

	public async Task<EditTunnelDialogResult> ShowAsync(TunnelInfo tunnel, XamlRoot xamlRoot)
	{
		EditTunnelForm form = CreateForm(tunnel);
		ContentDialog dialog = ModernDialogFactory.Create(
			xamlRoot,
			"编辑隧道",
			ModernDialogFactory.Scrollable(form.Root),
			primaryButtonText: "保存",
			closeButtonText: "取消",
			defaultButton: ContentDialogButton.Primary);

		bool updated = false;
		string updateMessage = string.Empty;
		dialog.PrimaryButtonClick += async (_, args) =>
		{
			ContentDialogButtonClickDeferral deferral = args.GetDeferral();
			string originalPrimaryText = dialog.PrimaryButtonText;
			try
			{
				form.HideError();
				TunnelUpdateRequestBuildResult requestResult = _requestBuilder.Build(form.ToRequestInput(tunnel));
				if (!requestResult.Success || requestResult.Request == null)
				{
					form.ShowError(requestResult.ErrorMessage);
					args.Cancel = true;
					return;
				}

				dialog.IsPrimaryButtonEnabled = false;
				dialog.PrimaryButtonText = "保存中...";
				Task<ServiceActionResult> updateTask = _tunnelActionService.UpdateAsync(requestResult.Request);
				if (await Task.WhenAny(updateTask, Task.Delay(TimeSpan.FromSeconds(15))) != updateTask)
				{
					form.ShowError("保存请求超时，请稍后重试");
					args.Cancel = true;
					return;
				}

				ServiceActionResult updateResult = await updateTask;
				if (!updateResult.Success)
				{
					form.ShowError(updateResult.Message);
					args.Cancel = true;
					return;
				}

				updated = true;
				updateMessage = updateResult.Message;
			}
			finally
			{
				if (args.Cancel)
				{
					dialog.IsPrimaryButtonEnabled = true;
					dialog.PrimaryButtonText = originalPrimaryText;
				}

				deferral.Complete();
			}
		};

		try
		{
			ContentDialogResult result = await DialogHost.ShowAsync(dialog);
			return result == ContentDialogResult.Primary && updated
				? EditTunnelDialogResult.Completed(updateMessage)
				: EditTunnelDialogResult.Cancelled();
		}
		catch (Exception ex)
		{
			return EditTunnelDialogResult.FailedToOpen(ex.Message);
		}
	}

	private static EditTunnelForm CreateForm(TunnelInfo tunnel)
	{
		string protocol = (tunnel.proxyType ?? string.Empty).Trim().ToLowerInvariant();
		bool isHttpLike = protocol is "http" or "https";
		TextBox proxyNameBox = new TextBox
		{
			PlaceholderText = "请输入隧道名称",
			Text = tunnel.proxyName ?? string.Empty
		};
		TextBox localAddrBox = new TextBox
		{
			PlaceholderText = "请输入本地地址",
			Text = string.IsNullOrWhiteSpace(tunnel.localIp) ? "127.0.0.1" : tunnel.localIp
		};
		NumberBox localPortBox = new NumberBox
		{
			PlaceholderText = "请输入本地端口",
			Minimum = 1,
			Maximum = 65535,
			SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
			Value = tunnel.localPort <= 0 ? 1 : tunnel.localPort
		};
		ComboBox protocolBox = new ComboBox
		{
			IsEnabled = false
		};
		protocolBox.Items.Add(string.IsNullOrWhiteSpace(protocol) ? "TCP" : protocol.ToUpperInvariant());
		protocolBox.SelectedIndex = 0;
		NumberBox remotePortBox = new NumberBox
		{
			PlaceholderText = "请输入远程端口",
			Minimum = 1,
			Maximum = 65535,
			SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
			Value = tunnel.remotePort <= 0 ? 1 : tunnel.remotePort,
			IsEnabled = !isHttpLike
		};
		TextBox domainBox = new TextBox
		{
			PlaceholderText = "HTTP/HTTPS 可填：a.com,b.com（逗号分隔）",
			Text = string.Join(",", TunnelDetailsFormatter.ParseDomains(tunnel.domain)),
			IsEnabled = isHttpLike
		};
		TextBlock errorText = new TextBlock
		{
			Foreground = ErrorBrush,
			TextWrapping = TextWrapping.Wrap,
			Visibility = Visibility.Collapsed
		};
		StackPanel root = new StackPanel { Spacing = 10 };
		root.Children.Add(new TextBlock
		{
			Text = $"隧道 #{tunnel.Id} · 节点 {tunnel.NodeDisplayText}",
			FontWeight = FontWeights.SemiBold
		});
		root.Children.Add(new TextBlock
		{
			Text = "协议类型不可修改；如需改协议，请删除后重新创建。",
			Foreground = SecondaryTextBrush
		});
		AddLabeledControl(root, "隧道名称 *", proxyNameBox);
		AddLabeledControl(root, "本地地址 *", localAddrBox);
		AddLabeledControl(root, "本地端口 *", localPortBox);
		AddLabeledControl(root, "协议类型", protocolBox);
		AddLabeledControl(root, "远程端口（TCP/UDP 必填）", remotePortBox);
		AddLabeledControl(root, "绑定域名（HTTP/HTTPS 必填）", domainBox);
		root.Children.Add(errorText);
		return new EditTunnelForm(root, proxyNameBox, localAddrBox, localPortBox, remotePortBox, protocol, domainBox, errorText);
	}

	private static void AddLabeledControl(StackPanel root, string label, UIElement control)
	{
		root.Children.Add(new TextBlock { Text = label });
		root.Children.Add(control);
	}
}

internal sealed record EditTunnelDialogResult(bool Updated, string Message, string? ErrorTitle, string? ErrorMessage)
{
	public static EditTunnelDialogResult Completed(string message)
	{
		return new EditTunnelDialogResult(true, message, null, null);
	}

	public static EditTunnelDialogResult Cancelled()
	{
		return new EditTunnelDialogResult(false, string.Empty, null, null);
	}

	public static EditTunnelDialogResult FailedToOpen(string message)
	{
		return new EditTunnelDialogResult(false, string.Empty, "打开失败", message);
	}
}

internal sealed class EditTunnelForm
{
	private readonly TextBox _proxyNameBox;
	private readonly TextBox _localAddrBox;
	private readonly NumberBox _localPortBox;
	private readonly NumberBox _remotePortBox;
	private readonly string _protocol;
	private readonly TextBox _domainBox;
	private readonly TextBlock _errorText;

	public EditTunnelForm(
		StackPanel root,
		TextBox proxyNameBox,
		TextBox localAddrBox,
		NumberBox localPortBox,
		NumberBox remotePortBox,
		string protocol,
		TextBox domainBox,
		TextBlock errorText)
	{
		Root = root;
		_proxyNameBox = proxyNameBox;
		_localAddrBox = localAddrBox;
		_localPortBox = localPortBox;
		_remotePortBox = remotePortBox;
		_protocol = protocol;
		_domainBox = domainBox;
		_errorText = errorText;
	}

	public StackPanel Root { get; }

	public TunnelUpdateRequestInput ToRequestInput(TunnelInfo tunnel)
	{
		return new TunnelUpdateRequestInput(
			tunnel.proxyId,
			tunnel.nodeId,
			_proxyNameBox.Text ?? string.Empty,
			_localAddrBox.Text ?? string.Empty,
			(int)Math.Round(_localPortBox.Value),
			(int)Math.Round(_remotePortBox.Value),
			_protocol,
			_domainBox.Text ?? string.Empty);
	}

	public void ShowError(string message)
	{
		_errorText.Text = message;
		_errorText.Visibility = Visibility.Visible;
	}

	public void HideError()
	{
		_errorText.Visibility = Visibility.Collapsed;
	}
}
