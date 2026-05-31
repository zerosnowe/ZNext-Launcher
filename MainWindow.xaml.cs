using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ZNext
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void HtestButton_Click(object sender, RoutedEventArgs e)
        {
            // �����ť����¼�
            (sender as Button).Content = "Clicked!";
        }
    }
<Window
    x:Class="ZNext.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:ZNext"
    xmlns:controls="using:Microsoft.UI.Xaml.Controls"
    Title="MainWindow"
    Width="800"
    Height="450">
    <Grid>
        <controls:Button
            x:Name="htestButton"
            Content="htest"
            HorizontalAlignment="Center"
            VerticalAlignment="Center"
            Click="HtestButton_Click" />
    </Grid>
</Window>
}