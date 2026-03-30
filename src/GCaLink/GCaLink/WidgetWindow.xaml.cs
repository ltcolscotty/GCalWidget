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
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using GCaLink.Models;
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

        private void ColorSchemePicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            PublicKey ConfigOptions config = new ConfigOptions();
            var color = sender.Color;
            string hex = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
            HexOutput.Text = hex;
            config.BackgroundColor = hex;
            TaskList.Background = new SolidColorBrush(color);
        }
    }
}
