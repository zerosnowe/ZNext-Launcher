using System;
using System.Security.Cryptography;
using System.Text;
using ZNext.Infrastructure.Settings;

namespace ZNext.Services;

internal sealed class SecurityAccessService
{
	private const string SecurityPasswordEnabledKey = "SecurityPasswordEnabled";
	private const string SecurityPasswordCipherKey = "SecurityPasswordCipher";
	private const string SecurityRsaPublicKeyKey = "SecurityRsaPublicKey";
	private const string SecurityRsaPrivateKeyKey = "SecurityRsaPrivateKey";

	private readonly IAppSettingsStore _settingsStore;

	public SecurityAccessService()
		: this(new AppSettingsStore())
	{
	}

	public SecurityAccessService(IAppSettingsStore settingsStore)
	{
		_settingsStore = settingsStore;
	}

	public bool IsEnabled => _settingsStore.GetBool(SecurityPasswordEnabledKey);

	public bool HasPassword()
	{
		return !string.IsNullOrWhiteSpace(_settingsStore.GetString(SecurityPasswordCipherKey));
	}

	public SecurityAccessState GetEffectiveState()
	{
		bool isEnabled = IsEnabled;
		bool hasPassword = HasPassword();
		if (isEnabled && !hasPassword)
		{
			SetEnabled(false);
			isEnabled = false;
		}

		return new SecurityAccessState(isEnabled, hasPassword);
	}

	public void SetEnabled(bool enabled)
	{
		_settingsStore.SetBool(SecurityPasswordEnabledKey, enabled);
	}

	public SecurityActionResult ApplyEnabledState(bool enabled)
	{
		if (enabled && !HasPassword())
		{
			SetEnabled(false);
			return SecurityActionResult.Failure("请先设置密码后再开启安全访问。");
		}

		SetEnabled(enabled);
		return SecurityActionResult.Success(enabled ? "普通密码已开启" : "普通密码已关闭");
	}

	public void SavePassword(string password)
	{
		string cipherText = EncryptPassword(password);
		_settingsStore.SetString(SecurityPasswordCipherKey, cipherText);
	}

	public SecurityActionResult ValidateNewPassword(string password, string confirm)
	{
		return ValidatePasswordPair(
			password,
			confirm,
			emptyMessage: "密码不能为空",
			lengthMessage: "密码长度至少为 4 位",
			mismatchMessage: "两次输入的密码不一致");
	}

	public SecurityActionResult ValidatePasswordReset(string oldPassword, string newPassword, string confirm)
	{
		if (!VerifyPassword(oldPassword))
		{
			return SecurityActionResult.Failure("原密码错误");
		}

		return ValidatePasswordPair(
			newPassword,
			confirm,
			emptyMessage: "新密码不能为空",
			lengthMessage: "新密码长度至少为 4 位",
			mismatchMessage: "两次输入的新密码不一致");
	}

	public bool VerifyPassword(string input)
	{
		try
		{
			string? plainPassword = DecryptPassword();
			return !string.IsNullOrWhiteSpace(plainPassword) && string.Equals(plainPassword, input, StringComparison.Ordinal);
		}
		catch
		{
			return false;
		}
	}

	public SecurityActionResult TryUnlock(string input)
	{
		return VerifyPassword(input)
			? SecurityActionResult.Success(string.Empty)
			: SecurityActionResult.Failure("密码错误，请重试");
	}

	private static SecurityActionResult ValidatePasswordPair(
		string password,
		string confirm,
		string emptyMessage,
		string lengthMessage,
		string mismatchMessage)
	{
		if (string.IsNullOrWhiteSpace(password))
		{
			return SecurityActionResult.Failure(emptyMessage);
		}

		if (password.Length < 4)
		{
			return SecurityActionResult.Failure(lengthMessage);
		}

		if (!string.Equals(password, confirm, StringComparison.Ordinal))
		{
			return SecurityActionResult.Failure(mismatchMessage);
		}

		return SecurityActionResult.Success(string.Empty);
	}

	private void EnsureRsaKeys(out string publicKey, out string privateKey)
	{
		publicKey = _settingsStore.GetString(SecurityRsaPublicKeyKey) ?? string.Empty;
		privateKey = _settingsStore.GetString(SecurityRsaPrivateKeyKey) ?? string.Empty;
		if (!string.IsNullOrWhiteSpace(publicKey) && !string.IsNullOrWhiteSpace(privateKey))
		{
			return;
		}

		using RSA rsa = RSA.Create(2048);
		publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
		privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
		_settingsStore.SetString(SecurityRsaPublicKeyKey, publicKey);
		_settingsStore.SetString(SecurityRsaPrivateKeyKey, privateKey);
	}

	private string EncryptPassword(string password)
	{
		EnsureRsaKeys(out string publicKey, out _);
		using RSA rsa = RSA.Create();
		rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
		byte[] plain = Encoding.UTF8.GetBytes(password);
		byte[] cipher = rsa.Encrypt(plain, RSAEncryptionPadding.OaepSHA256);
		return Convert.ToBase64String(cipher);
	}

	private string? DecryptPassword()
	{
		string? cipherText = _settingsStore.GetString(SecurityPasswordCipherKey);
		if (string.IsNullOrWhiteSpace(cipherText))
		{
			return null;
		}

		EnsureRsaKeys(out _, out string privateKey);
		using RSA rsa = RSA.Create();
		rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);
		byte[] cipher = Convert.FromBase64String(cipherText);
		byte[] plain = rsa.Decrypt(cipher, RSAEncryptionPadding.OaepSHA256);
		return Encoding.UTF8.GetString(plain);
	}
}

internal sealed record SecurityActionResult(bool Succeeded, string Message)
{
	public static SecurityActionResult Success(string message)
	{
		return new SecurityActionResult(true, message);
	}

	public static SecurityActionResult Failure(string message)
	{
		return new SecurityActionResult(false, message);
	}
}

internal sealed record SecurityAccessState(bool IsEnabled, bool HasPassword);
