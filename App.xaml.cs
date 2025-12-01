using DesktopTaskAid.Services;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace DesktopTaskAid
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            LoggingService.Log("=== OnStartup BEGIN ===");
            
            try
            {
                // Set English culture for the entire application
                LoggingService.Log("Setting English culture");
                var englishCulture = new CultureInfo("en-US");
                Thread.CurrentThread.CurrentCulture = englishCulture;
                Thread.CurrentThread.CurrentUICulture = englishCulture;
                CultureInfo.DefaultThreadCurrentCulture = englishCulture;
                CultureInfo.DefaultThreadCurrentUICulture = englishCulture;
                
                LoggingService.Log("Setting up unhandled exception handlers");
                
                // Handle unhandled exceptions
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                DispatcherUnhandledException += App_DispatcherUnhandledException;
                EnsureStartMenuShortcut();
                LoggingService.Log("Calling base.OnStartup");
                base.OnStartup(e);
                
                LoggingService.Log("=== OnStartup COMPLETED SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("CRITICAL ERROR in OnStartup", ex);
                MessageBox.Show(
                    $"Critical startup error occurred. Check log file at:{Environment.NewLine}{LoggingService.GetLogFilePath()}{Environment.NewLine}{Environment.NewLine}Error: {ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            LoggingService.LogError("UNHANDLED EXCEPTION (CurrentDomain)", exception);
            
            if (e.IsTerminating)
            {
                LoggingService.Log("Application is terminating due to unhandled exception", "CRITICAL");
                MessageBox.Show(
                    $"Fatal error occurred. Check log file at:{Environment.NewLine}{LoggingService.GetLogFilePath()}{Environment.NewLine}{Environment.NewLine}Error: {exception?.Message}",
                    "Fatal Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LoggingService.LogError("UNHANDLED EXCEPTION (Dispatcher)", e.Exception);
            
            MessageBox.Show(
                $"An error occurred. Check log file at:{Environment.NewLine}{LoggingService.GetLogFilePath()}{Environment.NewLine}{Environment.NewLine}Error: {e.Exception.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            // Mark as handled to prevent app crash
            e.Handled = true;
        }

        private void EnsureStartMenuShortcut()
        {
            try
            {
                var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "DesktopTaskAid";
                var programsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                var appFolder = Path.Combine(programsFolder, appName);

                Directory.CreateDirectory(appFolder);

                var shortcutPath = Path.Combine(appFolder, $"{appName}.lnk");
                if (File.Exists(shortcutPath))
                {
                    LoggingService.Log($"Start Menu shortcut already exists at {shortcutPath}");
                    return;
                }

                var wshShellType = Type.GetTypeFromProgID("WScript.Shell");
                if (wshShellType == null)
                {
                    LoggingService.Log("WScript.Shell COM type not available; skipping Start Menu shortcut creation", "WARNING");
                    return;
                }

                dynamic shell = Activator.CreateInstance(wshShellType);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);

                var exePath = Assembly.GetEntryAssembly()?.Location ?? string.Empty;
                shortcut.TargetPath = exePath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
                shortcut.Description = appName;
                shortcut.IconLocation = exePath;

                shortcut.Save();
                LoggingService.Log($"Created Start Menu shortcut at {shortcutPath}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to ensure Start Menu shortcut", ex);
            }
        }
    }
}
