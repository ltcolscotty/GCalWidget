using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using GCaLink.ViewWindows.WidgetView;
using GCaLink.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GCaLink
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
            UnhandledException += OnUnhandledException;
        }

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
