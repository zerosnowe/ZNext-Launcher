using System;
using System.Text;

namespace ZNext.Services;

internal static class CaptchaVerificationBridge
{
	public const string LoginClient = "ZNextWinUI3App";
	public const string SignInClient = "ZNextSignIn";

	public static Uri CreateCaptchaUri(string client)
	{
		string escapedClient = Uri.EscapeDataString(client);
		long nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		return new Uri($"https://www.mefrp.com/3rdparty/captcha?client={escapedClient}&_={nonce}");
	}

	public static string? DecodeToken(string? tokenText)
	{
		if (string.IsNullOrWhiteSpace(tokenText))
		{
			return null;
		}

		string token = CleanCandidate(tokenText);
		string? decodedToken = TryDecodeBase64(token);
		if (!string.IsNullOrWhiteSpace(decodedToken))
		{
			token = decodedToken.Trim();
		}

		string[] parts = token.Split(new[] { "||" }, StringSplitOptions.None);
		string normalized = parts.Length > 0 ? parts[0].Trim() : token;
		return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
	}

	private static string CleanCandidate(string value)
	{
		string cleaned = value.Trim().Trim('"', '\'', '`', '<', '>', '[', ']', '(', ')', '{', '}', ',', ';');
		cleaned = cleaned.Replace("&amp;", "&", StringComparison.OrdinalIgnoreCase);

		try
		{
			return Uri.UnescapeDataString(cleaned);
		}
		catch
		{
			return cleaned;
		}
	}

	private static string? TryDecodeBase64(string token)
	{
		string normalized = token.Trim().Replace('-', '+').Replace('_', '/');
		int padding = normalized.Length % 4;
		if (padding > 0)
		{
			normalized = normalized.PadRight(normalized.Length + 4 - padding, '=');
		}

		try
		{
			byte[] bytes = Convert.FromBase64String(normalized);
			string decoded = Encoding.UTF8.GetString(bytes).Trim();
			return string.IsNullOrWhiteSpace(decoded) ? null : decoded;
		}
		catch
		{
			return null;
		}
	}
}
