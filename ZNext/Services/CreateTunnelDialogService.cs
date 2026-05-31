using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using ZNext.Services.Dialogs;
using ZNext.ViewModels;

namespace ZNext.Services;

internal sealed class CreateTunnelDialogService
{
	private static readonly SolidColorBrush ErrorBrush = new SolidColorBrush(Color.FromArgb(255, 197, 15, 31));
	private static readonly SolidColorBrush SecondaryTextBrush = new SolidColorBrush(Color.FromArgb(255, 107, 114, 128));
	private readonly CreateProxyService _createProxyService;
	private readonly UserSessionService _userSessionService;
	private readonly CreateTunnelPortPresetService _portPresetService = new CreateTunnelPortPresetService();
	private readonly CreateTunnelRequestBuilder _requestBuilder = new CreateTunnelRequestBuilder();

	public CreateTunnelDialogService(CreateProxyService createProxyService, UserSessionService userSessionService)
	{
		_createProxyService = createProxyService;
		_userSessionService = userSessionService;
	}

	public async Task<CreateTunnelDialogResult> ShowAsync(
		CreateTunnelNodeCard node,
		XamlRoot xamlRoot,
		int? defaultLocalPort = null,
		string? defaultLocalAddress = null,
		string? defaultProxyName = null)
	{
		CreateTunnelForm form = CreateForm(node, defaultLocalPort, defaultLocalAddress, defaultProxyName);
		ContentDialog dialog = ModernDialogFactory.Create(
			xamlRoot,
			"创建隧道",
			ModernDialogFactory.Scrollable(form.Root),
			primaryButtonText: "创建",
			closeButtonText: "取消",
			defaultButton: ContentDialogButton.Primary);

		bool created = false;
		string createMessage = string.Empty;
		dialog.PrimaryButtonClick += async (_, args) =>
		{
			ContentDialogButtonClickDeferral deferral = args.GetDeferral();
			string originalPrimaryText = dialog.PrimaryButtonText;
			try
			{
				form.HideError();
				CreateTunnelRequestBuildResult requestResult = _requestBuilder.Build(form.ToRequestInput(node));
				if (!requestResult.Success || requestResult.Request == null)
				{
					form.ShowError(requestResult.ErrorMessage);
					args.Cancel = true;
					return;
				}

				dialog.IsPrimaryButtonEnabled = false;
				dialog.PrimaryButtonText = "创建中...";
				_userSessionService.SynchronizeTokens();
				Task<CreateProxyResult> createTask = _createProxyService.CreateProxyAsync(requestResult.Request);
				if (await Task.WhenAny(createTask, Task.Delay(TimeSpan.FromSeconds(15))) != createTask)
				{
					form.ShowError("创建请求超时，请稍后重试");
					args.Cancel = true;
					return;
				}

				CreateProxyResult createResult = await createTask;
				if (!createResult.Success)
				{
					form.ShowError(createResult.Message);
					args.Cancel = true;
					return;
				}

				created = true;
				createMessage = createResult.Message;
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
			return result == ContentDialogResult.Primary && created
				? CreateTunnelDialogResult.Completed(createMessage)
				: CreateTunnelDialogResult.Cancelled();
		}
		catch (Exception ex)
		{
			return CreateTunnelDialogResult.FailedToOpen(ex.Message);
		}
	}

	private CreateTunnelForm CreateForm(
		CreateTunnelNodeCard node,
		int? defaultLocalPort,
		string? defaultLocalAddress,
		string? defaultProxyName)
	{
		TextBox proxyNameBox = new TextBox
		{
			PlaceholderText = "请输入隧道名称",
			Text = string.IsNullOrWhiteSpace(defaultProxyName) ? string.Empty : defaultProxyName.Trim()
		};
		TextBox localAddrBox = new TextBox
		{
			PlaceholderText = "请输入本地地址（如 127.0.0.1）",
			Text = string.IsNullOrWhiteSpace(defaultLocalAddress) ? "127.0.0.1" : defaultLocalAddress.Trim()
		};
		NumberBox localPortBox = new NumberBox
		{
			PlaceholderText = "请输入本地端口",
			Minimum = 1,
			Maximum = 65535,
			SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
		};
		if (defaultLocalPort is >= 1 and <= 65535)
		{
			localPortBox.Value = defaultLocalPort.Value;
		}

		ComboBox localPortPresetBox = new ComboBox
		{
			ItemsSource = _portPresetService.Presets,
			SelectedIndex = 0
		};
		ComboBox protocolBox = CreateProtocolBox(node);
		NumberBox remotePortBox = new NumberBox
		{
			PlaceholderText = node.PortMin == 1 && node.PortMax == 65535
				? "请输入远程端口"
				: $"请输入远程端口（{node.PortMin}-{node.PortMax}）",
			Minimum = node.PortMin,
			Maximum = node.PortMax,
			SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
		};
		Button freePortButton = new Button
		{
			Content = "获取空闲端口",
			MinWidth = 110
		};
		TextBox domainBox = new TextBox
		{
			PlaceholderText = "HTTP/HTTPS 可填：a.com,b.com（逗号分隔）"
		};
		TextBox sslCertPathBox = new TextBox
		{
			PlaceholderText = "HTTPS 可填：SSL 证书路径（如 C:\\certs\\fullchain.pem）"
		};
		TextBox sslKeyPathBox = new TextBox
		{
			PlaceholderText = "HTTPS 可填：SSL 证书密钥路径（如 C:\\certs\\privkey.pem）"
		};
		TextBlock errorText = new TextBlock
		{
			Foreground = ErrorBrush,
			TextWrapping = TextWrapping.Wrap,
			Visibility = Visibility.Collapsed
		};

		TextBlock remotePortLabel = new TextBlock { Text = "远程端口（TCP/UDP 必填）" };
		Grid remotePortGrid = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
				new ColumnDefinition { Width = GridLength.Auto }
			}
		};
		remotePortGrid.Children.Add(remotePortBox);
		remotePortGrid.Children.Add(freePortButton);
		Grid.SetColumn(freePortButton, 1);

		TextBlock domainLabel = new TextBlock { Text = "绑定域名（HTTP/HTTPS 必填）" };
		TextBlock sslCertPathLabel = new TextBlock { Text = "SSL 证书路径（HTTPS必填）" };
		TextBlock sslKeyPathLabel = new TextBlock { Text = "SSL 证书密钥路径（HTTPS必填）" };

		StackPanel root = new StackPanel { Spacing = 10 };
		root.Children.Add(new TextBlock
		{
			Text = $"节点 #{node.NodeId} {node.Name} · {node.RegionText}",
			FontWeight = FontWeights.SemiBold
		});
		root.Children.Add(new TextBlock
		{
			Text = $"支持协议: {node.ProtocolText} · 端口范围: {node.PortMin}-{node.PortMax}",
			Foreground = SecondaryTextBrush
		});
		AddLabeledControl(root, "隧道名称 *", proxyNameBox);
		AddLabeledControl(root, "本地地址 *", localAddrBox);
		root.Children.Add(new TextBlock { Text = "本地端口 *" });
		root.Children.Add(localPortPresetBox);
		root.Children.Add(localPortBox);
		AddLabeledControl(root, "协议类型 *", protocolBox);
		root.Children.Add(remotePortLabel);
		root.Children.Add(remotePortGrid);
		root.Children.Add(domainLabel);
		root.Children.Add(domainBox);
		root.Children.Add(sslCertPathLabel);
		root.Children.Add(sslCertPathBox);
		root.Children.Add(sslKeyPathLabel);
		root.Children.Add(sslKeyPathBox);
		root.Children.Add(errorText);

		CreateTunnelForm form = new CreateTunnelForm(
			root,
			proxyNameBox,
			localAddrBox,
			localPortBox,
			protocolBox,
			remotePortBox,
			domainBox,
			sslCertPathBox,
			sslKeyPathBox,
			errorText,
			remotePortLabel,
			remotePortGrid,
			domainLabel,
			sslCertPathLabel,
			sslCertPathBox,
			sslKeyPathLabel,
			sslKeyPathBox);

		form.UpdateProtocolFields();
		protocolBox.SelectionChanged += (_, _) => form.UpdateProtocolFields();
		localPortPresetBox.SelectionChanged += (_, _) => ApplyLocalPortPreset(localPortPresetBox, localPortBox, protocolBox);
		freePortButton.Click += async (_, _) => await FillFreeRemotePortAsync(node, protocolBox, remotePortBox, form);
		return form;
	}

	private static ComboBox CreateProtocolBox(CreateTunnelNodeCard node)
	{
		ComboBox protocolBox = new ComboBox();
		foreach (string protocol in node.Protocols)
		{
			protocolBox.Items.Add(protocol.ToUpperInvariant());
		}

		protocolBox.SelectedIndex = protocolBox.Items.Count > 0 ? 0 : -1;
		TrySelectProtocol(protocolBox, "TCP");
		return protocolBox;
	}

	private async Task FillFreeRemotePortAsync(
		CreateTunnelNodeCard node,
		ComboBox protocolBox,
		NumberBox remotePortBox,
		CreateTunnelForm form)
	{
		try
		{
			string protocol = (protocolBox.SelectedItem?.ToString() ?? "TCP").ToLowerInvariant();
			_userSessionService.SynchronizeTokens();
			FreeNodePortResult freePortResult = await _createProxyService.GetFreeNodePortAsync(node.NodeId, protocol == "udp" ? "udp" : "tcp");
			if (freePortResult.Success && freePortResult.Port > 0)
			{
				remotePortBox.Value = freePortResult.Port;
				form.HideError();
				return;
			}

			form.ShowError(freePortResult.Message);
		}
		catch (Exception ex)
		{
			form.ShowError("获取空闲端口失败: " + ex.Message);
		}
	}

	private void ApplyLocalPortPreset(ComboBox localPortPresetBox, NumberBox localPortBox, ComboBox protocolBox)
	{
		string presetText = localPortPresetBox.SelectedItem?.ToString() ?? string.Empty;
		CreateTunnelPortPresetResult preset = _portPresetService.Resolve(presetText);
		if (preset.Port.HasValue)
		{
			localPortBox.Value = preset.Port.Value;
		}

		if (!string.IsNullOrWhiteSpace(preset.PreferredProtocol))
		{
			TrySelectProtocol(protocolBox, preset.PreferredProtocol);
		}
	}

	private static void TrySelectProtocol(ComboBox protocolBox, string protocol)
	{
		for (int i = 0; i < protocolBox.Items.Count; i++)
		{
			if (string.Equals(protocolBox.Items[i]?.ToString(), protocol, StringComparison.OrdinalIgnoreCase))
			{
				protocolBox.SelectedIndex = i;
				return;
			}
		}
	}

	private static void AddLabeledControl(StackPanel root, string label, UIElement control)
	{
		root.Children.Add(new TextBlock { Text = label });
		root.Children.Add(control);
	}
}

internal sealed record CreateTunnelDialogResult(bool Created, string Message, string? ErrorTitle, string? ErrorMessage)
{
	public static CreateTunnelDialogResult Completed(string message)
	{
		return new CreateTunnelDialogResult(true, message, null, null);
	}

	public static CreateTunnelDialogResult Cancelled()
	{
		return new CreateTunnelDialogResult(false, string.Empty, null, null);
	}

	public static CreateTunnelDialogResult FailedToOpen(string message)
	{
		return new CreateTunnelDialogResult(false, string.Empty, "打开失败", message);
	}
}

internal sealed class CreateTunnelForm
{
	private readonly TextBox _proxyNameBox;
	private readonly TextBox _localAddrBox;
	private readonly NumberBox _localPortBox;
	private readonly ComboBox _protocolBox;
	private readonly NumberBox _remotePortBox;
	private readonly TextBox _domainBox;
	private readonly TextBox _sslCertPathBox;
	private readonly TextBox _sslKeyPathBox;
	private readonly TextBlock _errorText;
	private readonly TextBlock _remotePortLabel;
	private readonly Grid _remotePortGrid;
	private readonly TextBlock _domainLabel;
	private readonly TextBlock _sslCertPathLabel;
	private readonly TextBox _sslCertPathInput;
	private readonly TextBlock _sslKeyPathLabel;
	private readonly TextBox _sslKeyPathInput;

	public CreateTunnelForm(
		StackPanel root,
		TextBox proxyNameBox,
		TextBox localAddrBox,
		NumberBox localPortBox,
		ComboBox protocolBox,
		NumberBox remotePortBox,
		TextBox domainBox,
		TextBox sslCertPathBox,
		TextBox sslKeyPathBox,
		TextBlock errorText,
		TextBlock remotePortLabel,
		Grid remotePortGrid,
		TextBlock domainLabel,
		TextBlock sslCertPathLabel,
		TextBox sslCertPathInput,
		TextBlock sslKeyPathLabel,
		TextBox sslKeyPathInput)
	{
		Root = root;
		_proxyNameBox = proxyNameBox;
		_localAddrBox = localAddrBox;
		_localPortBox = localPortBox;
		_protocolBox = protocolBox;
		_remotePortBox = remotePortBox;
		_domainBox = domainBox;
		_sslCertPathBox = sslCertPathBox;
		_sslKeyPathBox = sslKeyPathBox;
		_errorText = errorText;
		_remotePortLabel = remotePortLabel;
		_remotePortGrid = remotePortGrid;
		_domainLabel = domainLabel;
		_sslCertPathLabel = sslCertPathLabel;
		_sslCertPathInput = sslCertPathInput;
		_sslKeyPathLabel = sslKeyPathLabel;
		_sslKeyPathInput = sslKeyPathInput;
	}

	public StackPanel Root { get; }

	public CreateTunnelRequestInput ToRequestInput(CreateTunnelNodeCard node)
	{
		return new CreateTunnelRequestInput(
			node.NodeId,
			node.PortMin,
			node.PortMax,
			_proxyNameBox.Text ?? string.Empty,
			_localAddrBox.Text ?? string.Empty,
			(int)Math.Round(_localPortBox.Value),
			(int)Math.Round(_remotePortBox.Value),
			_protocolBox.SelectedItem?.ToString() ?? string.Empty,
			_domainBox.Text ?? string.Empty,
			_sslCertPathBox.Text ?? string.Empty,
			_sslKeyPathBox.Text ?? string.Empty);
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

	public void UpdateProtocolFields()
	{
		string protocol = (_protocolBox.SelectedItem?.ToString() ?? string.Empty).Trim().ToLowerInvariant();
		bool isHttpLike = protocol is "http" or "https";
		bool isHttps = protocol == "https";
		_remotePortLabel.Visibility = isHttpLike ? Visibility.Collapsed : Visibility.Visible;
		_remotePortGrid.Visibility = isHttpLike ? Visibility.Collapsed : Visibility.Visible;
		_domainLabel.Visibility = isHttpLike ? Visibility.Visible : Visibility.Collapsed;
		_domainBox.Visibility = isHttpLike ? Visibility.Visible : Visibility.Collapsed;
		_sslCertPathLabel.Visibility = isHttps ? Visibility.Visible : Visibility.Collapsed;
		_sslCertPathInput.Visibility = isHttps ? Visibility.Visible : Visibility.Collapsed;
		_sslKeyPathLabel.Visibility = isHttps ? Visibility.Visible : Visibility.Collapsed;
		_sslKeyPathInput.Visibility = isHttps ? Visibility.Visible : Visibility.Collapsed;
	}
}
