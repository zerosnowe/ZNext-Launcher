using System;
using System.Diagnostics;
using System.IO;

namespace ZNext.Services;

internal sealed class ExternalLauncherService
{
	public void OpenUrl(string url)
	{
		OpenShellTarget(url);
	}

	public void OpenDirectory(string directory)
	{
		if (string.IsNullOrWhiteSpace(directory))
		{
			throw new ArgumentException("目录不能为空。", nameof(directory));
		}

		if (!Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}

		OpenShellTarget(directory);
	}

	private static void OpenShellTarget(string target)
	{
		if (string.IsNullOrWhiteSpace(target))
		{
			throw new ArgumentException("打开目标不能为空。", nameof(target));
		}

		Process.Start(new ProcessStartInfo
		{
			FileName = target,
			UseShellExecute = true
		});
	}
}
