
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace HelloWorld;

public partial class MainPage : ContentPage
{
    private bool _polling;

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

    private Task RefreshCallsAsync()
    {
#if ANDROID
        try
        {
            var calls =
                Platforms.Android.CallStorage.Load();

            CallsLayout.Clear();

            if (calls.Count == 0)
            {
                LatestNumberLabel.Text = "No caller captured yet";
                StatusLabel.Text =
                    "No numbers have been captured. Enable call screening, " +
                    "then make a test call.";
                return Task.CompletedTask;
            }

            LatestNumberLabel.Text = calls[0].PhoneNumber;
            StatusLabel.Text =
                $"{calls.Count} call(s) stored on this phone.";

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
        }
        catch (Exception ex)
        {
            StatusLabel.Text =
                $"Could not load captured calls: {ex.Message}";
        }
#endif

        return Task.CompletedTask;
    }
}
