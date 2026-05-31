using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ZNext.Services;
using ZNext.Services.Dialogs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ZNext
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        internal static Window? CurrentAppWindow { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            TryRegisterAppNotifications();
            
            // 添加全局异常处理
            UnhandledException += App_UnhandledException;
        }

        private static void TryRegisterAppNotifications()
        {
            AppNotificationService.Register();
        }

        /// <summary>
        /// 全局异常处理程序
        /// </summary>
        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // 标记异常已处理，防止应用崩溃
            e.Handled = true;
            
            // 记录错误信息
            System.Diagnostics.Debug.WriteLine($"未处理异常: {e.Exception.Message}");
            System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {e.Exception.StackTrace}");
            
            // 显示错误对话框
            ShowErrorDialog(e.Exception);
        }

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        private async void ShowErrorDialog(Exception ex)
        {
            try
            {
                var errorDialog = ModernDialogFactory.Create(
                    _window?.Content?.XamlRoot,
                    "应用程序错误",
                    $"发生了一个未处理的错误：\n\n{ex.Message}\n\n应用将尝试继续运行。",
                    closeButtonText: "确定",
                    defaultButton: ContentDialogButton.Close);

                if (errorDialog.XamlRoot != null)
                {
                    await DialogHost.ShowAsync(errorDialog);
                }
            }
            catch
            {
                // 如果显示对话框也失败，忽略错误
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            string? launchArguments = args?.Arguments;
            var authService = new Services.AuthService();
            string? startupToken = authService.Token;
            bool shouldPromptLogin = !authService.IsLoggedIn || string.IsNullOrWhiteSpace(startupToken);

            MainWindow mainWindow = LaunchMainWindow(launchArguments, startupToken);
            if (shouldPromptLogin)
            {
                _ = mainWindow.ShowStartupLoginDialogAsync();
            }
        }

        private MainWindow LaunchMainWindow(string? launchArguments, string? startupToken)
        {
            var mainWindow = new MainWindow(startupToken);
            _window = mainWindow;
            CurrentAppWindow = _window;
            _window.Activate();

            if (AppNotificationService.IsOpenConsoleLaunch(launchArguments))
            {
                mainWindow.NavigateToConsoleFromNotification();
            }

            return mainWindow;
        }

        internal static XamlRoot? TryGetCurrentXamlRoot()
        {
            return CurrentAppWindow?.Content?.XamlRoot;
        }
    }
}
