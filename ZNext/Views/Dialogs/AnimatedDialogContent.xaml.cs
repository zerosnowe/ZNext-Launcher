using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext.Views.Dialogs;

public sealed partial class AnimatedDialogContent : UserControl
{
	public AnimatedDialogContent()
	{
		InitializeComponent();
	}

	public UIElement? Body
	{
		get => BodyPresenter.Content as UIElement;
		set => BodyPresenter.Content = value;
	}

	private void AnimatedDialogContent_Loaded(object sender, RoutedEventArgs e)
	{
		OpenStoryboard.Begin();
	}
}
