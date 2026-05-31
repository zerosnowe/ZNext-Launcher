using System;
using System.Text.Json;

namespace ZNext.Services;

internal static class DisplayFormatter
{
	public static string FormatUnixTime(long unixTime)
	{
		if (unixTime <= 0)
		{
			return "-";
		}

		try
		{
			DateTimeOffset time = unixTime > 9999999999L
				? DateTimeOffset.FromUnixTimeMilliseconds(unixTime)
				: DateTimeOffset.FromUnixTimeSeconds(unixTime);

			return time.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
		}
		catch
		{
			return "-";
		}
	}

	public static string FormatTraffic(long traffic)
	{
		if (traffic <= 0)
		{
			return "0 GB";
		}

		double value = traffic;
		string unit = "MB";
		if (value >= 1048576.0)
		{
			value /= 1024.0;
			value /= 1024.0;
			unit = "TB";
		}
		else if (value >= 1024.0)
		{
			value /= 1024.0;
			unit = "GB";
		}

		return $"{value:F2} {unit}";
	}

	public static string FormatBandwidthFromApi(JsonElement value)
	{
		if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
		{
			return "-";
		}

		if (value.ValueKind == JsonValueKind.String)
		{
			string? text = value.GetString()?.Trim();
			return string.IsNullOrWhiteSpace(text) ? "-" : text;
		}

		if (value.ValueKind == JsonValueKind.Object)
		{
			if (value.TryGetProperty("value", out JsonElement numericValue) && value.TryGetProperty("unit", out JsonElement unitValue))
			{
				string? numericText = numericValue.ValueKind == JsonValueKind.Number ? numericValue.ToString() : numericValue.GetString();
				string? unitText = unitValue.GetString();
				if (!string.IsNullOrWhiteSpace(numericText) && !string.IsNullOrWhiteSpace(unitText))
				{
					return numericText + " " + unitText;
				}
			}

			return value.ToString();
		}

		if (value.ValueKind == JsonValueKind.Number)
		{
			if (value.TryGetInt64(out long integerValue))
			{
				return FormatApiBandwidthNumber(integerValue);
			}

			if (value.TryGetDouble(out double doubleValue))
			{
				return FormatApiBandwidthNumber(doubleValue);
			}
		}

		return value.ToString();
	}

	public static string FormatBytesToGb(long bytes)
	{
		double value = (double)bytes / 1073741824.0;
		return $"{value:F2} GB";
	}

	private static string FormatApiBandwidthNumber(double rawValue)
	{
		double mbps = rawValue / 128.0;
		string text = Math.Abs(mbps - Math.Round(mbps)) < 0.01
			? Math.Round(mbps).ToString()
			: mbps.ToString("F2");

		return text + " Mbps";
	}
}
