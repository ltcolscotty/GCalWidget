using Microsoft.UI.Xaml;
using MosaiCal.Services;
using MosaiCal.ViewWindows.WidgetView;
using System;
using System.Linq;
using System.Runtime.InteropServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MosaiCal
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            try
            {
                AppDataPaths.EnsureCreated();
            }
            catch (Exception exception)
            {
                ShowStartupError(
                    IntPtr.Zero,
                    $"MosaiCal cannot start because it could not initialize its required app-data folders."
                    + $"{Environment.NewLine}{Environment.NewLine}{exception.Message}"
                    + $"{Environment.NewLine}{Environment.NewLine}Details:{Environment.NewLine}{exception}",
                    "MosaiCal startup failed",
                    0x10);
                throw;
            }

            UnhandledException += OnUnhandledException;
        }

        [DllImport("user32.dll", EntryPoint = "MessageBoxW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int ShowStartupError(IntPtr hWnd, string text, string caption, uint type);

        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            LoggerService.LogException("Unhandled application exception", e.Exception);
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            StartupLaunchService.SyncWithSettings();
            if (IsStartupLaunch(args.Arguments))
            {
                if (!SettingsRetriever.GetPrimaryViewEnabled())
                {
                    LoggerService.Log("App: Startup launch skipped because the calendar view is disabled.");
                    Exit();
                    return;
                }

                ViewWindowManager.PrimaryViewClosedByUser += OnStartupPrimaryViewClosedByUser;
                ViewWindowManager.OpenConfiguredPrimaryViewOnStartup();
                return;
            }

            _window = new WidgetWindow();
            _window.Activate();
            await StartupLaunchService.RequestConsentIfNeededAsync(_window);
        }

        private static bool IsStartupLaunch(string? activationArguments)
        {
            return string.Equals(
                    activationArguments?.Trim(),
                    StartupLaunchService.StartupArgument,
                    StringComparison.OrdinalIgnoreCase) ||
                Environment.GetCommandLineArgs().Skip(1).Any(argument =>
                    string.Equals(argument, StartupLaunchService.StartupArgument, StringComparison.OrdinalIgnoreCase));
        }

        private void OnStartupPrimaryViewClosedByUser()
        {
            ViewWindowManager.PrimaryViewClosedByUser -= OnStartupPrimaryViewClosedByUser;
            SettingsRetriever.SetPrimaryViewEnabled(false);
            StartupLaunchService.SyncWithSettings();
        }
    }
}
