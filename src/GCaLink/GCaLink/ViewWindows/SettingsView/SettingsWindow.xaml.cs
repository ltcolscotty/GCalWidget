using System;
using System.Threading.Tasks;
using GCaLink.Models;
using GCaLink.Services;
using Microsoft.UI.Xaml.Media.Imaging;
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
		await SignInAsync();
	}

	private async void SignOutClick(object sender, RoutedEventArgs e)
	{
		await EventAggService.GetGoogleCalService().SignOutAsync();
		SettingsRetriever.SetGoogleEnabled(false);
		GoogleEnabledCheckBox.IsChecked = false;
		await UpdateConnectionStatusAsync();
	}

	private async void SwitchAccountClick(object sender, RoutedEventArgs e)
	{
		await EventAggService.GetGoogleCalService().SignOutAsync();
		SettingsRetriever.SetGoogleEnabled(false);
		GoogleEnabledCheckBox.IsChecked = false;
		await SignInAsync();
	}

	private async Task SignInAsync()
	{
		SignInButton.IsEnabled = false;
		ConnectionStatusText.Text = "Waiting for Google sign-in...";
		try
		{
			bool connected = await EventAggService.GetGoogleCalService().AuthorizeAsync();
			ConnectionStatusText.Text = connected ? "Connected to Google Calendar." : "Google sign-in did not complete.";
			await UpdateConnectionStatusAsync(connected);
		}
		finally
		{
			SignInButton.IsEnabled = true;
		}
	}

	private async Task UpdateConnectionStatusAsync(bool? connectedOverride = null)
	{
		bool connected = connectedOverride ?? await EventAggService.GetGoogleCalService().IsAccountActiveAsync();
		ConnectionStatusText.Text = connected ? "Connected to Google Calendar." : "Not connected";
		SignedOutPanel.Visibility = connected ? Visibility.Collapsed : Visibility.Visible;
		AccountCard.Visibility = connected ? Visibility.Visible : Visibility.Collapsed;

		if (connected)
		{
			GoogleAccountProfile? profile = await EventAggService.GetGoogleCalService().GetAccountProfileAsync();
			if (profile != null)
			{
				ProfileNameText.Text = string.IsNullOrWhiteSpace(profile.Name) ? profile.Email : profile.Name;
				ProfileEmailText.Text = profile.Email;
				if (Uri.TryCreate(profile.PictureUrl, UriKind.Absolute, out Uri? pictureUri))
					ProfileImage.Source = new BitmapImage(pictureUri);
			}
		}
	}

	private void GoogleEnabledChecked(object sender, RoutedEventArgs e) => SettingsRetriever.SetGoogleEnabled(true);
	private void GoogleEnabledUnchecked(object sender, RoutedEventArgs e) => SettingsRetriever.SetGoogleEnabled(false);
}
