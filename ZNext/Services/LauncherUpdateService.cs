using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ZNext.Services;

internal sealed class LauncherUpdateService
{
	private const string LauncherUpdateApiBaseUrl = "https://alist.yealqp.cn";
	private const string LauncherUpdateListPath = "/ZNext Launcher/x64";

	public async Task<LauncherUpdateResult> GetLatestLauncherZipUrlAsync()
	{
		try
		{
			using HttpClient client = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(20)
			};

			string listPayload = JsonSerializer.Serialize(new
			{
				path = LauncherUpdateListPath,
				password = string.Empty,
				page = 1,
				per_page = 200,
				refresh = false
			});

			using StringContent listContent = new StringContent(listPayload, Encoding.UTF8, "application/json");
			using HttpResponseMessage listResponse = await client.PostAsync(LauncherUpdateApiBaseUrl + "/api/fs/list", listContent);
			string listBody = await listResponse.Content.ReadAsStringAsync();
			if (!listResponse.IsSuccessStatusCode)
			{
				return LauncherUpdateResult.Failed($"请求文件列表失败: HTTP {(int)listResponse.StatusCode}");
			}

			using JsonDocument listDoc = JsonDocument.Parse(listBody);
			JsonElement listRoot = listDoc.RootElement;
			int code = TryGetInt(listRoot, "code");
			if (code != 200)
			{
				return LauncherUpdateResult.Failed(TryGetString(listRoot, "message") ?? "文件列表获取失败");
			}

			if (!TryGetPropertyIgnoreCase(listRoot, "data", out JsonElement dataElement)
				|| !TryGetPropertyIgnoreCase(dataElement, "content", out JsonElement contentElement)
				|| contentElement.ValueKind != JsonValueKind.Array)
			{
				return LauncherUpdateResult.Failed("返回数据缺少文件列表");
			}

			JsonElement? bestFile = null;
			Version bestVersion = new Version(0, 0, 0, 0);
			DateTimeOffset bestModified = DateTimeOffset.MinValue;

			foreach (JsonElement item in contentElement.EnumerateArray())
			{
				string name = TryGetString(item, "name") ?? string.Empty;
				if (!name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				if (TryGetPropertyIgnoreCase(item, "is_dir", out JsonElement isDirElement)
					&& isDirElement.ValueKind == JsonValueKind.True)
				{
					continue;
				}

				Version version = ExtractVersionFromFileName(name) ?? new Version(0, 0, 0, 0);
				DateTimeOffset modified = DateTimeOffset.MinValue;
				string modifiedText = TryGetString(item, "modified") ?? string.Empty;
				if (!string.IsNullOrWhiteSpace(modifiedText))
				{
					DateTimeOffset.TryParse(modifiedText, out modified);
				}

				bool shouldReplace = version > bestVersion
					|| (version == bestVersion && modified > bestModified);

				if (shouldReplace)
				{
					bestVersion = version;
					bestModified = modified;
					bestFile = item;
				}
			}

			if (!bestFile.HasValue)
			{
				return LauncherUpdateResult.Failed("未找到可用的 zip 更新包");
			}

			string fileName = TryGetString(bestFile.Value, "name") ?? string.Empty;
			string filePath = LauncherUpdateListPath.TrimEnd('/') + "/" + fileName;

			string getPayload = JsonSerializer.Serialize(new
			{
				path = filePath,
				password = string.Empty
			});

			using StringContent getContent = new StringContent(getPayload, Encoding.UTF8, "application/json");
			using HttpResponseMessage getResponse = await client.PostAsync(LauncherUpdateApiBaseUrl + "/api/fs/get", getContent);
			string getBody = await getResponse.Content.ReadAsStringAsync();
			if (!getResponse.IsSuccessStatusCode)
			{
				return LauncherUpdateResult.Failed($"获取下载链接失败: HTTP {(int)getResponse.StatusCode}", bestVersion);
			}

			using JsonDocument getDoc = JsonDocument.Parse(getBody);
			JsonElement getRoot = getDoc.RootElement;
			if (TryGetInt(getRoot, "code") != 200)
			{
				return LauncherUpdateResult.Failed(TryGetString(getRoot, "message") ?? "获取下载链接失败", bestVersion);
			}

			if (!TryGetPropertyIgnoreCase(getRoot, "data", out JsonElement getData)
				|| !TryGetPropertyIgnoreCase(getData, "raw_url", out JsonElement rawUrlElement))
			{
				return LauncherUpdateResult.Failed("返回数据中缺少 raw_url", bestVersion);
			}

			string? rawUrl = rawUrlElement.ValueKind == JsonValueKind.String ? rawUrlElement.GetString() : null;
			if (string.IsNullOrWhiteSpace(rawUrl))
			{
				return LauncherUpdateResult.Failed("下载链接为空", bestVersion);
			}

			return new LauncherUpdateResult(true, rawUrl, bestVersion, "success");
		}
		catch (Exception ex)
		{
			return LauncherUpdateResult.Failed("获取更新异常: " + ex.Message);
		}
	}

	private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
	{
		foreach (JsonProperty property in element.EnumerateObject())
		{
			if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
			{
				value = property.Value;
				return true;
			}
		}

		value = default;
		return false;
	}

	private static Version? ExtractVersionFromFileName(string fileName)
	{
		if (string.IsNullOrWhiteSpace(fileName))
		{
			return null;
		}

		Match match = Regex.Match(fileName, @"(\d+\.\d+\.\d+\.\d+)");
		if (!match.Success)
		{
			return null;
		}

		return Version.TryParse(match.Groups[1].Value, out Version? version) ? version : null;
	}

	private static int TryGetInt(JsonElement root, string name)
	{
		if (!TryGetPropertyIgnoreCase(root, name, out JsonElement element))
		{
			return 0;
		}

		if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int number))
		{
			return number;
		}

		if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out number))
		{
			return number;
		}

		return 0;
	}

	private static string? TryGetString(JsonElement root, string name)
	{
		return TryGetPropertyIgnoreCase(root, name, out JsonElement element) && element.ValueKind == JsonValueKind.String
			? element.GetString()
			: null;
	}
}

internal sealed record LauncherUpdateResult(bool Success, string? Url, Version? LatestVersion, string Message)
{
	public static LauncherUpdateResult Failed(string message, Version? latestVersion = null)
	{
		return new LauncherUpdateResult(false, null, latestVersion, message);
	}
}
