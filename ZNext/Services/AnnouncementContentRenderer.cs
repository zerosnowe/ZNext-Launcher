using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.System;
using Windows.UI;

namespace ZNext.Services;

internal sealed class AnnouncementContentRenderer
{
	public void SetPlainText(RichTextBlock target, string text)
	{
		target.Blocks.Clear();
		Paragraph paragraph = CreateParagraph();
		paragraph.Inlines.Add(new Run { Text = text });
		target.Blocks.Add(paragraph);
	}

	public void Render(RichTextBlock target, string markdown, bool extractFirstHeadingAsTitle, out string title)
	{
		title = "重要公告";
		target.Blocks.Clear();
		string text = (markdown ?? string.Empty).Replace("\r\n", "\n").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			Paragraph fallback = CreateParagraph();
			fallback.Inlines.Add(new Run { Text = "暂无详细内容", FontSize = 14 });
			target.Blocks.Add(fallback);
			return;
		}

		string[] lines = text.Split('\n');
		bool inCodeBlock = false;
		List<string> codeLines = new List<string>();
		bool hasBodyContent = false;
		for (int i = 0; i < lines.Length; i++)
		{
			string rawLine = lines[i] ?? string.Empty;
			while (HasUnclosedInlineHtmlTag(rawLine, "span") || HasUnclosedInlineHtmlTag(rawLine, "font"))
			{
				if (i + 1 >= lines.Length)
				{
					break;
				}
				rawLine += "\n" + (lines[++i] ?? string.Empty);
			}

			string trimmed = rawLine.Trim();
			if (trimmed.StartsWith("```", StringComparison.Ordinal))
			{
				if (inCodeBlock)
				{
					Paragraph codeParagraph = CreateParagraph();
					codeParagraph.Margin = new Thickness(0, 6, 0, 6);
					codeParagraph.FontFamily = new FontFamily("Consolas");
					codeParagraph.Foreground = new SolidColorBrush(ColorFromHex("#6B7280"));
					codeParagraph.Inlines.Add(new Run { Text = string.Join(Environment.NewLine, codeLines), FontSize = 13 });
					target.Blocks.Add(codeParagraph);
					codeLines.Clear();
					inCodeBlock = false;
					hasBodyContent = true;
				}
				else
				{
					inCodeBlock = true;
				}
				continue;
			}

			if (inCodeBlock)
			{
				codeLines.Add(rawLine);
				continue;
			}

			if (string.IsNullOrWhiteSpace(trimmed))
			{
				Paragraph blank = CreateParagraph();
				blank.Inlines.Add(new Run { Text = string.Empty });
				target.Blocks.Add(blank);
				continue;
			}

			if (extractFirstHeadingAsTitle
				&& title == "重要公告"
				&& (trimmed.StartsWith("# ") || trimmed.StartsWith("## ") || trimmed.StartsWith("### ")))
			{
				title = trimmed.TrimStart('#', ' ');
				continue;
			}

			if (trimmed.StartsWith("> ", StringComparison.Ordinal))
			{
				Paragraph quote = CreateParagraph();
				quote.Margin = new Thickness(10, 2, 0, 4);
				quote.Foreground = new SolidColorBrush(ColorFromHex("#6B7280"));
				quote.Inlines.Add(new Run { Text = "▎ ", FontSize = 14 });
				AddInline(quote, trimmed.Substring(2));
				target.Blocks.Add(quote);
				hasBodyContent = true;
				continue;
			}

			if (trimmed.StartsWith("- ", StringComparison.Ordinal) || trimmed.StartsWith("* ", StringComparison.Ordinal))
			{
				Paragraph bullet = CreateParagraph();
				bullet.Margin = new Thickness(0, 2, 0, 2);
				bullet.Inlines.Add(new Run { Text = "• ", FontSize = 14 });
				AddInline(bullet, trimmed.Substring(2));
				target.Blocks.Add(bullet);
				hasBodyContent = true;
				continue;
			}

			if (trimmed.StartsWith("### ", StringComparison.Ordinal))
			{
				Paragraph h3 = CreateHeading(17, new Thickness(0, 6, 0, 4));
				AddInline(h3, trimmed.Substring(4));
				target.Blocks.Add(h3);
				hasBodyContent = true;
				continue;
			}

			if (trimmed.StartsWith("## ", StringComparison.Ordinal))
			{
				Paragraph h2 = CreateHeading(19, new Thickness(0, 8, 0, 4));
				AddInline(h2, trimmed.Substring(3));
				target.Blocks.Add(h2);
				hasBodyContent = true;
				continue;
			}

			if (trimmed.StartsWith("# ", StringComparison.Ordinal))
			{
				Paragraph h1 = CreateHeading(21, new Thickness(0, 10, 0, 6));
				AddInline(h1, trimmed.Substring(2));
				target.Blocks.Add(h1);
				hasBodyContent = true;
				continue;
			}

			Paragraph normal = CreateParagraph();
			normal.Margin = new Thickness(0, 2, 0, 2);
			normal.FontSize = 14;
			AddInline(normal, rawLine);
			target.Blocks.Add(normal);
			hasBodyContent = true;
		}

		if (!hasBodyContent)
		{
			Paragraph fallback = CreateParagraph();
			fallback.Inlines.Add(new Run { Text = "暂无详细内容", FontSize = 14 });
			target.Blocks.Add(fallback);
		}
	}

	private static Paragraph CreateHeading(double fontSize, Thickness margin)
	{
		Paragraph heading = CreateParagraph();
		heading.Margin = margin;
		heading.FontSize = fontSize;
		heading.FontWeight = FontWeights.SemiBold;
		return heading;
	}

	private static Paragraph CreateParagraph()
	{
		return new Paragraph
		{
			FontFamily = new FontFamily("Microsoft YaHei UI")
		};
	}

	private static void AddInline(Paragraph paragraph, string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		text = WebUtility.HtmlDecode(text);
		Regex tokenRegex = new Regex(
			"(?<img>!\\[(?<imgAlt>[^\\]]*)\\]\\((?<imgUrl>[^)\\s]+)\\))"
			+ "|(?<imgTag><img\\s+[^>]*src=['\\\"]?(?<imgTagUrl>[^'\\\">\\s]+)['\\\"]?[^>]*>)"
			+ "|(?<bold>\\*\\*[^*]+\\*\\*)"
			+ "|(?<code>`[^`]+`)"
			+ "|(?<link>\\[(?<linkText>[^\\]]+)\\]\\((?<linkUrl>[^)\\s]+)\\))"
			+ "|(?<font><font(?<fontAttr>[^>]*)>(?<fontText>.*?)</font>)"
			+ "|(?<span><span(?<spanAttr>[^>]*)>(?<spanText>.*?)</span>)",
			RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
		int position = 0;
		foreach (Match match in tokenRegex.Matches(text))
		{
			if (match.Index > position)
			{
				paragraph.Inlines.Add(new Run { Text = text.Substring(position, match.Index - position) });
			}

			if (match.Groups["img"].Success || match.Groups["imgTag"].Success)
			{
				AddImageInline(paragraph, match);
			}
			else if (match.Groups["bold"].Success)
			{
				string token = match.Value;
				paragraph.Inlines.Add(new Run
				{
					Text = token.Substring(2, token.Length - 4),
					FontWeight = FontWeights.SemiBold
				});
			}
			else if (match.Groups["code"].Success)
			{
				string token = match.Value;
				paragraph.Inlines.Add(new Run
				{
					Text = token.Substring(1, token.Length - 2),
					FontFamily = new FontFamily("Consolas")
				});
			}
			else if (match.Groups["font"].Success || match.Groups["span"].Success)
			{
				AddStyledInline(paragraph, match);
			}
			else
			{
				AddLinkInline(paragraph, match);
			}

			position = match.Index + match.Length;
		}

		if (position < text.Length)
		{
			paragraph.Inlines.Add(new Run { Text = text[position..] });
		}
	}

	private static void AddImageInline(Paragraph paragraph, Match match)
	{
		string imageUrl = match.Groups["img"].Success ? match.Groups["imgUrl"].Value : match.Groups["imgTagUrl"].Value;
		if (string.IsNullOrWhiteSpace(imageUrl) && match.Groups["imgTag"].Success)
		{
			imageUrl = ExtractHtmlAttributeValue(match.Value, "src") ?? string.Empty;
		}
		string imageAlt = match.Groups["imgAlt"].Value;
		imageUrl = NormalizeUrl(imageUrl);
		if (Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri? uri))
		{
			Image image = new Image
			{
				MaxWidth = 920,
				MaxHeight = 360,
				Stretch = Stretch.Uniform,
				Margin = new Thickness(0, 6, 0, 6)
			};

			if (match.Groups["imgTag"].Success)
			{
				ApplyImageTagSize(image, match.Value);
			}

			image.Source = new BitmapImage(uri);
			paragraph.Inlines.Add(new InlineUIContainer { Child = image });
			return;
		}

		string imageInfo = string.IsNullOrWhiteSpace(imageAlt) ? "[图片]" : $"[图片: {imageAlt}]";
		if (!string.IsNullOrWhiteSpace(imageUrl))
		{
			imageInfo += $" {imageUrl}";
		}

		paragraph.Inlines.Add(new Run { Text = imageInfo });
	}

	private static void AddStyledInline(Paragraph paragraph, Match match)
	{
		bool isFontTag = match.Groups["font"].Success;
		string attrText = isFontTag ? match.Groups["fontAttr"].Value : match.Groups["spanAttr"].Value;
		string inlineText = WebUtility.HtmlDecode(isFontTag ? match.Groups["fontText"].Value : match.Groups["spanText"].Value);
		Run styledRun = new Run { Text = inlineText };

		string? colorRaw = ExtractHtmlAttributeValue(attrText, "color") ?? ExtractStyleValue(attrText, "color");
		if (TryParseColor(colorRaw ?? string.Empty, out Color color))
		{
			styledRun.Foreground = new SolidColorBrush(color);
		}

		string? fontSizeRaw = ExtractStyleValue(attrText, "font-size");
		if (TryParseCssFontSize(fontSizeRaw, out double fontSize))
		{
			styledRun.FontSize = fontSize;
		}

		string? fontWeightRaw = ExtractStyleValue(attrText, "font-weight");
		if (TryParseCssFontWeight(fontWeightRaw, out Windows.UI.Text.FontWeight fontWeight))
		{
			styledRun.FontWeight = fontWeight;
		}

		paragraph.Inlines.Add(styledRun);
	}

	private static void AddLinkInline(Paragraph paragraph, Match match)
	{
		string linkText = match.Groups["linkText"].Value;
		string url = NormalizeUrl(match.Groups["linkUrl"].Value);
		if (!string.IsNullOrWhiteSpace(linkText) && !string.IsNullOrWhiteSpace(url))
		{
			Hyperlink link = new Hyperlink();
			link.Inlines.Add(new Run { Text = linkText });
			link.Click += async (_, _) =>
			{
				if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
				{
					await Launcher.LaunchUriAsync(uri);
				}
			};
			paragraph.Inlines.Add(link);
			return;
		}

		paragraph.Inlines.Add(new Run { Text = match.Value });
	}

	private static void ApplyImageTagSize(Image image, string imgTagRaw)
	{
		if (image == null || string.IsNullOrWhiteSpace(imgTagRaw))
		{
			return;
		}

		string? widthRaw = ExtractHtmlAttributeValue(imgTagRaw, "width") ?? ExtractStyleValue(imgTagRaw, "width");
		string? heightRaw = ExtractHtmlAttributeValue(imgTagRaw, "height") ?? ExtractStyleValue(imgTagRaw, "height");

		if (TryParseCssLength(widthRaw, out double width))
		{
			image.Width = width;
		}
		if (TryParseCssLength(heightRaw, out double height))
		{
			image.Height = height;
		}
	}

	private static bool HasUnclosedInlineHtmlTag(string text, string tagName)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}

		int openCount = Regex.Matches(text, $"<{tagName}\\b", RegexOptions.IgnoreCase).Count;
		int closeCount = Regex.Matches(text, $"</{tagName}>", RegexOptions.IgnoreCase).Count;
		return openCount > closeCount;
	}

	private static string NormalizeUrl(string url)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return string.Empty;
		}

		string normalized = WebUtility.HtmlDecode(url).Trim();
		if (normalized.StartsWith("//", StringComparison.Ordinal))
		{
			return "https:" + normalized;
		}
		if (normalized.StartsWith("/", StringComparison.Ordinal))
		{
			return "https://www.mefrp.com" + normalized;
		}

		return normalized;
	}

	private static string? ExtractHtmlAttributeValue(string attributeText, string attributeName)
	{
		if (string.IsNullOrWhiteSpace(attributeText))
		{
			return null;
		}

		Match match = Regex.Match(attributeText, $"{attributeName}\\s*=\\s*['\\\"]?(?<v>[^'\\\">\\s]+)", RegexOptions.IgnoreCase);
		return match.Success ? match.Groups["v"].Value : null;
	}

	private static string? ExtractStyleValue(string attributeText, string key)
	{
		if (string.IsNullOrWhiteSpace(attributeText))
		{
			return null;
		}

		Match styleMatch = Regex.Match(attributeText, "style\\s*=\\s*['\\\"](?<style>[^'\\\"]+)['\\\"]", RegexOptions.IgnoreCase);
		if (!styleMatch.Success)
		{
			return null;
		}

		string style = styleMatch.Groups["style"].Value;
		foreach (string pair in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
		{
			string[] parts = pair.Split(':', 2, StringSplitOptions.TrimEntries);
			if (parts.Length == 2 && string.Equals(parts[0], key, StringComparison.OrdinalIgnoreCase))
			{
				return parts[1];
			}
		}

		return null;
	}

	private static bool TryParseCssFontSize(string? raw, out double size)
	{
		size = 0;
		if (string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}

		string value = raw.Trim().ToLowerInvariant().Replace("px", string.Empty).Trim();
		return double.TryParse(value, out size) && size > 0;
	}

	private static bool TryParseCssLength(string? raw, out double value)
	{
		value = 0;
		if (string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}

		string text = raw.Trim().ToLowerInvariant();
		if (text.EndsWith("px", StringComparison.Ordinal))
		{
			text = text[..^2].Trim();
		}

		return double.TryParse(text, out value) && value > 0;
	}

	private static bool TryParseCssFontWeight(string? raw, out Windows.UI.Text.FontWeight weight)
	{
		weight = FontWeights.Normal;
		if (string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}

		string value = raw.Trim().ToLowerInvariant();
		if (value == "bold" || value == "bolder")
		{
			weight = FontWeights.Bold;
			return true;
		}

		if (int.TryParse(value, out int numeric))
		{
			weight = numeric >= 600 ? FontWeights.Bold : FontWeights.Normal;
			return true;
		}

		return false;
	}

	private static bool TryParseColor(string raw, out Color color)
	{
		color = default;
		if (string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}

		string value = raw.Trim();
		try
		{
			if (value.StartsWith("#", StringComparison.Ordinal))
			{
				color = ColorFromHex(value);
				return color.A != 0
					|| string.Equals(value, "#000000", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(value, "#FF000000", StringComparison.OrdinalIgnoreCase);
			}

			Match rgb = Regex.Match(value, "^rgb\\s*\\(\\s*(?<r>\\d{1,3})\\s*,\\s*(?<g>\\d{1,3})\\s*,\\s*(?<b>\\d{1,3})\\s*\\)$", RegexOptions.IgnoreCase);
			if (rgb.Success)
			{
				byte r = (byte)Math.Clamp(int.Parse(rgb.Groups["r"].Value), 0, 255);
				byte g = (byte)Math.Clamp(int.Parse(rgb.Groups["g"].Value), 0, 255);
				byte b = (byte)Math.Clamp(int.Parse(rgb.Groups["b"].Value), 0, 255);
				color = Color.FromArgb(255, r, g, b);
				return true;
			}

			switch (value.ToLowerInvariant())
			{
			case "red":
				color = Color.FromArgb(255, 220, 38, 38);
				return true;
			case "orange":
				color = Color.FromArgb(255, 234, 88, 12);
				return true;
			case "yellow":
				color = Color.FromArgb(255, 202, 138, 4);
				return true;
			case "green":
				color = Color.FromArgb(255, 22, 163, 74);
				return true;
			case "blue":
				color = Color.FromArgb(255, 37, 99, 235);
				return true;
			case "purple":
				color = Color.FromArgb(255, 147, 51, 234);
				return true;
			case "cyan":
				color = Color.FromArgb(255, 8, 145, 178);
				return true;
			case "pink":
				color = Color.FromArgb(255, 219, 39, 119);
				return true;
			case "gray":
			case "grey":
				color = Color.FromArgb(255, 107, 114, 128);
				return true;
			case "white":
				color = Color.FromArgb(255, 255, 255, 255);
				return true;
			case "black":
				color = Color.FromArgb(255, 0, 0, 0);
				return true;
			default:
				return false;
			}
		}
		catch
		{
			return false;
		}
	}

	private static Color ColorFromHex(string hex)
	{
		if (string.IsNullOrWhiteSpace(hex))
		{
			return Color.FromArgb(0, 0, 0, 0);
		}

		string text = hex.TrimStart('#');
		if (text.Length == 6)
		{
			byte r = Convert.ToByte(text.Substring(0, 2), 16);
			byte g = Convert.ToByte(text.Substring(2, 2), 16);
			byte b = Convert.ToByte(text.Substring(4, 2), 16);
			return Color.FromArgb(byte.MaxValue, r, g, b);
		}
		if (text.Length == 8)
		{
			byte a = Convert.ToByte(text.Substring(0, 2), 16);
			byte r = Convert.ToByte(text.Substring(2, 2), 16);
			byte g = Convert.ToByte(text.Substring(4, 2), 16);
			byte b = Convert.ToByte(text.Substring(6, 2), 16);
			return Color.FromArgb(a, r, g, b);
		}

		return Color.FromArgb(0, 0, 0, 0);
	}
}
