
using System.Linq;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace HelloWorld;

public partial class MainPage : ContentPage
{
    private bool _polling;
    private bool _refreshInProgress;

private const string SeriesDecisionsKey =
    "IndianCallSeriesDecisions";

    public MainPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _polling = true;
        _ = RefreshCallsAsync();

        // Refresh the displayed number while this page is visible.
        Dispatcher.StartTimer(
            TimeSpan.FromSeconds(2),
            () =>
            {
                if (!_polling)
                    return false;

                _ = RefreshCallsAsync();
                return true;
            });
    }

    protected override void OnDisappearing()
    {
        _polling = false;
        base.OnDisappearing();
    }

    private async void OnEnableCallScreeningClicked(
        object? sender, EventArgs e)
    {
#if ANDROID
        if (Android.OS.Build.VERSION.SdkInt <
            Android.OS.BuildVersionCodes.Q)
        {
            StatusLabel.Text =
                "Call screening requires Android 10 or later.";
            return;
        }

        try
        {
            // Ask permission to read contacts. This allows Android to
            // provide screening callbacks for saved contacts too.
            var contactsStatus =
                await Permissions.RequestAsync<Permissions.ContactsRead>();

            if (contactsStatus != PermissionStatus.Granted)
            {
                StatusLabel.Text =
                    "Contacts permission was not granted. Android may only " +
                    "provide calls from numbers not saved in your contacts.";
            }

            var roleManager =
                Android.App.Application.Context.GetSystemService(
                    Android.Content.Context.RoleService)
                as Android.App.Roles.RoleManager;

            if (roleManager is null)
            {
                StatusLabel.Text =
                    "Android's role manager is unavailable.";
                return;
            }

            if (!roleManager.IsRoleAvailable(
                    Android.App.Roles.RoleManager.RoleCallScreening))
            {
                StatusLabel.Text =
                    "Call-screening role is unavailable on this phone.";
                return;
            }

            if (roleManager.IsRoleHeld(
                    Android.App.Roles.RoleManager.RoleCallScreening))
            {
                StatusLabel.Text =
                    "Call screening is enabled. Make a test call.";
                return;
            }

            var intent = roleManager.CreateRequestRoleIntent(
                Android.App.Roles.RoleManager.RoleCallScreening);

            var activity =
                Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;

            if (activity is null)
            {
                StatusLabel.Text =
                    "Could not open the Android permission screen. Try again.";
                return;
            }

            activity.StartActivityForResult(intent, 1001);

            StatusLabel.Text =
                "Approve the call-screening request in Android.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text =
                $"Could not enable call screening: {ex.Message}";
        }
#else
        await DisplayAlert(
            "Android only",
            "Call capture is available only on Android.",
            "OK");
#endif
    }

    private async void OnRefreshCallsClicked(
        object? sender, EventArgs e)
    {
        await RefreshCallsAsync();
    }

    
private async Task RefreshCallsAsync()
{
#if ANDROID
    if (_refreshInProgress)
        return;

    _refreshInProgress = true;

    try
    {
        var calls = Platforms.Android.CallStorage.Load();

        CallsLayout.Clear();

        if (calls.Count == 0)
        {
            LatestNumberLabel.Text = "No caller captured yet";
            StatusLabel.Text =
                "No numbers have been captured. Enable call screening, then make a test call.";
            return;
        }

        LatestNumberLabel.Text = calls[0].PhoneNumber;
        StatusLabel.Text = $"{calls.Count} call(s) stored on this phone.";

        foreach (var call in calls)
        {
            CallsLayout.Add(new Label
            {
                Text =
                    $"{call.PhoneNumber}\n" +
                    $"{call.CapturedAtUtc.LocalDateTime:g}",
                FontSize = 16
            });

            CallsLayout.Add(new BoxView
            {
                HeightRequest = 1,
                Opacity = 0.2
            });
        }

        // Only ask about the newest captured call.
        await AskAboutIndianSeriesAsync(calls[0].PhoneNumber);
    }
    catch (Exception ex)
    {
        StatusLabel.Text = $"Could not load captured calls: {ex.Message}";
    }
    finally
    {
        _refreshInProgress = false;
    }
#endif
}

private static string? GetIndianFourDigitSeries(string? phoneNumber)
{
    if (string.IsNullOrWhiteSpace(phoneNumber))
        return null;

    // Keep digits only, removing spaces, +, brackets, and hyphens.
    var digits = new string(
        phoneNumber.Where(char.IsDigit).ToArray());

    // International format: 0091 followed by a 10-digit number.
    if (digits.Length == 14 &&
        digits.StartsWith("0091", StringComparison.Ordinal))
    {
        digits = digits[4..];
    }
    // International format: 91 followed by a 10-digit number.
    else if (digits.Length == 12 &&
             digits.StartsWith("91", StringComparison.Ordinal))
    {
        digits = digits[2..];
    }
    // Domestic trunk prefix: 0 followed by a 10-digit number.
    else if (digits.Length == 11 &&
             digits.StartsWith("0", StringComparison.Ordinal))
    {
        digits = digits[1..];
    }

    // This first version accepts 10-digit Indian numbers only.
    if (digits.Length != 10)
        return null;

    return digits[..4];
}

private static Dictionary<string, string> LoadSeriesDecisions()
{
    var json = Microsoft.Maui.Storage.Preferences.Get(
        SeriesDecisionsKey, "{}");

    return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
           ?? new Dictionary<string, string>();
}

private static void SaveSeriesDecision(
    string series, bool wantsToBlock)
{
    var decisions = LoadSeriesDecisions();

    // Save both Yes and No so the same series is not repeatedly prompted.
    decisions[series] = wantsToBlock ? "accepted" : "declined";

    Microsoft.Maui.Storage.Preferences.Set(
        SeriesDecisionsKey,
        JsonSerializer.Serialize(decisions));
}

private async Task AskAboutIndianSeriesAsync(string phoneNumber)
{
    var series = GetIndianFourDigitSeries(phoneNumber);

    // Ignore numbers that don't match the supported Indian format.
    if (series is null)
        return;

    var decisions = LoadSeriesDecisions();

    // Ask only once per series, including after restarting the app.
    if (decisions.ContainsKey(series))
        return;

    var wantsToBlock = await DisplayAlert(
        "Block number series?",
        $"An incoming call was received from a number starting with {series}.\n\n" +
        $"Would you like to block future calls from numbers starting with {series}?\n\n" +
        "Blocking is not active yet. Your choice will be saved for a future update.",
        "Yes, save preference",
        "No, thanks");

    SaveSeriesDecision(series, wantsToBlock);

    StatusLabel.Text = wantsToBlock
        ? $"Saved your preference for series {series}. Calls are NOT blocked yet."
        : $"You chose not to block series {series}.";
}


}