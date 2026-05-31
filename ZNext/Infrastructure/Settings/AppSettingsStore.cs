using System.Diagnostics;
using System.Text.Json;
using Windows.Storage;

namespace ZNext.Infrastructure.Settings;

internal sealed class AppSettingsStore : IAppSettingsStore
{
	private static readonly object FileLock = new();
	private static readonly string SettingsFilePath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"ZNext",
		"settings.json");

	private ApplicationDataContainer LocalSettings => ApplicationData.Current.LocalSettings;

	public string? GetString(string key)
	{
		try
		{
			if (LocalSettings.Values[key] is string value)
			{
				return value;
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Read LocalSettings string failed: {key}, {ex.Message}");
		}

		return GetFileValue(key);
	}

	public void SetString(string key, string value)
	{
		try
		{
			LocalSettings.Values[key] = value;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Write LocalSettings string failed: {key}, {ex.Message}");
		}

		SetFileValue(key, value);
	}

	public bool GetBool(string key, bool fallback = false)
	{
		try
		{
			if (LocalSettings.Values.TryGetValue(key, out object? value)
				&& TryConvertBool(value, out bool parsedLocal))
			{
				return parsedLocal;
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Read LocalSettings bool failed: {key}, {ex.Message}");
		}

		string? text = GetFileValue(key);
		return bool.TryParse(text, out bool parsedFile) ? parsedFile : fallback;
	}

	public void SetBool(string key, bool value)
	{
		try
		{
			LocalSettings.Values[key] = value;
		}
		catch
		{
		}

		SetFileValue(key, value.ToString());
	}

	public void Remove(string key)
	{
		try
		{
			LocalSettings.Values.Remove(key);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Remove LocalSettings value failed: {key}, {ex.Message}");
		}

		RemoveFileValue(key);
	}

	private static bool TryConvertBool(object value, out bool result)
	{
		if (value is bool flag)
		{
			result = flag;
			return true;
		}

		if (value is string text && bool.TryParse(text, out bool parsed))
		{
			result = parsed;
			return true;
		}

		result = false;
		return false;
	}

	private static string? GetFileValue(string key)
	{
		lock (FileLock)
		{
			Dictionary<string, string> values = LoadFileValues();
			return values.TryGetValue(key, out string? value) ? value : null;
		}
	}

	private static void SetFileValue(string key, string value)
	{
		lock (FileLock)
		{
			Dictionary<string, string> values = LoadFileValues();
			values[key] = value;
			SaveFileValues(values);
		}
	}

	private static void RemoveFileValue(string key)
	{
		lock (FileLock)
		{
			Dictionary<string, string> values = LoadFileValues();
			if (values.Remove(key))
			{
				SaveFileValues(values);
			}
		}
	}

	private static Dictionary<string, string> LoadFileValues()
	{
		try
		{
			if (!File.Exists(SettingsFilePath))
			{
				return new Dictionary<string, string>(StringComparer.Ordinal);
			}

			string json = File.ReadAllText(SettingsFilePath);
			return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
				?? new Dictionary<string, string>(StringComparer.Ordinal);
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Read file settings failed: " + ex.Message);
			return new Dictionary<string, string>(StringComparer.Ordinal);
		}
	}

	private static void SaveFileValues(Dictionary<string, string> values)
	{
		try
		{
			string? directory = Path.GetDirectoryName(SettingsFilePath);
			if (!string.IsNullOrWhiteSpace(directory))
			{
				Directory.CreateDirectory(directory);
			}

			string json = JsonSerializer.Serialize(values, new JsonSerializerOptions
			{
				WriteIndented = true
			});
			File.WriteAllText(SettingsFilePath, json);
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Write file settings failed: " + ex.Message);
		}
	}
}
