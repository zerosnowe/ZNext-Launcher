using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ZNext.Services;

internal sealed class ConsoleOutputRenderer
{
	private readonly RichTextBlock _output;
	private readonly ScrollViewer _scrollViewer;
	private readonly SolidColorBrush _infoBrush = new SolidColorBrush(ColorFromHex("#374151"));
	private readonly SolidColorBrush _errorBrush = new SolidColorBrush(ColorFromHex("#DC2626"));
	private readonly SolidColorBrush _successBrush = new SolidColorBrush(ColorFromHex("#059669"));
	private readonly SolidColorBrush _warningBrush = new SolidColorBrush(ColorFromHex("#D97706"));
	private readonly SolidColorBrush _promptBrush = new SolidColorBrush(ColorFromHex("#2563EB"));
	private Paragraph? _activeParagraph;

	public ConsoleOutputRenderer(RichTextBlock output, ScrollViewer scrollViewer)
	{
		_output = output;
		_scrollViewer = scrollViewer;
	}

	public void Render(string text)
	{
		_output.Blocks.Clear();
		Paragraph paragraph = new Paragraph();
		_activeParagraph = paragraph;
		string normalized = text?.Replace("\r\n", "\n") ?? string.Empty;
		string[] lines = normalized.Split('\n');
		for (int i = 0; i < lines.Length; i++)
		{
			string line = lines[i];
			paragraph.Inlines.Add(new Run
			{
				Text = i == lines.Length - 1 ? line : line + "\n",
				Foreground = GetLineBrush(line)
			});
		}

		_output.Blocks.Add(paragraph);
		ScrollToEnd();
	}

	public void AppendLine(string line, string fallbackText)
	{
		if (_activeParagraph == null || _output.Blocks.Count == 0)
		{
			Render(fallbackText);
			return;
		}

		_activeParagraph.Inlines.Add(new Run
		{
			Text = line + "\n",
			Foreground = GetLineBrush(line)
		});
		ScrollToEnd();
	}

	private void ScrollToEnd()
	{
		_scrollViewer.ChangeView(
			horizontalOffset: null,
			verticalOffset: double.MaxValue,
			zoomFactor: null,
			disableAnimation: true);
	}

	private Brush GetLineBrush(string line)
	{
		if (string.IsNullOrWhiteSpace(line))
		{
			return _infoBrush;
		}

		string text = line.Trim();
		if (text.StartsWith("[ERR]", StringComparison.OrdinalIgnoreCase)
			|| text.Contains("失败", StringComparison.OrdinalIgnoreCase)
			|| text.Contains("异常", StringComparison.OrdinalIgnoreCase))
		{
			return _errorBrush;
		}

		if (text.Contains("成功", StringComparison.OrdinalIgnoreCase)
			|| text.Contains("已启动", StringComparison.OrdinalIgnoreCase)
			|| text.Contains("已在运行", StringComparison.OrdinalIgnoreCase))
		{
			return _successBrush;
		}

		if (text.Contains("重连", StringComparison.OrdinalIgnoreCase)
			|| text.Contains("提示", StringComparison.OrdinalIgnoreCase)
			|| text.Contains("回退", StringComparison.OrdinalIgnoreCase))
		{
			return _warningBrush;
		}

		if (text.StartsWith("PS ", StringComparison.OrdinalIgnoreCase))
		{
			return _promptBrush;
		}

		return _infoBrush;
	}

	private static Color ColorFromHex(string hex)
	{
		string value = hex.TrimStart('#');
		byte r = Convert.ToByte(value.Substring(0, 2), 16);
		byte g = Convert.ToByte(value.Substring(2, 2), 16);
		byte b = Convert.ToByte(value.Substring(4, 2), 16);
		return Color.FromArgb(255, r, g, b);
	}
}
