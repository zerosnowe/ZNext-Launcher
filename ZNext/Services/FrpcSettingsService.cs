using System;
using System.IO;
using System.Threading.Tasks;

namespace ZNext.Services;

internal sealed class FrpcSettingsService
{
	private readonly FrpcManagerService _frpcManagerService;

	public FrpcSettingsService(FrpcManagerService frpcManagerService)
	{
		_frpcManagerService = frpcManagerService;
	}

	public bool HasInstalledExecutable()
	{
		return _frpcManagerService.GetInstalledExecutablePaths().Length > 0;
	}

	public async Task<FrpcInstallState> GetInstallStateAsync()
	{
		try
		{
			string? frpcPath = _frpcManagerService.GetInstalledExecutablePath();
			if (string.IsNullOrWhiteSpace(frpcPath) || !File.Exists(frpcPath))
			{
				return FrpcInstallState.NotInstalled();
			}

			string version = await _frpcManagerService.GetVersionAsync(frpcPath);
			return FrpcInstallState.Installed(version);
		}
		catch (Exception ex)
		{
			return FrpcInstallState.Error("状态: 检测失败 - " + ex.Message);
		}
	}

	public async Task<FrpcOperationResult> InstallOrUninstallAsync(IProgress<string>? progress = null)
	{
		string[] installedPaths = _frpcManagerService.GetInstalledExecutablePaths();
		if (installedPaths.Length > 0)
		{
			progress?.Report("状态: 正在删除...");
			_frpcManagerService.KillResidualProcesses();
			bool uninstallOk = await _frpcManagerService.UninstallAsync(installedPaths);
			return uninstallOk
				? FrpcOperationResult.Success(isInstallFlow: false)
				: FrpcOperationResult.Failure(isInstallFlow: false, "删除失败", "删除未完成，请确认已同意管理员权限并重试。");
		}

		progress?.Report("状态: 正在解析下载链接...");
		FrpcDownloadResolution download = await _frpcManagerService.ResolveDownloadForCurrentArchitectureAsync();
		if (string.IsNullOrWhiteSpace(download.DownloadUrl))
		{
			return FrpcOperationResult.Failure(isInstallFlow: true, "安装失败", $"未找到适用于 {download.Architecture} 的客户端下载链接。");
		}

		bool installOk = await _frpcManagerService.InstallAsync(download.DownloadUrl);
		return installOk
			? FrpcOperationResult.Success(isInstallFlow: true)
			: FrpcOperationResult.Failure(isInstallFlow: true, "安装失败", "下载或安装未完成，请确认已同意管理员权限并重试。");
	}
}

internal sealed record FrpcInstallState(bool IsInstalled, bool IsError, string StatusText)
{
	public static FrpcInstallState NotInstalled()
	{
		return new FrpcInstallState(false, false, "状态: 未安装");
	}

	public static FrpcInstallState Installed(string version)
	{
		string statusText = string.IsNullOrWhiteSpace(version)
			? "状态: 已安装（无法读取版本）"
			: "状态: 已安装 " + version;

		return new FrpcInstallState(true, false, statusText);
	}

	public static FrpcInstallState Error(string statusText)
	{
		return new FrpcInstallState(false, true, statusText);
	}
}

internal sealed record FrpcOperationResult(bool Succeeded, bool IsInstallFlow, string? FailureTitle, string? FailureMessage)
{
	public static FrpcOperationResult Success(bool isInstallFlow)
	{
		return new FrpcOperationResult(true, isInstallFlow, null, null);
	}

	public static FrpcOperationResult Failure(bool isInstallFlow, string title, string message)
	{
		return new FrpcOperationResult(false, isInstallFlow, title, message);
	}
}
