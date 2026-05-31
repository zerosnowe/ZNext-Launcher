using System;
using System.Reflection;
using Windows.ApplicationModel;

namespace ZNext.Services;

internal sealed class AppVersionService
{
	private readonly Assembly _assembly;

	public AppVersionService(Assembly assembly)
	{
		_assembly = assembly;
	}

	public string GetDisplayVersion()
	{
		if (TryGetPackageVersion(out Version? packageVersion) && packageVersion != null)
		{
			return packageVersion.ToString();
		}

		if (TryGetAssemblyInformationalVersion(out string? informationalVersion) && !string.IsNullOrWhiteSpace(informationalVersion))
		{
			return informationalVersion;
		}

		return _assembly.GetName().Version?.ToString() ?? "-";
	}

	public bool TryGetCurrentVersion(out Version? version)
	{
		if (TryGetPackageVersion(out version) && version != null)
		{
			return true;
		}

		if (TryGetAssemblyInformationalVersion(out string? informationalVersion)
			&& Version.TryParse(informationalVersion, out Version? parsed))
		{
			version = parsed;
			return true;
		}

		version = _assembly.GetName().Version;
		return version != null;
	}

	private static bool TryGetPackageVersion(out Version? version)
	{
		version = null;
		try
		{
			PackageVersion packageVersion = Package.Current.Id.Version;
			version = new Version(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private bool TryGetAssemblyInformationalVersion(out string? version)
	{
		version = null;
		try
		{
			string? informational = _assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
			if (string.IsNullOrWhiteSpace(informational))
			{
				return false;
			}

			int plusIndex = informational.IndexOf('+');
			version = plusIndex > 0 ? informational.Substring(0, plusIndex) : informational;
			return !string.IsNullOrWhiteSpace(version);
		}
		catch
		{
			return false;
		}
	}
}
