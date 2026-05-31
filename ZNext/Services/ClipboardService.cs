using Windows.ApplicationModel.DataTransfer;

namespace ZNext.Services;

internal sealed class ClipboardService
{
	public void SetText(string text)
	{
		DataPackage package = new();
		package.SetText(text);
		Clipboard.SetContent(package);
		Clipboard.Flush();
	}
}
