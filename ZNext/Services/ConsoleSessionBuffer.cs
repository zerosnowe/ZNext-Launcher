namespace ZNext.Services;

internal sealed class ConsoleSessionBuffer
{
	private const int MaxChars = 120000;

	public ConsoleSessionBufferAppendResult AppendLine(ConsoleSession session, string line, bool captureSnapshotWhenTrimmed)
	{
		bool wasTrimmed = false;
		string? snapshot = null;
		lock (session.BufferLock)
		{
			session.Buffer.AppendLine(line);
			if (session.Buffer.Length > MaxChars)
			{
				session.Buffer.Remove(0, session.Buffer.Length - MaxChars);
				wasTrimmed = true;
			}
			if (wasTrimmed && captureSnapshotWhenTrimmed)
			{
				snapshot = session.Buffer.ToString();
			}
		}

		return new ConsoleSessionBufferAppendResult(wasTrimmed, snapshot);
	}

	public string GetSnapshot(ConsoleSession session)
	{
		lock (session.BufferLock)
		{
			return session.Buffer.ToString();
		}
	}
}

internal readonly record struct ConsoleSessionBufferAppendResult(bool WasTrimmed, string? Snapshot);
