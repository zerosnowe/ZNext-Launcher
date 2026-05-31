using System;
using System.Threading.Tasks;

namespace ZNext.Infrastructure.Async;

internal sealed class AsyncOperationGate
{
	private bool _isRunning;

	public async Task<bool> RunAsync(Func<Task> operation)
	{
		if (_isRunning)
		{
			return false;
		}

		_isRunning = true;
		try
		{
			await operation();
			return true;
		}
		finally
		{
			_isRunning = false;
		}
	}
}
