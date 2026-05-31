using System;
using System.Threading.Tasks;
using ZNext.Infrastructure.Async;

namespace ZNext.Services;

internal sealed class LauncherUpdateOperationCoordinator
{
	private readonly LauncherUpdateCoordinatorService _updateCoordinatorService;
	private readonly AsyncOperationGate _operationGate = new AsyncOperationGate();

	public LauncherUpdateOperationCoordinator(LauncherUpdateCoordinatorService updateCoordinatorService)
	{
		_updateCoordinatorService = updateCoordinatorService;
	}

	public async Task<LauncherUpdateOperationOutcome> CheckAsync(Action<bool> busyStateChanged)
	{
		LauncherUpdateActionResult? result = null;
		bool started = await _operationGate.RunAsync(async () =>
		{
			busyStateChanged(true);
			try
			{
				result = await _updateCoordinatorService.CheckAndOpenLatestAsync();
			}
			finally
			{
				busyStateChanged(false);
			}
		});

		return new LauncherUpdateOperationOutcome(started, result);
	}
}

internal sealed record LauncherUpdateOperationOutcome(bool Started, LauncherUpdateActionResult? Result);
