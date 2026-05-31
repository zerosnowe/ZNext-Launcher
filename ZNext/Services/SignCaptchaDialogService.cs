using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ZNext.Views.Dialogs;

namespace ZNext.Services;

internal sealed class SignCaptchaDialogService
{
	public async Task<string?> ShowAsync(XamlRoot? xamlRoot)
	{
		if (xamlRoot == null)
		{
			return null;
		}

		SignCaptchaDialog dialog = new SignCaptchaDialog
		{
			XamlRoot = xamlRoot
		};

		return await DialogHost.ShowAsync(dialog) == ContentDialogResult.Primary
			? dialog.Token
			: null;
	}
}
