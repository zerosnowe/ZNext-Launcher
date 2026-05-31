using System;
using System.Threading.Tasks;

namespace ZNext.Services;

internal sealed class FrpcSettingsOperationCoordinator
{
	private readonly FrpcSettingsService _settingsService;
	private bool _isRunning;

	public FrpcSettingsOperationCoordinator(FrpcSettingsService settingsService)
	{
		_settingsService = settingsService;
	}

	public async Task<FrpcSettingsOperationOutcome> InstallOrUninstallAsync(
		IProgress<string>? progress,
		Action<bool, bool> operationStateChanged)
	{
		if (_isRunning)
		{
			return FrpcSettingsOperationOutcome.Skipped();
		}

		_isRunning = true;
		bool isInstallFlow = !_settingsService.HasInstalledExecutable();
		operationStateChanged(true, isInstallFlow);

		try
		{
			FrpcOperationResult result = await _settingsService.InstallOrUninstallAsync(progress);
			return FrpcSettingsOperationOutcome.FromOperationResult(result);
		}
		catch (Exception ex)
		{
			return FrpcSettingsOperationOutcome.Failure(isInstallFlow, "安装失败", ex.Message);
		}
		finally
		{
			operationStateChanged(false, isInstallFlow);
			_isRunning = false;
		}
	}
}

internal sealed record FrpcSettingsOperationOutcome(
	bool Started,
	bool Succeeded,
	bool IsInstallFlow,
	string? FailureTitle,
	string? FailureMessage)
{
	public static FrpcSettingsOperationOutcome Skipped()
	{
		return new FrpcSettingsOperationOutcome(false, false, false, null, null);
	}

	public static FrpcSettingsOperationOutcome FromOperationResult(FrpcOperationResult result)
	{
		return new FrpcSettingsOperationOutcome(
			true,
			result.Succeeded,
			result.IsInstallFlow,
			result.FailureTitle,
			result.FailureMessage);
	}

	public static FrpcSettingsOperationOutcome Failure(bool isInstallFlow, string title, string message)
	{
		return new FrpcSettingsOperationOutcome(true, false, isInstallFlow, title, message);
	}
}
