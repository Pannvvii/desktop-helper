using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using DesktopHelper;
using DesktopTaskAid.Services;
using DesktopTaskAid.ViewModels;
using System.Windows.Controls;
using System.Threading;

namespace DesktopTaskAid
{
    public partial class MainWindow : Window
    {
        public static Thread t = new Thread(HelperThread);
        public MainWindow()
        {
            LoggingService.Log("=== MainWindow Constructor BEGIN ===");
            

            try
            {
                LoggingService.Log("Calling InitializeComponent");
                InitializeComponent();
                LoggingService.Log("InitializeComponent completed successfully");

                LoggingService.Log("Calling Helper");
                
                t.Start();

                LoggingService.Log("Helper completed successfully");

                // Update welcome illustration based on current theme
                UpdateWelcomeIllustration();
                
                // Subscribe to theme changes to update illustration
                if (DataContext is ViewModels.MainViewModel viewModel)
                {
                    viewModel.ThemeChanged += UpdateWelcomeIllustration;
                    LoggingService.Log("Subscribed to ThemeChanged event");
                }
                
                // Hook into window events
                Loaded += MainWindow_Loaded;
                ContentRendered += MainWindow_ContentRendered;
                
                LoggingService.Log("=== MainWindow Constructor COMPLETED ===");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("ERROR in MainWindow constructor", ex);
                throw; // Re-throw to trigger global exception handler
            }
        }

        static void HelperThread()
        {
            HelperWindow.HelperMain();
        }

        private void UpdateWelcomeIllustration()
        {
            // Check if dark theme is active
            var isDarkTheme = Application.Current.Resources.MergedDictionaries
                .Any(d => d.Source?.ToString().Contains("darkTheme.xaml") == true);
            
            string imageName = isDarkTheme ? "sticker-dark.png" : "sticker-light.png";
            
            try
            {
                string imagePath = $"pack://application:,,,/assets/images/{imageName}";
                WelcomeIllustration.Source = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                LoggingService.Log($"Welcome illustration updated to: {imagePath}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to update welcome illustration", ex);
                // Fallback: try relative path
                try
                {
                    WelcomeIllustration.Source = new BitmapImage(new Uri($"/assets/images/{imageName}", UriKind.Relative));
                    LoggingService.Log($"Welcome illustration loaded with relative path: {imageName}");
                }
                catch (Exception ex2)
                {
                    LoggingService.LogError("Fallback image load also failed", ex2);
                }
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoggingService.Log("MainWindow Loaded event fired");
            LoggingService.Log($"Window Size: {Width}x{Height}");
            LoggingService.Log($"Window State: {WindowState}");
            LoggingService.Log($"Window Visible: {IsVisible}");
            LoggingService.Log($"DataContext Type: {DataContext?.GetType().Name ?? "NULL"}");
        }

        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            LoggingService.Log("MainWindow ContentRendered event fired - Window is now visible to user");
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            LoggingService.Log("MinimizeWindow clicked");
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeWindow(object sender, RoutedEventArgs e)
        {
            LoggingService.Log("MaximizeWindow clicked");
            this.WindowState = this.WindowState == WindowState.Maximized 
                ? WindowState.Normal 
                : WindowState.Maximized;
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            LoggingService.Log("CloseWindow clicked - Application shutting down");
            t.Join();
            this.Close();
            
        }
    }
}
