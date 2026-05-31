using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext.Infrastructure.Binding;

public static class PasswordBoxBinding
{
	public static readonly DependencyProperty PasswordProperty =
		DependencyProperty.RegisterAttached(
			"Password",
			typeof(string),
			typeof(PasswordBoxBinding),
			new PropertyMetadata(string.Empty, OnPasswordChanged));

	public static string GetPassword(DependencyObject obj)
	{
		return (string)obj.GetValue(PasswordProperty);
	}

	public static void SetPassword(DependencyObject obj, string value)
	{
		obj.SetValue(PasswordProperty, value);
	}

	private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not PasswordBox passwordBox)
		{
			return;
		}

		passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
		string newPassword = e.NewValue as string ?? string.Empty;
		if (passwordBox.Password != newPassword)
		{
			passwordBox.Password = newPassword;
		}

		passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
	}

	private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
	{
		if (sender is PasswordBox passwordBox)
		{
			SetPassword(passwordBox, passwordBox.Password ?? string.Empty);
		}
	}
}
