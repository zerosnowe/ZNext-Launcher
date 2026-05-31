using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ZNext.Services;

internal sealed class FrpcDownloadService
{
	private const string FrpcDownloadsPageUrl = "https://www.mefrp.com/dashboard/downloads";
	private const string FrpcDirectAmd64TemplateUrl = "https://drive.mcsl.com.cn/d/ME-Frp/Lanzou/MEFrp-Core/0.67.0_20260214_7d549bc1/mefrpc_windows_amd64_0.67.0_20260214_7d549bc1.zip";
	private const string FrpcDirectArm64TemplateUrl = "https://drive.mcsl.com.cn/d/ME-Frp/Lanzou/MEFrp-Core/0.67.0_20260214_7d549bc1/mefrpc_windows_arm64_0.67.0_20260214_7d549bc1.zip";

	public async Task<string?> ResolveDownloadUrlAsync(string architecture)
	{
		string templateUrl = string.Equals(architecture, "arm64", StringComparison.OrdinalIgnoreCase)
			? FrpcDirectArm64TemplateUrl
			: FrpcDirectAmd64TemplateUrl;

		string fallbackVersion = ExtractVersionToken(templateUrl) ?? "0.67.0_20260214_7d549bc1";
		string version = await TryGetVersionFromDownloadsPageAsync() ?? fallbackVersion;
		return ApplyVersionToDirectUrl(templateUrl, version);
	}

	private static string? ExtractVersionToken(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}

		Match match = Regex.Match(text, @"v?(?<ver>\d+\.\d+\.\d+_[0-9]{8}_[0-9a-z]{6,})", RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			return null;
		}

		string value = match.Groups["ver"].Value;
		return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
	}

	private static async Task<string?> TryGetVersionFromDownloadsPageAsync()
	{
		try
		{
			using HttpClient client = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(20)
			};
			client.DefaultRequestHeaders.UserAgent.ParseAdd("ZNext-WinUI3-App/1.0");
			string html = await client.GetStringAsync(FrpcDownloadsPageUrl);

			string? version = ExtractVersionToken(html);
			if (!string.IsNullOrWhiteSpace(version))
			{
				return version;
			}

			MatchCollection scripts = Regex.Matches(html, "src\\s*=\\s*['\\\"](?<src>/assets/[^'\\\"]+\\.js)['\\\"]", RegexOptions.IgnoreCase);
			foreach (string scriptUrl in scripts
				.Select(m => m.Groups["src"].Value)
				.Where(v => !string.IsNullOrWhiteSpace(v))
				.Distinct()
				.Take(6))
			{
				string js = await client.GetStringAsync("https://www.mefrp.com" + scriptUrl);
				version = ExtractVersionToken(js);
				if (!string.IsNullOrWhiteSpace(version))
				{
					return version;
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("TryGetVersionFromDownloadsPageAsync failed: " + ex.Message);
		}

		return null;
	}

	private static string ApplyVersionToDirectUrl(string templateUrl, string version)
	{
		if (string.IsNullOrWhiteSpace(templateUrl) || string.IsNullOrWhiteSpace(version))
		{
			return templateUrl;
		}

		return Regex.Replace(templateUrl, @"\d+\.\d+\.\d+_[0-9]{8}_[0-9a-z]{6,}", version, RegexOptions.IgnoreCase);
	}
}
