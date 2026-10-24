
using global::Android.App;
using global::Android.Content;
using global::Android.OS;
using global::Android.Telecom;

namespace HelloWorld.Platforms.Android;

[Service(
    Name = "com.companyname.helloworld.IncomingCallScreeningService",
    Permission = "android.permission.BIND_SCREENING_SERVICE",
    Exported = true)]
[IntentFilter(new[] { "android.telecom.CallScreeningService" })]
public class IncomingCallScreeningService : CallScreeningService
{
    public override void OnScreenCall(Call.Details callDetails)
    {
        // This app's call-screening role is available on Android 10+.
        if (Build.VERSION.SdkInt < BuildVersionCodes.Q)
            return;

        // This app only stores incoming calls, not outgoing calls.
        if (callDetails.CallDirection != CallDirection.Incoming)
            return;

        try
        {
            // Respond immediately so normal incoming calls can ring.
            var response = new CallResponse.Builder()
                .SetDisallowCall(false)
                .SetRejectCall(false)
                .SetSilenceCall(false)
                .SetSkipCallLog(false)
                .SetSkipNotification(false)
                .Build();

            RespondToCall(callDetails, response);

            var handle = callDetails.GetHandle();
            var number = handle?.SchemeSpecificPart;

            if (string.IsNullOrWhiteSpace(number))
            {
                global::Android.Util.Log.Info(
                    "CallCapture",
                    "Incoming call did not provide a caller number.");
                return;
            }

            var record = new CallRecord
            {
                PhoneNumber = number,
                CapturedAtUtc = DateTimeOffset.UtcNow
            };

            CallStorage.Save(record);

            global::Android.Util.Log.Info(
                "CallCapture",
                $"Captured incoming number: {number}");
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error(
                "CallCapture",
                $"Failed to capture incoming call: {ex}");
        }
    }
}
