using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ZNext.Services;

internal sealed class AccountCenterService
{
	private const string ApiBaseUrl = "https://api.mefrp.com/api";
	private readonly IHttpService _httpService;
	private string? _token;

	public AccountCenterService(IHttpService httpService)
	{
		_httpService = httpService;
	}

	public void SetToken(string? token)
	{
		_token = token;
		_httpService.SetAuthToken(token);
	}

	public void ClearToken()
	{
		_token = null;
		_httpService.ClearAuthToken();
	}

	public Task<AccountApiResult<List<CdkUsageLog>>> GetCdkUsageAsync()
	{
		return SendAsync(
			() => _httpService.GetAsync(ApiBaseUrl + "/auth/cdk/usage"),
			data =>
			{
				List<CdkUsageLog> logs = new();
				if (data.ValueKind == JsonValueKind.Object
					&& data.TryGetProperty("logs", out JsonElement logsElement)
					&& logsElement.ValueKind == JsonValueKind.Array)
				{
					foreach (JsonElement item in logsElement.EnumerateArray())
					{
						logs.Add(new CdkUsageLog(
							TryGetInt(item, "logId"),
							TryGetString(item, "code"),
							TryGetString(item, "type"),
							TryGetString(item, "value"),
							TryGetLong(item, "useTime")));
					}
				}

				return logs;
			});
	}

	public Task<AccountApiResult<string>> RedeemCdkAsync(string code, string captchaToken)
	{
		JsonObject payload = new()
		{
			["code"] = code,
			["captchaToken"] = captchaToken
		};
		return SendAsync(
			() => _httpService.PostAsync(ApiBaseUrl + "/auth/cdk/redeem", CreateJsonContent(payload)),
			data =>
			{
				if (data.ValueKind == JsonValueKind.Object)
				{
					string type = TryGetString(data, "type");
					string value = TryGetString(data, "value");
					if (!string.IsNullOrWhiteSpace(type) || !string.IsNullOrWhiteSpace(value))
					{
						return FormatCdkReward(type, value);
					}
				}

				return string.Empty;
			});
	}

	public Task<AccountApiResult<List<IcpDomainInfo>>> GetIcpDomainsAsync()
	{
		return SendAsync(
			() => _httpService.GetAsync(ApiBaseUrl + "/auth/user/icpDomain/list"),
			data =>
			{
				List<IcpDomainInfo> domains = new();
				if (data.ValueKind == JsonValueKind.Array)
				{
					foreach (JsonElement item in data.EnumerateArray())
					{
						domains.Add(new IcpDomainInfo(
							TryGetString(item, "domain"),
							TryGetString(item, "icpId"),
							TryGetString(item, "unitName"),
							TryGetString(item, "natureName")));
					}
				}

				return domains;
			});
	}

	public Task<AccountApiResult<string>> AddIcpDomainAsync(string domain)
	{
		JsonObject payload = new() { ["domain"] = domain };
		return SendAsync(
			() => _httpService.PostAsync(ApiBaseUrl + "/auth/user/icpDomain/add", CreateJsonContent(payload)),
			_ => domain);
	}

	public Task<AccountApiResult<string>> DeleteIcpDomainAsync(string domain)
	{
		JsonObject payload = new() { ["domain"] = domain };
		return SendAsync(
			() => _httpService.PostAsync(ApiBaseUrl + "/auth/user/icpDomain/delete", CreateJsonContent(payload)),
			_ => domain);
	}

	public Task<AccountApiResult<List<AuditLogInfo>>> GetAuditLogsAsync()
	{
		string endTime = FormatAuditTime(DateTimeOffset.Now);
		string startTime = FormatAuditTime(DateTimeOffset.Now.AddDays(-7));
		string url = ApiBaseUrl
			+ "/auth/operationLog/list?page=1&pageSize=20"
			+ "&startTime=" + Uri.EscapeDataString(startTime)
			+ "&endTime=" + Uri.EscapeDataString(endTime);
		return SendAsync(
			() => _httpService.GetAsync(url),
			data =>
			{
				List<AuditLogInfo> logs = new();
				JsonElement listElement = data;
				if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("data", out JsonElement nested))
				{
					listElement = nested;
				}

				if (listElement.ValueKind == JsonValueKind.Array)
				{
					foreach (JsonElement item in listElement.EnumerateArray())
					{
						logs.Add(new AuditLogInfo(
							TryGetInt(item, "logId"),
							TryGetString(item, "category"),
							RedactSensitiveText(TryGetString(item, "details")),
							TryGetString(item, "ipAddress"),
							TryGetString(item, "status"),
							TryGetString(item, "createdAt")));
					}
				}

				return logs;
			});
	}

	public Task<AccountApiResult<string>> KickAllProxiesAsync()
	{
		return SendAsync(
			() => _httpService.GetAsync(ApiBaseUrl + "/auth/user/kickAllProxies"),
			_ => string.Empty);
	}

	public Task<AccountApiResult<string>> ChangePasswordAsync(string oldPassword, string newPassword)
	{
		JsonObject payload = new()
		{
			["oldPassword"] = oldPassword,
			["newPassword"] = newPassword
		};
		return SendAsync(
			() => _httpService.PostAsync(ApiBaseUrl + "/auth/user/passwordReset", CreateJsonContent(payload)),
			_ => string.Empty);
	}

	public Task<AccountApiResult<string>> ResetAccessTokenAsync(string captchaToken)
	{
		JsonObject payload = new() { ["captchaToken"] = captchaToken };
		return SendAsync(
			() => _httpService.PostAsync(ApiBaseUrl + "/auth/user/tokenReset", CreateJsonContent(payload)),
			data =>
			{
				if (data.ValueKind == JsonValueKind.Object)
				{
					return TryGetString(data, "newToken")
						?? TryGetString(data, "token")
						?? string.Empty;
				}

				return data.ValueKind == JsonValueKind.String ? data.GetString() ?? string.Empty : string.Empty;
			});
	}

	public Task<AccountApiResult<string>> GetRobotBindingKeyAsync()
	{
		return SendAsync(
			() => _httpService.GetAsync(ApiBaseUrl + "/auth/user/frpToken"),
			data =>
			{
				if (data.ValueKind == JsonValueKind.Object)
				{
					return TryGetString(data, "token")
						?? TryGetString(data, "frpToken")
						?? TryGetString(data, "accessKey")
						?? string.Empty;
				}

				return data.ValueKind == JsonValueKind.String ? data.GetString() ?? string.Empty : string.Empty;
			});
	}

	private async Task<AccountApiResult<T>> SendAsync<T>(
		Func<Task<HttpResponseMessage>> requestFactory,
		Func<JsonElement, T> dataParser)
	{
		if (string.IsNullOrWhiteSpace(_token))
		{
			return AccountApiResult<T>.Fail("未登录或访问密钥为空。");
		}

		try
		{
			using HttpResponseMessage response = await requestFactory();
			string content = await response.Content.ReadAsStringAsync();
			if (!response.IsSuccessStatusCode)
			{
				return AccountApiResult<T>.Fail($"请求失败: HTTP {(int)response.StatusCode}", content);
			}

			using JsonDocument doc = JsonDocument.Parse(content);
			JsonElement root = doc.RootElement;
			int code = TryGetInt(root, "code");
			string message = TryGetString(root, "message");
			if (code != 0 && code != 200)
			{
				return AccountApiResult<T>.Fail(string.IsNullOrWhiteSpace(message) ? "操作失败" : message, content);
			}

			JsonElement data = root.TryGetProperty("data", out JsonElement dataElement)
				? dataElement
				: default;
			return AccountApiResult<T>.Ok(dataParser(data), string.IsNullOrWhiteSpace(message) ? "操作成功" : message);
		}
		catch (Exception ex)
		{
			return AccountApiResult<T>.Fail("操作异常", ex.Message);
		}
	}

	private static StringContent CreateJsonContent(JsonObject payload)
	{
		return new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
	}

	private static int TryGetInt(JsonElement element, string propertyName)
	{
		if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out JsonElement value))
		{
			return 0;
		}

		return value.ValueKind switch
		{
			JsonValueKind.Number when value.TryGetInt32(out int number) => number,
			JsonValueKind.String when int.TryParse(value.GetString(), out int number) => number,
			_ => 0
		};
	}

	private static long TryGetLong(JsonElement element, string propertyName)
	{
		if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out JsonElement value))
		{
			return 0;
		}

		return value.ValueKind switch
		{
			JsonValueKind.Number when value.TryGetInt64(out long number) => number,
			JsonValueKind.String when long.TryParse(value.GetString(), out long number) => number,
			_ => 0
		};
	}

	private static string TryGetString(JsonElement element, string propertyName)
	{
		if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out JsonElement value))
		{
			return string.Empty;
		}

		return value.ValueKind switch
		{
			JsonValueKind.String => value.GetString() ?? string.Empty,
			JsonValueKind.Number => value.ToString(),
			JsonValueKind.True => "true",
			JsonValueKind.False => "false",
			_ => string.Empty
		};
	}

	private static string FormatCdkReward(string type, string value)
	{
		return type switch
		{
			"traffic" => $"获得流量：{value} GB",
			"proxy" => $"获得隧道数：{value}",
			"vip" => $"获得 VIP：{value} 天",
			_ => string.IsNullOrWhiteSpace(value) ? string.Empty : $"获得奖励：{value}"
		};
	}

	private static string FormatAuditTime(DateTimeOffset dateTime)
	{
		return dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
	}

	private static string RedactSensitiveText(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}

		int index = text.IndexOf("captchaToken:", StringComparison.OrdinalIgnoreCase);
		if (index < 0)
		{
			return text;
		}

		return text[..index] + "captchaToken: [REDACTED]";
	}
}

internal sealed record AccountApiResult<T>(bool Success, string Message, T? Data, string? Error)
{
	public static AccountApiResult<T> Ok(T data, string message)
	{
		return new AccountApiResult<T>(true, message, data, null);
	}

	public static AccountApiResult<T> Fail(string message, string? error = null)
	{
		return new AccountApiResult<T>(false, message, default, error);
	}
}

internal sealed record CdkUsageLog(int LogId, string Code, string Type, string Value, long UseTime);

internal sealed record IcpDomainInfo(string Domain, string IcpId, string UnitName, string NatureName);

internal sealed record AuditLogInfo(int LogId, string Category, string Details, string IpAddress, string Status, string CreatedAt);
