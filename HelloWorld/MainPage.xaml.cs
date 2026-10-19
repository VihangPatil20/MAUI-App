
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace HelloWorld;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            ApiUrlEntry.Text =
                await SecureStorage.Default.GetAsync("CallsApiUrl") ?? string.Empty;

            await RefreshCallsAsync();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Unable to load settings: {ex.Message}";
        }
    }

    private async void OnSaveBackendClicked(object? sender, EventArgs e)
    {
        var url = ApiUrlEntry.Text?.Trim();
        var key = ApiKeyEntry.Text?.Trim();

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(key))
        {
            StatusLabel.Text = "Enter a valid HTTPS URL and API key.";
            return;
        }

        try
        {
            await SecureStorage.Default.SetAsync("CallsApiUrl", url);
            await SecureStorage.Default.SetAsync("CallsApiKey", key);

            ApiKeyEntry.Text = string.Empty;
            StatusLabel.Text = "Backend settings saved on this device.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Unable to save settings: {ex.Message}";
        }
    }

    private async void OnEnableCallScreeningClicked(
        object? sender, EventArgs e)
    {
#if ANDROID
        if (Android.OS.Build.VERSION.SdkInt <
            Android.OS.BuildVersionCodes.Q)
        {
            await DisplayAlert(
                "Unsupported Android version",
                "Call screening requires Android 10 or later.",
                "OK");
            return;
        }

        try
        {
            var intent = new Android.Content.Intent(
                "android.app.role.action.REQUEST_ROLE");

            intent.PutExtra(
                "android.app.role.extra.ROLE_NAME",
                "android.app.role.CALL_SCREENING");

            var activity =
                Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;

            if (activity is null)
            {
                StatusLabel.Text =
                    "Unable to open Android settings. Reopen the app and try again.";
                return;
            }

            activity.StartActivity(intent);
        }
        catch (Exception ex)
        {
            StatusLabel.Text =
                $"Unable to request call screening: {ex.Message}";
        }
#else
        await DisplayAlert(
            "Android only",
            "Call screening is available only on Android.",
            "OK");
#endif
    }

    private async void OnRefreshCallsClicked(object? sender, EventArgs e)
    {
        await RefreshCallsAsync();
    }

    private Task RefreshCallsAsync()
    {
        CallsLayout.Clear();

        try
        {
            var json = Preferences.Default.Get("CapturedCalls", "[]");

            var calls =
                JsonSerializer.Deserialize<List<CallRecord>>(json)
                ?? new List<CallRecord>();

            StatusLabel.Text =
                $"{calls.Count} captured call(s) on this device.";

            foreach (var call in calls)
            {
                CallsLayout.Add(new Label
                {
                    Text =
                        $"{call.PhoneNumber}\n{call.CapturedAtUtc.LocalDateTime:g}",
                    FontSize = 16
                });

                CallsLayout.Add(new BoxView
                {
                    HeightRequest = 1,
                    Opacity = 0.2
                });
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Unable to load captured calls: {ex.Message}";
        }

        return Task.CompletedTask;
    }
}
