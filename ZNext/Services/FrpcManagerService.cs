using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using IOPath = System.IO.Path;

namespace ZNext.Services;

internal sealed class FrpcManagerService
{
	private readonly FrpcDownloadService _downloadService;

	public FrpcManagerService()
		: this(new FrpcDownloadService())
	{
	}

	public FrpcManagerService(FrpcDownloadService downloadService)
	{
		_downloadService = downloadService;
	}

	public string GetArchitectureKeyword()
	{
		return RuntimeInformation.OSArchitecture switch
		{
			Architecture.Arm64 => "arm64",
			Architecture.X64 => "amd64",
			_ => "amd64"
		};
	}

	public string GetApplicationRootDirectory()
	{
		try
		{
			string? processPath = Environment.ProcessPath;
			if (!string.IsNullOrWhiteSpace(processPath))
			{
				string? processDir = IOPath.GetDirectoryName(processPath);
				if (!string.IsNullOrWhiteSpace(processDir))
				{
					return processDir;
				}
			}
		}
		catch
		{
		}

		return AppContext.BaseDirectory;
	}

	public string GetExecutablePath()
	{
		return IOPath.Combine(GetApplicationRootDirectory(), "mefrpc.exe");
	}

	public string[] GetInstalledExecutablePaths()
	{
		string root = GetApplicationRootDirectory();
		string[] candidates =
		{
			IOPath.Combine(root, "mefrpc.exe"),
			IOPath.Combine(root, "frpc.exe")
		};

		return candidates.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
	}

	public string? GetInstalledExecutablePath()
	{
		return GetInstalledExecutablePaths().FirstOrDefault();
	}

	public Task<string> GetVersionAsync(string exePath)
	{
		return Task.Run(() => GetVersion(exePath));
	}

	public async Task<FrpcDownloadResolution> ResolveDownloadForCurrentArchitectureAsync()
	{
		string architecture = GetArchitectureKeyword();
		string? downloadUrl = await _downloadService.ResolveDownloadUrlAsync(architecture);
		return new FrpcDownloadResolution(architecture, downloadUrl);
	}

	public Task<bool> InstallAsync(string downloadUrl)
	{
		return RunElevatedFrpcInstallAsync(downloadUrl, GetExecutablePath());
	}

	public Task<bool> UninstallAsync(IReadOnlyList<string>? targetPaths = null)
	{
		return RunElevatedFrpcUninstallAsync(targetPaths ?? GetInstalledExecutablePaths());
	}

	public void KillResidualProcesses()
	{
		try
		{
			Process[] processesByName = Process.GetProcessesByName("mefrpc");
			foreach (Process process in processesByName)
			{
				try
				{
					if (!process.HasExited)
					{
						process.Kill(entireProcessTree: true);
					}
				}
				catch
				{
				}
				finally
				{
					process.Dispose();
				}
			}
		}
		catch
		{
		}
	}

	private static string GetVersion(string exePath)
	{
		try
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = exePath,
				Arguments = "-v",
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			using Process? process = Process.Start(startInfo);
			if (process == null)
			{
				return string.Empty;
			}

			string output = process.StandardOutput.ReadToEnd();
			string error = process.StandardError.ReadToEnd();
			process.WaitForExit(5000);
			string version = (output + " " + error).Trim();
			return version.Replace("\r", " ").Replace("\n", " ").Trim();
		}
		catch
		{
			return string.Empty;
		}
	}

	private static async Task<bool> RunElevatedFrpcInstallAsync(string downloadUrl, string targetPath)
	{
		string script = @"
param([string]$DownloadUrl,[string]$TargetPath)
$ErrorActionPreference = 'Stop'
$tmpDir = Join-Path $env:TEMP ('znext-frpc-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tmpDir -Force | Out-Null
try {
  $fileName = [System.IO.Path]::GetFileName(([Uri]$DownloadUrl).AbsolutePath)
  if ([string]::IsNullOrWhiteSpace($fileName)) { $fileName = 'mefrpc.zip' }
  $downloadPath = Join-Path $tmpDir $fileName
  Invoke-WebRequest -Uri $DownloadUrl -OutFile $downloadPath -UseBasicParsing
  $lower = $downloadPath.ToLowerInvariant()
  if ($lower.EndsWith('.exe')) {
    Copy-Item -Path $downloadPath -Destination $TargetPath -Force
  } elseif ($lower.EndsWith('.zip')) {
    $extractDir = Join-Path $tmpDir 'extract'
    Expand-Archive -Path $downloadPath -DestinationPath $extractDir -Force
    $frpc = Get-ChildItem -Path $extractDir -Recurse -Include 'mefrpc.exe','frpc.exe' -File | Select-Object -First 1
    if ($null -eq $frpc) { throw '压缩包中未找到 mefrpc.exe/frpc.exe' }
    Copy-Item -Path $frpc.FullName -Destination $TargetPath -Force
  } else {
    throw ('不支持的下载格式: ' + $downloadPath)
  }
} finally {
  if (Test-Path $tmpDir) { Remove-Item -Path $tmpDir -Recurse -Force -ErrorAction SilentlyContinue }
}";
		return await RunElevatedPowerShellScriptAsync(script, new Dictionary<string, string>
		{
			["DownloadUrl"] = downloadUrl,
			["TargetPath"] = targetPath
		});
	}

	private static async Task<bool> RunElevatedFrpcUninstallAsync(IReadOnlyList<string> targetPaths)
	{
		string script = @"
param([string]$TargetPathList)
$ErrorActionPreference = 'Stop'
Get-Process -Name 'mefrpc','frpc' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
$paths = $TargetPathList -split '\|'
foreach ($p in $paths) {
  if (-not [string]::IsNullOrWhiteSpace($p) -and (Test-Path $p)) {
    Remove-Item -Path $p -Force -ErrorAction Stop
  }
}";
		return await RunElevatedPowerShellScriptAsync(script, new Dictionary<string, string>
		{
			["TargetPathList"] = string.Join("|", targetPaths ?? Array.Empty<string>())
		});
	}

	private static async Task<bool> RunElevatedPowerShellScriptAsync(string script, IReadOnlyDictionary<string, string> parameters)
	{
		string tempScript = IOPath.Combine(IOPath.GetTempPath(), "znext-admin-" + Guid.NewGuid().ToString("N") + ".ps1");
		await File.WriteAllTextAsync(tempScript, script, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		try
		{
			StringBuilder argsBuilder = new StringBuilder();
			argsBuilder.Append("-NoProfile -ExecutionPolicy Bypass -File ");
			argsBuilder.Append('"').Append(tempScript).Append('"');
			foreach (KeyValuePair<string, string> kv in parameters)
			{
				argsBuilder.Append(' ').Append('-').Append(kv.Key).Append(' ');
				argsBuilder.Append('"').Append((kv.Value ?? string.Empty).Replace("\"", "`\"")).Append('"');
			}

			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe"),
				Arguments = argsBuilder.ToString(),
				UseShellExecute = true,
				Verb = "runas",
				CreateNoWindow = false
			};

			Process? process = null;
			try
			{
				process = Process.Start(startInfo);
			}
			catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
			{
				return false;
			}

			if (process == null)
			{
				return false;
			}

			await Task.Run(() => process.WaitForExit());
			return process.ExitCode == 0;
		}
		finally
		{
			try
			{
				if (File.Exists(tempScript))
				{
					File.Delete(tempScript);
				}
			}
			catch
			{
			}
		}
	}
}

internal sealed record FrpcDownloadResolution(string Architecture, string? DownloadUrl);
