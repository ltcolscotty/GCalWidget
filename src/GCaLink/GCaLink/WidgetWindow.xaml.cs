using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI;
using GCaLink.Models;
using GCaLink.Services;
using Windows.ApplicationModel.UserDataTasks;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics.Contracts;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GCaLink
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WidgetWindow : Window
    {
        public WidgetWindow()
        {
            InitializeComponent();
        }

        private async void WidgetWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await SettingsRetriever.InitializeAsync();

            BkgStyleRadioSettings.SelectedIndex = SettingsRetriever.GetBackgroundSetting() switch
            {
                "Solid" => 0,
                "Mica" => 1,
                "Acrylic" => 2,
                _ => 0
            };



            CanvasCalLinkInput.Text = SettingsRetriever.GetCanvasICSLink();
        }

        private void BkgStyle_changed(object sender, SelectionChangedEventArgs e)
        {

        }

        private void GoogleSI_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Main_Save_Click(object sender, RoutedEventArgs e)
        {

        }
        
        private void Canvas_Save_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Refresh_Canvas_Sources(object sender, RoutedEventArgs e)
        {

        }
        private void Refresh_Google_Sources(object sender, RoutedEventArgs e)
        {

        }
        
        private async void UpdateRefreshButton()
        {
            GoogleCalService GCS = EventAggService.GetGoogleCalService();
            Dictionary<string, bool> sources = await SettingsRetriever.GetActiveSources(GCS);
            RefreshGoogleSourcesButton.Visibility = sources["google"]
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void GoogleEnabled(object sender, RoutedEventArgs e)
        {
            SettingsRetriever.SetGoogleEnabled(true);
        }
        private void GoogleDisabled(object sender, RoutedEventArgs e)
        {
            SettingsRetriever.SetGoogleEnabled(false);
        }

        private void CanvasEnabled(object sender, RoutedEventArgs e)
        {
            SettingsRetriever.SetCanvasEnabled(true);
        }

        private void CanvasDisabled(object sender, RoutedEventArgs e)
        {
            SettingsRetriever.SetCanvasEnabled(false);
        }
    }
}
