using Microsoft.UI.Xaml.Controls;
using ZNext.Views;

namespace ZNext.Services;

internal sealed class AnnouncementSectionController
{
	private readonly AnnouncementService _announcementService;
	private readonly AnnouncementContentRenderer _renderer;
	private readonly Func<AnnouncementSectionView?> _getView;
	private string _lastMarkdown = string.Empty;
	private bool _hasContentLoaded;

	public AnnouncementSectionController(
		AnnouncementService announcementService,
		AnnouncementContentRenderer renderer,
		Func<AnnouncementSectionView?> getView)
	{
		_announcementService = announcementService;
		_renderer = renderer;
		_getView = getView;
	}

	public async Task LoadAsync()
	{
		try
		{
			SetStatus("正在加载公告...");
			SetPlainText("正在加载公告...");
			string markdownContent = await _announcementService.GetAnnouncementAsync();
			if (string.IsNullOrWhiteSpace(markdownContent) || markdownContent == "NO_ANNOUNCEMENT")
			{
				_lastMarkdown = string.Empty;
				_hasContentLoaded = true;
				SetStatus("暂无公告");
				SetPlainText("当前没有系统公告内容。");
				return;
			}
			if (markdownContent.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
			{
				_lastMarkdown = string.Empty;
				_hasContentLoaded = false;
				SetStatus("公告加载失败");
				SetPlainText(markdownContent);
				return;
			}

			_lastMarkdown = markdownContent;
			_hasContentLoaded = true;
			SetStatus("已更新");
			RenderMarkdown(markdownContent);
		}
		catch (Exception ex)
		{
			_lastMarkdown = string.Empty;
			_hasContentLoaded = false;
			SetStatus("公告加载失败");
			SetPlainText("加载公告失败: " + ex.Message);
		}
	}

	public async Task RefreshIfViewCreatedAsync()
	{
		if (_getView() != null)
		{
			await LoadAsync();
		}
	}

	public void RefreshView()
	{
		if (string.IsNullOrWhiteSpace(_lastMarkdown))
		{
			SetStatus(_hasContentLoaded ? "暂无公告" : "点击右上角公告按钮加载");
			if (_hasContentLoaded)
			{
				SetPlainText("当前没有系统公告内容。");
			}
			return;
		}

		SetStatus("已更新");
		RenderMarkdown(_lastMarkdown);
	}

	public void RenderCachedMarkdown()
	{
		if (_hasContentLoaded && !string.IsNullOrWhiteSpace(_lastMarkdown))
		{
			RenderMarkdown(_lastMarkdown);
		}
	}

	private void SetStatus(string text)
	{
		AnnouncementSectionView? view = _getView();
		if (view?.AnnouncementStatusText != null)
		{
			view.AnnouncementStatusText.Text = text;
		}
	}

	private void SetPlainText(string text)
	{
		RichTextBlock? richTextBlock = _getView()?.AnnouncementRichTextBlock;
		if (richTextBlock != null)
		{
			_renderer.SetPlainText(richTextBlock, text);
		}
	}

	private void RenderMarkdown(string markdown)
	{
		RichTextBlock? richTextBlock = _getView()?.AnnouncementRichTextBlock;
		if (richTextBlock != null)
		{
			_renderer.Render(richTextBlock, markdown, extractFirstHeadingAsTitle: false, out string _);
		}
	}
}
