using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using GCaLink.Models;
using GCaLink.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace GCaLink.ViewWindows.SettingsView;

public sealed partial class SettingsWindow : Window
{
	public SettingsWindow()
	{
		InitializeComponent();
		GoogleCalOptions options = SettingsRetriever.GetGoogleCalOptions();
		GoogleEnabledCheckBox.IsChecked = SettingsRetriever.GetGoogleEnabled();
		CredentialsStatusText.Text = options.HasClientCredentials
			? "OAuth client configured."
			: "No OAuth client configured.";
		_ = UpdateConnectionStatusAsync();
	}

	private async void ChooseCredentialsClick(object sender, RoutedEventArgs e)
	{
		FileOpenPicker picker = new()
		{
			SuggestedStartLocation = PickerLocationId.DocumentsLibrary
		};
		picker.FileTypeFilter.Add(".json");
		InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

		var file = await picker.PickSingleFileAsync();
		if (file == null)
			return;

		try
		{
			using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(file.Path));
			JsonElement root = document.RootElement;
			JsonElement client = root.TryGetProperty("installed", out JsonElement installed)
				? installed
				: root.GetProperty("web");
			string clientId = client.GetProperty("client_id").GetString() ?? "";
			string clientSecret = client.GetProperty("client_secret").GetString() ?? "";

			if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
				throw new InvalidDataException("The selected file does not contain a Google OAuth client.");

			SettingsRetriever.SetGoogleCredentials(clientId, clientSecret);
			await EventAggService.ReloadGoogleServiceAsync();
			CredentialsStatusText.Text = "OAuth client configured.";
			await UpdateConnectionStatusAsync();
		}
		catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidDataException)
		{
			CredentialsStatusText.Text = $"Could not read credentials: {ex.Message}";
		}
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
