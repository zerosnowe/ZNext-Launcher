using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ZNext.Services;

internal sealed class ConsoleSessionTabRenderer
{
	public bool IsUpdatingSelection { get; private set; }

	public void Render(
		NavigationView navigationView,
		IReadOnlyList<ConsoleSession> sessions,
		ConsoleSession? activeSession,
		ElementTheme actualTheme,
		RoutedEventHandler closeRequested)
	{
		bool isDark = actualTheme == ElementTheme.Dark;
		IsUpdatingSelection = true;
		try
		{
			navigationView.MenuItems.Clear();
			navigationView.Visibility = sessions.Count <= 0 ? Visibility.Collapsed : Visibility.Visible;

			foreach (ConsoleSession session in sessions)
			{
				NavigationViewItem item = CreateSessionItem(session, ReferenceEquals(activeSession, session), isDark, closeRequested);
				navigationView.MenuItems.Add(item);
				if (ReferenceEquals(activeSession, session) && !ReferenceEquals(navigationView.SelectedItem, item))
				{
					navigationView.SelectedItem = item;
				}
			}
		}
		finally
		{
			IsUpdatingSelection = false;
		}
	}

	private static NavigationViewItem CreateSessionItem(
		ConsoleSession session,
		bool isActive,
		bool isDark,
		RoutedEventHandler closeRequested)
	{
		NavigationViewItem item = new NavigationViewItem
		{
			Tag = session,
			Height = 42.0,
			Margin = new Thickness(2.0, 0.0, 2.0, 4.0),
			Icon = new FontIcon
			{
				Glyph = "\ue756",
				FontFamily = new FontFamily("Segoe Fluent Icons"),
				FontSize = 12.0
			}
		};

		Grid content = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				},
				new ColumnDefinition
				{
					Width = GridLength.Auto
				}
			}
		};

		TextBlock title = new TextBlock
		{
			Text = session.Title,
			FontSize = 13.0,
			FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal,
			TextTrimming = TextTrimming.CharacterEllipsis,
			VerticalAlignment = VerticalAlignment.Center,
			Foreground = new SolidColorBrush(isDark ? ColorFromHex("#E5E7EB") : ColorFromHex("#1F2937"))
		};

		Button closeButton = new Button
		{
			Tag = session,
			Width = 24.0,
			Height = 24.0,
			Padding = new Thickness(0.0),
			Margin = new Thickness(4.0, 0.0, 0.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0.0),
			Opacity = isActive ? 0.95 : 0.75
		};
		closeButton.Click += closeRequested;
		closeButton.Content = new FontIcon
		{
			Glyph = "\ue711",
			FontSize = 10.0,
			Foreground = new SolidColorBrush(isDark ? ColorFromHex("#E5E7EB") : ColorFromHex("#3F3F46"))
		};

		Grid.SetColumn(title, 0);
		Grid.SetColumn(closeButton, 1);
		content.Children.Add(title);
		content.Children.Add(closeButton);
		item.Content = content;
		return item;
	}

	private static Color ColorFromHex(string hex)
	{
		string text = hex.TrimStart('#');
		byte r = Convert.ToByte(text[..2], 16);
		byte g = Convert.ToByte(text.Substring(2, 2), 16);
		byte b = Convert.ToByte(text.Substring(4, 2), 16);
		return Color.FromArgb(byte.MaxValue, r, g, b);
	}
}
