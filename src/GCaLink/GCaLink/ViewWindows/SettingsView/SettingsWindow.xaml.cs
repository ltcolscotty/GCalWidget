using System;
using System.Threading.Tasks;
using GCaLink.Models;
using GCaLink.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GCaLink.ViewWindows.SettingsView;

public sealed partial class SettingsWindow : Window
{
	public SettingsWindow()
	{
		InitializeComponent();
		GoogleEnabledCheckBox.IsChecked = SettingsRetriever.GetGoogleEnabled();
		_ = UpdateConnectionStatusAsync();
	}

	private async void SignInClick(object sender, RoutedEventArgs e)
	{
		SignInButton.IsEnabled = false;
		ConnectionStatusText.Text = "Waiting for Google sign-in...";
		try
		{
			bool connected = await EventAggService.GetGoogleCalService().AuthorizeAsync();
			ConnectionStatusText.Text = connected ? "Connected to Google Calendar." : "Google sign-in did not complete.";
			SignOutButton.IsEnabled = connected;
		}
		finally
		{
			SignInButton.IsEnabled = true;
		}
	}

	private async void SignOutClick(object sender, RoutedEventArgs e)
	{
		await EventAggService.GetGoogleCalService().SignOutAsync();
		SettingsRetriever.SetGoogleEnabled(false);
		GoogleEnabledCheckBox.IsChecked = false;
		await UpdateConnectionStatusAsync();
	}

	private async Task UpdateConnectionStatusAsync()
	{
		bool connected = await EventAggService.GetGoogleCalService().IsAccountActiveAsync();
		ConnectionStatusText.Text = connected ? "Connected to Google Calendar." : "Not connected";
		SignOutButton.IsEnabled = connected;
	}

	private void GoogleEnabledChecked(object sender, RoutedEventArgs e) => SettingsRetriever.SetGoogleEnabled(true);
	private void GoogleEnabledUnchecked(object sender, RoutedEventArgs e) => SettingsRetriever.SetGoogleEnabled(false);
}
